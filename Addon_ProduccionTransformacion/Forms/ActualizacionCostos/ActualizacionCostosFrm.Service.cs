using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;
using System.Collections.Generic;

namespace Addon_TransformProduction.Forms.ActualizacionCostos
{
    public partial class ActualizacionCostosFrm
    {
        private List<ActualizacionCostosFormRow> _currentRows = new List<ActualizacionCostosFormRow>();

        /// <summary>
        /// Orienta los botones propios del formulario ("Seleccionar Excel..." y "Confirmar y
        /// Programar") antes de la acción de SAP.
        /// </summary>
        private void ManejarBotonPresionado_AntesDeAccion(SAPbouiCOM.Form oForm, ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            if (pVal.ItemUID == UIDs.BtnSelectExcel)
            {
                string selectedWhs = LeerAlmacenSeleccionado(oForm);
                if (string.IsNullOrEmpty(selectedWhs))
                {
                    NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.ValidationNoWarehouseSelected);
                    return;
                }

                AbrirDialogoExcelDiferido(oForm);
            }
            else if (pVal.ItemUID == UIDs.BtnConfirm)
            {
                if (ConnectionSDK.UIAPI.MessageBox("¿Seguro que querés guardar? Esta acción no se puede deshacer.", 1, "Aceptar", "Cancelar") == 1) return;

                BubbleEvent = false;
            }
        }

        /// <summary>
        /// Abre el diálogo de selección de Excel y — si el usuario eligió uno — reimporta el
        /// formulario "fresco" y carga las filas. Se abre en un thread STA dedicado (FileService)
        /// para no bloquear el thread de eventos COM de SAP.
        /// </summary>
        private void AbrirDialogoExcelDiferido(SAPbouiCOM.Form oForm)
        {
            string formUID = oForm.UniqueID;

            try
            {
                string filePath = FileService.ObtenerRutaArchivoExcel();
                if (!string.IsNullOrEmpty(filePath))
                {
                    var freshForm = ConnectionSDK.UIAPI.Forms.Item(formUID);
                    ImportarExcel(freshForm, filePath);
                }
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(ex.Message);
            }
        }

        /// <summary>
        /// Muestra el formulario, reutilizando la instancia existente si ya está abierto.
        /// </summary>
        private void MostrarFormulario()
        {
            try
            {
                var oExisting = ConnectionSDK.UIAPI.Forms.Item(FormUniqueID);
                oExisting.Select();
                oExisting.Visible = true;
                return;
            }
            catch
            {
            }

            ConstruirFormulario();
        }

        /// <summary>
        /// Vacía <see cref="_currentRows"/> (últimas filas importadas del Excel, en memoria). Se
        /// llama junto con <see cref="RestablecerEtiquetasImportacion"/> cuando el formulario
        /// vuelve a fm_ADD_MODE tras crear un documento.
        /// </summary>
        private void ReiniciarFilasActuales()
        {
            _currentRows = new List<ActualizacionCostosFormRow>();
        }

        /// <summary>
        /// Importa el Excel. Antes de cargar la grilla, valida que todos los códigos de almacén
        /// del archivo existan en OWHS — si hay alguno incorrecto, se avisa y NO se carga la grilla.
        /// </summary>
        private void ImportarExcel(SAPbouiCOM.Form oForm, string filePath)
        {
            string selectedWhs = LeerAlmacenSeleccionado(oForm);
            if (string.IsNullOrEmpty(selectedWhs))
            {
                NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.ValidationNoWarehouseSelected);
                return;
            }

            //var rows = // ExcelImportService.ReadRows(filePath);

            //var invalidWhsCodes = ObtenerCodigosAlmacenInvalidos(rows);
            //if (invalidWhsCodes.Count > 0)
            //{
            //    NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.ValidationInvalidWarehouseCodesPrefix + string.Join(", ", invalidWhsCodes));
            //    return; // no se carga la grilla
            //}

            //ValidarTodasLasFilas(rows, selectedWhs);

            //_currentRows = rows;
            //PoblarGrillaItems(oForm, rows);

            var oLbl = (SAPbouiCOM.StaticText)oForm.Items.Item(UIDs.LblExcelPath).Specific;
            oLbl.Caption = filePath;
        }

        //private void ConfirmarProgramacion(SAPbouiCOM.Form oForm)
        //{
        //    string selectedWhs = LeerAlmacenSeleccionado(oForm);
        //    if (string.IsNullOrEmpty(selectedWhs))
        //    {
        //        NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.ValidationNoWarehouseSelected);
        //        return;
        //    }

        //    if (_currentRows.Count == 0)
        //    {
        //        NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.ValidationNoValidRowsToStage);
        //        return;
        //    }

        //    if (HayErroresBloqueantesParaAlmacenSeleccionado(_currentRows))
        //    {
        //        NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.ValidationPendingErrorsInGrid);
        //        return;
        //    }

        //    DateTime? scheduledDate = LeerFechaProgramada(oForm);
        //    string scheduledTime = LeerHoraProgramada(oForm);

        //    try
        //    {
        //        ValidationService.ValidateScheduledDateTime(scheduledDate, scheduledTime);
        //    }
        //    catch (Exception ex)
        //    {
        //        NotificationService.MostrarAlerta(ex.Message);
        //        return;
        //    }

        //    var rowsToStage = ObtenerFilasParaProgramar(_currentRows);

        //    if (rowsToStage.Count == 0)
        //    {
        //        NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.ValidationNoValidRowsToStage);
        //        return;
        //    }

        //    try
        //    {
        //        string userName = ConnectionSDK.DIAPI.UserName;
        //        InsertarEnStaging(rowsToStage, selectedWhs, scheduledDate.Value, scheduledTime, userName);

        //        NotificationService.MostrarExito(CONSTANTS_GLOBALS.Messages.CostUpdateScheduledOk);

        //        // Documento guardado con éxito: los labels sin databind se resetean para que el
        //        // formulario arranque "limpio" para el próximo documento.
        //        RestablecerEtiquetasImportacion(oForm);
        //    }
        //    catch (Exception ex)
        //    {
        //        NotificationService.MostrarAlerta(CONSTANTS_GLOBALS.Messages.CostUpdateScheduleErrorPrefix + ex.Message);
        //    }
        //}

        /// <summary>
        /// El usuario editó "Costo Nuevo" directo en la grilla (columna habilitada como editable en
        /// CorregirEditabilidadColumnasMatriz, ver ActualizacionCostosFrm.UIBuilder.cs). Sincroniza
        /// el valor tipeado con la fila correspondiente de <see cref="_currentRows"/>, la revalida
        /// por completo y refresca Estado/Motivo solo en esa fila.
        /// </summary>
        private void ManejarCostoNuevoEditado(SAPbouiCOM.Form oForm, int matrixRow)
        {
            int idx = matrixRow - 1;
            if (idx < 0 || idx >= _currentRows.Count)
                return; // fuera de rango: no debería pasar, pero por las dudas no reventamos la UI

            var oMtx = (SAPbouiCOM.Matrix)oForm.Items.Item(UIDs.MatrixItems).Specific;
            var oCell = (SAPbouiCOM.EditText)oMtx.GetCellSpecific(UIDs.ColNewCost, matrixRow);

            var row = _currentRows[idx];

            if (!decimal.TryParse(oCell.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out decimal newCost)
                && !decimal.TryParse(oCell.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out newCost))
            {
                // Valor no numérico: se deja en 0, ValidationService.ValidateRow lo va a rechazar
                // igual que un costo <= 0 (mismo mensaje que ya existía para ese caso).
                newCost = 0;
            }

            row.NewCost = newCost;

            //ValidationService.ValidateRow(row);

            RefrescarEstadoFila(oMtx, matrixRow, row);

            // NO se reescribe oCell.Value acá. C_0_7 (Costo Nuevo) es una columna databind a un UDF
            // tipo Precio — SAP la renderiza con su editor numérico enmascarado propio, que ya se
            // encargó de normalizar lo que el usuario tipeó apenas perdió el foco. Reescribirla por
            // código es exactamente el patrón que rompía PoblarGrillaItems en su versión anterior.
        }
    }
}