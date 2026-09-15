using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;
using System.IO;

namespace Addon_TransformProduction.Forms.ActualizacionCostos
{
    public partial class ActualizacionCostosFrm
    {
        /// <summary>
        /// Ruta física del .xml exportado desde B1 Studio para este formulario. Se despliega junto
        /// al ejecutable conservando la misma estructura de carpetas que tiene en el proyecto.
        /// </summary>
        private static readonly string RutaXmlFormulario = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Forms", "ActualizacionCostos", "B1 Studio", "ActualizacionCostosArtsFrm.xml");

        /// <summary>
        /// Construye el formulario cargando la definición exportada desde B1 Studio (.xml), en vez
        /// de armarlo item por item por código. El formulario está diseñado como formulario de UDO
        /// (AutoManaged="1", ObjectType="ITPS_COSTUPD" en el .xml).
        /// </summary>
        private SAPbouiCOM.Form ConstruirFormulario()
        {
            string xml = File.ReadAllText(RutaXmlFormulario);
            ConnectionSDK.UIAPI.LoadBatchActions(ref xml);

            var oForm = ConnectionSDK.UIAPI.Forms.Item(FormUniqueID);

            // El .xml exportado por B1 Studio marca todas las columnas de la grilla como
            // editable="1" por defecto — solo "Costo Nuevo" tiene que ser editable por el usuario;
            // el resto lo completa el addon al importar el Excel. Se corrige acá en vez de editar
            // el .xml a mano, para no desalinear el diseño con lo que se ve en B1 Studio.
            CorregirEditabilidadColumnasMatriz(oForm);

            // Por las dudas el .xml venga con un código ya cargado, se deja el label de nombre de
            // almacén sincronizado desde el arranque.
            RefrescarEtiquetaNombreAlmacen(oForm);

            // Estado inicial de "Seleccionar Excel..."/"Confirmar y Programar" según el Mode con el
            // que arranca el formulario (normalmente fm_ADD_MODE, por ser un documento nuevo).
            ActualizarEstadoBotonesAccion(oForm);

            oForm.Visible = true;

            return oForm;
        }

        /// <summary>
        /// Habilita "Seleccionar Excel..." y "Confirmar y Programar" solo cuando el formulario está
        /// armando un documento NUEVO (<c>fm_ADD_MODE</c>); en cualquier otro Mode (sobre todo
        /// <c>fm_OK_MODE</c>) los deja deshabilitados.
        /// </summary>
        private static void ActualizarEstadoBotonesAccion(SAPbouiCOM.Form oForm)
        {
            bool isNewDocument = oForm.Mode == BoFormMode.fm_ADD_MODE;

            oForm.Items.Item(UIDs.BtnSelectExcel).Enabled = isNewDocument;
            oForm.Items.Item(UIDs.BtnConfirm).Enabled = isNewDocument;
        }

        private static void CorregirEditabilidadColumnasMatriz(SAPbouiCOM.Form oForm)
        {
            var oMtx = (Matrix)oForm.Items.Item(UIDs.MatrixItems).Specific;

            EstablecerColumnaEditable(oMtx, UIDs.ColItemCode, false);
            EstablecerColumnaEditable(oMtx, UIDs.ColItemName, false);
            EstablecerColumnaEditable(oMtx, UIDs.ColWhsCode, false);
            EstablecerColumnaEditable(oMtx, UIDs.ColWhsName, false);
            EstablecerColumnaEditable(oMtx, UIDs.ColCurrentCost, false);
            EstablecerColumnaEditable(oMtx, UIDs.ColNewCost, true);
            EstablecerColumnaEditable(oMtx, UIDs.ColStatus, false);
            EstablecerColumnaEditable(oMtx, UIDs.ColError, false);
        }

        private static void EstablecerColumnaEditable(Matrix oMtx, string colUid, bool editable)
        {
            oMtx.Columns.Item(colUid).Editable = editable;
        }

        /// <summary>
        /// Crea la entrada de menú del addon bajo "Inventario" (una sola vez, al iniciar), si
        /// todavía no existe.
        /// </summary>
        public static void RegistrarMenusCustom()
        {
            try
            {
                var oMenus = ConnectionSDK.UIAPI.Menus;

                // Idempotencia: si el addon se reinicia (aet_LanguageChanged / aet_FontChanged
                // disparan Application.Restart(), que vuelve a correr Program.Main), no hay que
                // volver a crear el ítem.
                if (oMenus.Exists(CONSTANTS_GLOBALS.Menus.OpenActualizacionCostosUID))
                    return;

                var oParentMenu = oMenus.Item(CONSTANTS_GLOBALS.Menus.ParentInventoryMenuUID);

                var oCreationPackage = (MenuCreationParams)ConnectionSDK.UIAPI.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                oCreationPackage.Type = BoMenuType.mt_STRING;
                oCreationPackage.UniqueID = CONSTANTS_GLOBALS.Menus.OpenActualizacionCostosUID;
                oCreationPackage.String = "Actualización de Costos";
                oCreationPackage.Position = -1; // al final del submenú de "Inventario"

                oParentMenu.SubMenus.AddEx(oCreationPackage);
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS_GLOBALS.Messages.MenuRegisterErrorPrefix + ex.Message);
            }
        }
    }
}