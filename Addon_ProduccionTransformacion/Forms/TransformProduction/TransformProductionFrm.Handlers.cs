using Addon_TransformProduction.Common;
using Addon_TransformProduction.Forms.WindowBatches;
using Addon_TransformProduction.Services;
using System;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Al abrir el formulario (et_FORM_LOAD) crea el contexto de la producción, guarda la
        /// referencia al formulario y aplica el post-proceso de la UI (editabilidad de columnas).
        /// </summary>
        public void ManejarCargaFormulario(string formUid)
        {
            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            ctx.FormTransfProd = oForm;

            //InicializarFormulario(oForm);
        }

        /// <summary>
        /// Al cerrar el formulario (et_FORM_CLOSE) cierra el formulario de lotes asociado (si
        /// quedó abierto) y elimina el contexto de la producción.
        /// </summary>
        public void ManejarCierreFormulario(string formUid)
        {
            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            try
            {
                if (ctx.FormBatches != null) ctx.FormBatches.Close();
            }
            catch { }

            ContextManager.Eliminar(oForm.TypeCount.ToString());
        }

        /// <summary>
        /// Abre el formulario de selección de lotes al presionar el botón de cabecera
        /// (CONSTANTS.UID.BUTTONS.LOTE_SELECT). Delegado desde OnItemEvent. Le pasa el
        /// TypeCount del formulario de producción para que se cree/use el formulario de
        /// lotes específico de esa instancia.
        /// </summary>
        public void AbrirFormularioLotes(SAPbouiCOM.Form oForm)
        {
            try
            {
                var windowBatches = new WindowBatchesFrm();
                windowBatches.MostrarFormulario(oForm.TypeCount.ToString());
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError($"Error abriendo el formulario de lotes: {ex.Message}");
            }
        }

        /// <summary>
        /// Activa el botón de lotes cuando hay un artículo cargado y una cantidad válida en la
        /// cabecera; lo desactiva en caso contrario.
        /// </summary>
        public void HabilitarBotonSeleccionLotes(SAPbouiCOM.Form oForm)
        {
            string itemCode = LeerCodigoArticulo(oForm);
            string qtyConsumed = LeerCantidadConsumida(oForm);

            SAPbouiCOM.Item oItem = oForm.Items.Item(CONSTANTS.UID.BUTTONS.LOTE_SELECT);

            oItem.Enabled = !string.IsNullOrWhiteSpace(itemCode) && EsCantidadConsumidaValida(qtyConsumed, out _);
        }

        /// <summary>
        /// El usuario seleccionó el menú del addon que abre este formulario: reutiliza la
        /// instancia existente si ya está abierta, o crea un documento nuevo de producción.
        /// </summary>
        public void ManejarMenuRegistrado()
        {
            if (MostrarFormularioExistente()) return;

            AbrirFormularioNuevo();
        }

        private bool MostrarFormularioExistente()
        {
            try
            {
                var oExisting = ConnectionSDK.UIAPI.Forms.Item(CONSTANTS.UID.FORM);
                oExisting.Select();
                oExisting.Visible = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ManejarCreacionProduccion(SAPbouiCOM.Form oForm, out bool BubbleEvent)
        {
            BubbleEvent = true;
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            if (!ValidarFormulario(oForm, ctx)) 
            { 
                BubbleEvent = false; 
                return; 
            }

            //if (ctx.FormBatches != null && ctx.FormBatches.Visible)
            //{
            //    NotificationService.MostrarError("Debe cerrar el formulario de lotes antes de crear la producción.");
            //    BubbleEvent = false;
            //    return;
            //}
            //CrearProduccion(ctx);
        }
        public void ManejarCreacionProduccionPendiente(SAPbouiCOM.Form oForm, out bool BubbleEvent)
        {
            BubbleEvent = true;

            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            if (!ValidarFormulario(oForm, ctx))
            {
                BubbleEvent = false;
                return;
            }

            //if (ctx.FormBatches != null && ctx.FormBatches.Visible)
            //{
            //    NotificationService.MostrarError("Debe cerrar el formulario de lotes antes de crear la producción.");
            //    BubbleEvent = false;
            //    return;
            //}

            //CrearProduccionPendiente(ctx);
        }
    }
}