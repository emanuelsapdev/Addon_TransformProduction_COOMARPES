using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Crea la entrada de menú del addon bajo "Inventario" (una sola vez, al iniciar),
        /// si todavía no existe. Idempotente: si el addon se reinicia (aet_LanguageChanged /
        /// aet_FontChanged relanzan Program.Main), no vuelve a crear el ítem.
        /// </summary>
        public static void RegistrarMenus()
        {
            try
            {
                var oMenus = ConnectionSDK.UIAPI.Menus;

                if (oMenus.Exists(CONSTANTS.MENU_UID))
                    return;

                var oParentMenu = oMenus.Item(CONSTANTS.PARENT_INVENTORY_MENU_UID);

                var oCreationPackage = (MenuCreationParams)ConnectionSDK.UIAPI.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                oCreationPackage.Type = BoMenuType.mt_STRING;
                oCreationPackage.UniqueID = CONSTANTS.MENU_UID;
                oCreationPackage.String = CONSTANTS.UDO.OBJECT_NAME;
                oCreationPackage.Position = -1;

                oParentMenu.SubMenus.AddEx(oCreationPackage);
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS.MESSAGES.MENU_REGISTER_ERROR_PREFIX + ex.Message);
            }
        }

        private static void EstablecerColumnaEditable(Matrix oMatrix, string colUid, bool editable)
        {
            oMatrix.Columns.Item(colUid).Editable = editable;
        }

        /// <summary>
        /// Columnas de la grilla que el usuario carga (las que B1 Studio deja editables).
        /// </summary>
        private static readonly string[] ColumnasEditablesDetalle =
        {
            CONSTANTS.UID.GRID_COLUMNS.QUANTITY_OBTAINED,
            CONSTANTS.UID.GRID_COLUMNS.WAREHOUSE,
            CONSTANTS.UID.GRID_COLUMNS.PRICE,
            CONSTANTS.UID.GRID_COLUMNS.CURRENT,
            CONSTANTS.UID.GRID_COLUMNS.BATCH_NUM,
            CONSTANTS.UID.GRID_COLUMNS.EXTDATE_BATCH,
            CONSTANTS.UID.GRID_COLUMNS.MNFDATE_BATCH,
            CONSTANTS.UID.GRID_COLUMNS.INDATE_BATCH
        };

        /// <summary>
        /// Habilita o bloquea la edición del documento: columnas cargables de la grilla y
        /// Cantidad consumida; al bloquear, también el campo Producto. Al habilitar, Producto no
        /// se toca: solo es editable en modo agregar (ver SincronizarCampoProducto).
        /// </summary>
        public static void EstablecerEdicionDocumento(SAPbouiCOM.Form oForm, bool editable)
        {
            // SAP no deja deshabilitar el item/columna que tiene el foco.
            if (!editable) QuitarFocoDeEdicion(oForm);

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            foreach (string colUid in ColumnasEditablesDetalle)
                EstablecerColumnaEditable(oMatrix, colUid, editable);

            oForm.Items.Item(CONSTANTS.UID.HEADER.QUANTITY).Enabled = editable;

            if (!editable && ExisteItem(oForm, CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR))
                oForm.Items.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR).Enabled = false;
        }

        /// <summary>
        /// Lleva el foco a Comentarios para poder deshabilitar Producto, Cantidad y la grilla.
        /// </summary>
        private static void QuitarFocoDeEdicion(SAPbouiCOM.Form oForm)
        {
            try
            {
                oForm.ActiveItem = CONSTANTS.UID.HEADER.REMARK;
            }
            catch
            {
                // Si Comentarios no puede tomar el foco, se intenta igual deshabilitar.
            }
        }

        /// <summary>
        /// Asegura el campo espejo de "Producto" (ver CONSTANTS.UID.CHOOSE_FROM_LIST): UserDataSource,
        /// CFL de artículos filtrado por lista de materiales, EditText en la misma posición que
        /// el campo original y el original oculto. Se llama en et_FORM_ACTIVATE (en et_FORM_LOAD
        /// SAP rechaza modificar el campo original del UDO con "Invalid item"). Cada paso es
        /// idempotente, así que si alguno falla se reintenta en la próxima activación.
        /// Devuelve true si en esta llamada se creó el campo espejo.
        /// </summary>
        public bool AsegurarCampoProductoConFiltro(SAPbouiCOM.Form oForm)
        {
            string paso = string.Empty;
            bool creado = false;
            try
            {
                paso = "UserDataSource";
                AsegurarUserDataSourceProducto(oForm);

                paso = "ChooseFromList";
                AsegurarChooseFromListArticulos(oForm);

                if (!ExisteItem(oForm, CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR))
                {
                    paso = "crear campo espejo";
                    CrearCampoEspejoProducto(oForm);
                    creado = true;
                }

                SAPbouiCOM.Item oOriginal = oForm.Items.Item(CONSTANTS.UID.HEADER.ITEM_CODE);
                if (oOriginal.Visible)
                {
                    paso = "quitar foco del campo original";
                    QuitarFocoCampoOriginal(oForm);

                    paso = "ocultar campo original";
                    oOriginal.Visible = false;

                    paso = "etiqueta";
                    oForm.Items.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_LABEL).LinkTo = CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR;
                }
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError($"{CONSTANTS.MESSAGES.CFL_ITEM_BUILD_ERROR_PREFIX}({paso}) {ex.Message}");
            }

            return creado;
        }

        private static void AsegurarUserDataSourceProducto(SAPbouiCOM.Form oForm)
        {
            try
            {
                oForm.DataSources.UserDataSources.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.USER_DATASOURCE);
            }
            catch
            {
                oForm.DataSources.UserDataSources.Add(
                    CONSTANTS.UID.CHOOSE_FROM_LIST.USER_DATASOURCE, BoDataType.dt_SHORT_TEXT, 50);
            }
        }

        private static void AsegurarChooseFromListArticulos(SAPbouiCOM.Form oForm)
        {
            ChooseFromList oCfl;
            try
            {
                oCfl = oForm.ChooseFromLists.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.UID);
                return; // ya creado y filtrado
            }
            catch
            {
                oCfl = CrearChooseFromListArticulos(oForm);
            }

            AplicarFiltroListaMateriales(oCfl);
        }

        private static void CrearCampoEspejoProducto(SAPbouiCOM.Form oForm)
        {
            SAPbouiCOM.Item oOriginal = oForm.Items.Item(CONSTANTS.UID.HEADER.ITEM_CODE);
            SAPbouiCOM.Item oEspejo = oForm.Items.Add(CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR, BoFormItemTypes.it_EDIT);
            oEspejo.Left = oOriginal.Left;
            oEspejo.Top = oOriginal.Top;
            oEspejo.Width = oOriginal.Width;
            oEspejo.Height = oOriginal.Height;
            oEspejo.FromPane = oOriginal.FromPane;
            oEspejo.ToPane = oOriginal.ToPane;

            var oEdit = (EditText)oEspejo.Specific;
            oEdit.DataBind.SetBound(true, "", CONSTANTS.UID.CHOOSE_FROM_LIST.USER_DATASOURCE);
            oEdit.ChooseFromListUID = CONSTANTS.UID.CHOOSE_FROM_LIST.UID;
            oEdit.ChooseFromListAlias = CONSTANTS.UID.CHOOSE_FROM_LIST.ALIAS;
        }

        /// <summary>
        /// SAP no deja ocultar el item que tiene el foco: si lo tiene el campo original, se lo
        /// pasa al campo espejo (o, si no se puede, a la cantidad consumida).
        /// </summary>
        private static void QuitarFocoCampoOriginal(SAPbouiCOM.Form oForm)
        {
            if (oForm.ActiveItem != CONSTANTS.UID.HEADER.ITEM_CODE) return;

            try
            {
                oForm.ActiveItem = CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR;
            }
            catch
            {
                oForm.ActiveItem = CONSTANTS.UID.HEADER.QUANTITY;
            }
        }

        private static bool ExisteItem(SAPbouiCOM.Form oForm, string itemUid)
        {
            try
            {
                oForm.Items.Item(itemUid);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ChooseFromList CrearChooseFromListArticulos(SAPbouiCOM.Form oForm)
        {
            var oParams = (ChooseFromListCreationParams)ConnectionSDK.UIAPI.CreateObject(BoCreatableObjectType.cot_ChooseFromListCreationParams);
            oParams.UniqueID = CONSTANTS.UID.CHOOSE_FROM_LIST.UID;
            oParams.ObjectType = CONSTANTS.UID.CHOOSE_FROM_LIST.OBJECT_TYPE;
            oParams.MultiSelection = false;

            return oForm.ChooseFromLists.Add(oParams);
        }

        /// <summary>
        /// Deja el CFL mostrando solo artículos con lista de materiales de producción
        /// (OITM."TreeType" = 'P'). SetConditions reemplaza las condiciones anteriores.
        /// </summary>
        private static void AplicarFiltroListaMateriales(ChooseFromList oCfl)
        {
            var oConditions = (Conditions)ConnectionSDK.UIAPI.CreateObject(BoCreatableObjectType.cot_Conditions);

            Condition oCondition = oConditions.Add();
            oCondition.Alias = CONSTANTS.UID.CHOOSE_FROM_LIST.TREE_TYPE_ALIAS;
            oCondition.Operation = BoConditionOperation.co_EQUAL;
            oCondition.CondVal = CONSTANTS.UID.CHOOSE_FROM_LIST.TREE_TYPE_PRODUCTION;

            oCfl.SetConditions(oConditions);
        }
    }
}