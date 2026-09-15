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
    }
}