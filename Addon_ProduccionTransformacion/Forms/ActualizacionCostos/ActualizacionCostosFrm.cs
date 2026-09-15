using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Forms.ActualizacionCostos
{
    public partial class ActualizacionCostosFrm : IFormEventHandler
    {
        /// <summary>
        /// FormTypeEx propio de este formulario (no extiende una pantalla existente de SAP B1).
        /// Se usa para enrutar eventos (<see cref="EventRouter"/>), NO para ubicar la instancia ya
        /// abierta del formulario — para eso está <see cref="FormUniqueID"/>.
        /// </summary>
        public const string FormType = ""; //Constants.FormTypes.ActualizacionCostos;

        /// <summary>
        /// UID del formulario, tal como quedó definido en el atributo <c>uid</c> del &lt;form&gt;
        /// de Forms/ActualizacionCostos/B1 Studio/ActualizacionCostosArtsFrm.xml. Se usa con
        /// <c>Forms.Item()</c> para reutilizar la instancia si ya está abierta (ver
        /// <see cref="MostrarFormulario"/>) — distinto de <see cref="FormType"/> (FormTypeEx), que
        /// es lo que SAP reporta en los eventos de UI API.
        /// </summary>
        public const string FormUniqueID = "UDO_F_ITPS_COSTUPD";

        /// <summary>
        /// Shell de eventos: determina qué item/evento dispara cada evento y delega a
        /// un único método por evento (vive en Service/FormReader/UIBuilder).
        /// </summary>
        public void OnMenuEvent(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            if (!pVal.BeforeAction && pVal.MenuUID == CONSTANTS_GLOBALS.Menus.OpenActualizacionCostosUID)
            {
                try
                {
                    MostrarFormulario();
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError(ex.Message);
                }
            }
        }

        public void OnItemEvent(string FormUID, ref ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            try
            {
                SAPbouiCOM.Form oForm;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(FormUID);
                }
                catch
                {
                    // Formulario ya destruido/invalidado por SAP: no es un error funcional.
                    return;
                }

                if (pVal.EventType == BoEventTypes.et_ITEM_PRESSED && pVal.BeforeAction)
                {
                    ManejarBotonPresionado_AntesDeAccion(oForm, pVal, out BubbleEvent);
                }
                else if (pVal.EventType == BoEventTypes.et_VALIDATE && !pVal.BeforeAction
                    && pVal.ItemUID == UIDs.MatrixItems && pVal.ColUID == UIDs.ColNewCost)
                {
                    // Costo Nuevo es editable en la grilla (ver CorregirEditabilidadColumnasMatriz,
                    // en ActualizacionCostosFrm.UIBuilder.cs): revalida esa fila apenas el usuario
                    // termina de editar la celda, sin esperar a "Confirmar y Programar".
                    ManejarCostoNuevoEditado(oForm, pVal.Row);
                }
                else if (pVal.ItemUID == UIDs.EdtWarehouse && !pVal.BeforeAction
                    && (pVal.EventType == BoEventTypes.et_CHOOSE_FROM_LIST || pVal.EventType == BoEventTypes.et_LOST_FOCUS))
                {
                    // Se actualiza el nombre del almacén (UIDs.LblWhsName) tanto si el usuario elige
                    // uno desde el ChooseFromList como si edita el código a mano y sale del campo.
                    RefrescarEtiquetaNombreAlmacen(oForm);
                }
                else if (pVal.EventType == BoEventTypes.et_ALL_EVENTS && !pVal.BeforeAction)
                {
                    // El formulario cambió de Mode (ej. fm_ADD_MODE -> fm_OK_MODE al quedar el
                    // documento ya creado/cargado) — resincroniza "Seleccionar Excel..."/"Confirmar
                    // y Programar" con el Mode actual.
                    ActualizarEstadoBotonesAccion(oForm);
                }
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(ex.Message);
            }
        }

        /// <summary>
        /// A diferencia de lo que decía el comentario previo antes de que el formulario pasara a
        /// ser un UDO (AutoManaged="1", ObjectType="ITPS_COSTUPD" en el .xml): este formulario SÍ
        /// está atado a un objeto de negocio (el UDO), así que SAP dispara este evento — por
        /// ejemplo al cargar un documento existente vía Find, o al completarse un Add. Se usa para
        /// resincronizar "Seleccionar Excel..."/"Confirmar y Programar" con el Mode del formulario.
        /// </summary>
        public void OnFormDataEvent(ref BusinessObjectInfo boi, out bool BubbleEvent)
        {
            BubbleEvent = true;

            if (boi.BeforeAction)
                return;

            try
            {
                SAPbouiCOM.Form oForm;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(boi.FormUID);
                }
                catch
                {
                    // El formulario pudo cerrarse entre el disparo del evento y su manejo.
                    return;
                }

                ActualizarEstadoBotonesAccion(oForm);

                if (oForm.Mode == BoFormMode.fm_ADD_MODE)
                {
                    RestablecerEtiquetasImportacion(oForm);
                }
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(ex.Message);
            }
        }
    }
}