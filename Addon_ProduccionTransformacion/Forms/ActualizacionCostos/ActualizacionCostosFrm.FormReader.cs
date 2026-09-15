using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Repositories;
using SAPbouiCOM;
using System.Collections.Generic;
using System.Globalization;

namespace Addon_TransformProduction.Forms.ActualizacionCostos
{
    public partial class ActualizacionCostosFrm
    {
        /// <summary>
        /// Puebla la grilla a partir de las filas importadas del Excel.
        /// </summary>
        /// <remarks>
        /// NO usa <c>Matrix.Clear()</c>/<c>Matrix.AddRow()</c> + <c>GetCellSpecific().Value</c>.
        /// Las columnas de costos están databind directo a UDFs tipo Precio de
        /// "@ITPS_COSTUPD_LINE" — SAP renderiza esas columnas con su editor numérico enmascarado
        /// propio, y asignarle un string por código vía GetCellSpecific().Value NO lo parsea de
        /// forma confiable. La forma correcta es escribir a través del DBDataSource de la tabla
        /// hija y reflejar con <c>Matrix.LoadFromDataSource()</c>.
        /// </remarks>
        private void PoblarGrillaItems(SAPbouiCOM.Form oForm, List<ActualizacionCostosFormRow> rows)
        {
            var oMtx = (Matrix)oForm.Items.Item(UIDs.MatrixItems).Specific;
            var oDbDs = oForm.DataSources.DBDataSources.Item("@ITPS_COSTUPD_LINE");

            // Vacía el DataSource de atrás para adelante (RemoveRecord reindexa las filas
            // restantes, recorrer de adelante para atrás salteo filas).
            for (int i = oDbDs.Size - 1; i >= 0; i--)
                oDbDs.RemoveRecord(i);

            int rowCount = rows.Count > 0 ? rows.Count : 1;
            for (int i = 0; i < rowCount; i++)
                oDbDs.InsertRecord(i);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                oDbDs.SetValue("U_ITPS_ItemCode", i, row.ItemCode ?? string.Empty);
                oDbDs.SetValue("U_ITPS_ItemName", i, row.ItemName ?? string.Empty);
                oDbDs.SetValue("U_ITPS_WhsCode", i, row.WhsCode ?? string.Empty);
                oDbDs.SetValue("U_ITPS_WhsName", i, row.WhsName ?? string.Empty);

                oDbDs.SetValue("U_ITPS_CurrentCost", i, row.CurrentCost.ToString(CultureInfo.InvariantCulture));
                oDbDs.SetValue("U_ITPS_NewCost", i, row.NewCost.ToString(CultureInfo.InvariantCulture));

                var (estado, motivo) = CalcularEstadoFila(row);
                oDbDs.SetValue("U_ITPS_LineStatus", i, estado);
                oDbDs.SetValue("U_ITPS_LineError", i, motivo ?? string.Empty);
            }

            oMtx.LoadFromDataSource();
        }

        /// <summary>
        /// Calcula Estado/Motivo de una fila a partir de su estado de validación actual. Función
        /// pura reutilizada tanto por <see cref="PoblarGrillaItems"/> (población masiva vía
        /// DBDataSource) como por <see cref="RefrescarEstadoFila"/> (actualización en vivo de una
        /// sola fila ya existente en la matriz).
        /// </summary>
        private static (string Estado, string Motivo) CalcularEstadoFila(ActualizacionCostosFormRow row)
        {
            string estado = !row.IsValid ? "Error" : (!row.MatchesSelectedWarehouse ? "Otro almacén" : "OK");
            string motivo = row.ErrorMessage;
            if (string.IsNullOrEmpty(motivo) && !row.MatchesSelectedWarehouse)
                motivo = CONSTANTS_GLOBALS.Messages.ValidationRowWarehouseMismatch;

            return (estado, motivo);
        }

        /// <summary>
        /// Actualiza las columnas Estado/Motivo (C_0_8/C_0_9) de una fila puntual YA EXISTENTE en
        /// la grilla, escribiendo directo en la matriz visual — a diferencia de las columnas de
        /// costos (editor enmascarado), estas dos son texto simple, así que GetCellSpecific().Value
        /// es seguro acá. Se usa después de editar "Costo Nuevo" a mano.
        /// </summary>
        private void RefrescarEstadoFila(Matrix oMtx, int matrixRow, ActualizacionCostosFormRow row)
        {
            var (estado, motivo) = CalcularEstadoFila(row);

            ((EditText)oMtx.GetCellSpecific(UIDs.ColStatus, matrixRow)).Value = estado;
            ((EditText)oMtx.GetCellSpecific(UIDs.ColError, matrixRow)).Value = motivo;
        }

        /// <summary>
        /// Almacén único elegido en la cabecera, o null si todavía no se eligió ninguno.
        /// </summary>
        /// <remarks>
        /// El campo (<see cref="UIDs.EdtWarehouse"/>) es un EditText con ChooseFromList (CFL_0,
        /// ObjectType Warehouses) — ya no un ComboBox: SAP escribe el código elegido directo en
        /// <c>EditText.Value</c>, así que alcanza con leer el Value tal cual.
        /// </remarks>
        private string LeerAlmacenSeleccionado(SAPbouiCOM.Form oForm)
        {
            var oEdt = (EditText)oForm.Items.Item(UIDs.EdtWarehouse).Specific;
            string raw = oEdt.Value?.Trim();
            return string.IsNullOrEmpty(raw) ? null : raw;
        }

        /// <summary>
        /// Refresca el StaticText <see cref="UIDs.LblWhsName"/> con el nombre del almacén
        /// actualmente elegido en <see cref="UIDs.EdtWarehouse"/> (vacío si no hay ninguno).
        /// </summary>
        private void RefrescarEtiquetaNombreAlmacen(SAPbouiCOM.Form oForm)
        {
            var oLbl = (StaticText)oForm.Items.Item(UIDs.LblWhsName).Specific;
            string whsCode = LeerAlmacenSeleccionado(oForm);
            oLbl.Caption = string.IsNullOrEmpty(whsCode)
                ? string.Empty
                : (WarehouseRepository.ObtenerNombreAlmacen(whsCode) ?? string.Empty);
        }

        /// <summary>
        /// Vuelve <see cref="UIDs.LblExcelPath"/> (ruta del Excel) y <see cref="UIDs.LblWhsName"/>
        /// (nombre del almacén) a los mismos valores por defecto que traen en el .xml al abrir el
        /// formulario. Se llama al guardar exitosamente el documento y al volver a fm_ADD_MODE.
        /// </summary>
        private static void RestablecerEtiquetasImportacion(SAPbouiCOM.Form oForm)
        {
            var oLblPath = (StaticText)oForm.Items.Item(UIDs.LblExcelPath).Specific;
            oLblPath.Caption = "(ningún archivo seleccionado)";

            var oLblWhsName = (StaticText)oForm.Items.Item(UIDs.LblWhsName).Specific;
            oLblWhsName.Caption = string.Empty;
        }
    }
}