using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;
using System.IO;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        private static readonly string RutaXmlFormulario = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Forms", "TransformProduction", "B1 Studio", "TransformProductionFrm.xml");

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

        /// <summary>
        /// Abre un documento nuevo de Producción/Transformación. Carga el diseño del formulario
        /// exportado desde B1 Studio (.xml, misma estructura de carpetas que WindowBatches/
        /// ActualizacionCostos) y lo deja visible en modo agregar.
        /// </summary>
        public void AbrirFormularioNuevo()
        {
            try
            {
                string xml = File.ReadAllText(RutaXmlFormulario);
                ConnectionSDK.UIAPI.LoadBatchActions(ref xml);

                var oForm = ConnectionSDK.UIAPI.Forms.Item(CONSTANTS.UID.FORM);
                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS.MESSAGES.FORM_OPEN_ERROR_PREFIX + ex.Message);
            }
        }

        /// <summary>
        /// Post-proceso de la UI al abrir el formulario (et_FORM_LOAD):
        /// corrige la editabilidad de las columnas de la grilla (B1 Studio deja todo editable
        /// por defecto) y deja el botón de lotes deshabilitado hasta que haya artículo y
        /// cantidad válidos.
        /// </summary>
        public void InicializarFormulario(SAPbouiCOM.Form oForm)
        {
            try
            {
                var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;

                EstablecerColumnaEditable(oMatrix, CONSTANTS.UID.GRID_COLUMNS.SUBPRODUCT, false);
                EstablecerColumnaEditable(oMatrix, CONSTANTS.UID.GRID_COLUMNS.SUBPRODUCT_NAME, false);
                EstablecerColumnaEditable(oMatrix, CONSTANTS.UID.GRID_COLUMNS.QUANTITY_OBTAINED, true);
                EstablecerColumnaEditable(oMatrix, CONSTANTS.UID.GRID_COLUMNS.UNIT_MEASUREMENT, false);
                EstablecerColumnaEditable(oMatrix, CONSTANTS.UID.GRID_COLUMNS.LAST_PUR_PRICE, false);
                EstablecerColumnaEditable(oMatrix, CONSTANTS.UID.GRID_COLUMNS.PRICE, true);

                oForm.Items.Item(CONSTANTS.UID.BUTTONS.LOTE_SELECT).Enabled = false;
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS.MESSAGES.FORM_INIT_ERROR_PREFIX + ex.Message);
            }
        }

        private static void EstablecerColumnaEditable(Matrix oMatrix, string colUid, bool editable)
        {
            oMatrix.Columns.Item(colUid).Editable = editable;
        }

        /// <summary>
        /// Asigna al campo "Producto" de la cabecera un ChooseFromList de artículos que muestra
        /// solo los que tienen lista de materiales de producción (OITM."TreeType" = 'P').
        /// Reemplaza al CFL automático que SAP genera por el UDF vinculado a Artículos
        /// (U_ITPS_ItemCode), que no filtra. Idempotente: si el CFL ya existe en el
        /// formulario lo reutiliza y solo re-aplica la condición.
        /// </summary>
        public void ConfigurarChooseFromListArticulo(SAPbouiCOM.Form oForm)
        {
            try
            {
                ChooseFromList oCfl = ObtenerOCrearChooseFromListArticulo(oForm);
                AplicarFiltroListaMateriales(oCfl);

                var oItemCode = (EditText)oForm.Items.Item(CONSTANTS.UID.HEADER.ITEM_CODE).Specific;
                oItemCode.ChooseFromListUID = CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE;
                oItemCode.ChooseFromListAlias = CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_ALIAS;
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS.MESSAGES.CFL_ITEM_CONFIG_ERROR_PREFIX + ex.Message);
            }
        }

        private static ChooseFromList ObtenerOCrearChooseFromListArticulo(SAPbouiCOM.Form oForm)
        {
            try
            {
                return oForm.ChooseFromLists.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE);
            }
            catch
            {
                // No existe todavía en este formulario: se crea abajo.
            }

            var oParams = (ChooseFromListCreationParams)ConnectionSDK.UIAPI.CreateObject(BoCreatableObjectType.cot_ChooseFromListCreationParams);
            oParams.UniqueID = CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE;
            oParams.ObjectType = CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_OBJECT_TYPE;
            oParams.MultiSelection = false;

            return oForm.ChooseFromLists.Add(oParams);
        }

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