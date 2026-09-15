using System;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        public static class CONSTANTS
        {
            public const string FORM_TYPE = "60004";
            public const string FORM_UNIQUE_ID = "WINDOW_BATCHES";
            public const string TITLE = "Lotes";

            /// <summary>
            /// Devuelve el UID único del formulario de lotes correspondiente al TypeCount del
            /// formulario de producción que lo invoca: un formulario de lotes por cada
            /// instancia de TransformProduction abierta.
            /// </summary>
            public static string ObtenerFormUID(string typeCount)
            {
                return $"{FORM_UNIQUE_ID}_{typeCount}";
            }

            /// <summary>
            /// Recupera el TypeCount del formulario de producción desde el UID del formulario
            /// de lotes (inverso de ObtenerFormUID). Devuelve null si el UID no es de este addon.
            /// </summary>
            public static string ObtenerTypeCount(string formUid)
            {
                string prefijo = FORM_UNIQUE_ID + "_";
                return formUid != null && formUid.StartsWith(prefijo, StringComparison.Ordinal)
                    ? formUid.Substring(prefijo.Length)
                    : null;
            }

            public static class DATATABLE
            {
                public const string UID = "DT_BATCHES";

                public static class COLUMNS
                {
                    public const string NUM_LINE = "Col_NumLin";
                    public const string CHECK = "Col_Check";
                    public const string BATCH = "Col_Batch";
                    public const string DUE_DATE = "Col_DueDate";
                    public const string WAREHOUSE = "Col_Whs";
                    public const string QTY = "Col_Qty";
                    public const string QTY_ASSIGNED = "Col_QtyAsig";
                }
            }

            public static class UID
            {
                public const string LABEL_ART = "Item_0";
                public const string TEXT_ART = "Item_1";
                public const string MATRIX = "Item_2";

                public static class BUTTONS
                {
                    public const string OK = "Item_3";
                    public const string CANCEL = "Item_4";
                    //public const string ADD_BATCH = "Item_5";
                }

                public static class GRID_COLUMNS
                {
                    public const string NUM_LINE = "#";
                    public const string CHECK = "Col_0";
                    public const string BATCH = "Col_1";
                    public const string DUE_DATE = "Col_2";
                    public const string WAREHOUSE = "Col_3";
                    public const string QTY = "Col_4";
                    public const string QTY_ASSIGNED = "Col_5";
                }
            }

            public static class MESSAGES
            {
                public static string MATRIX_ADD_NEW_LINE_ERROR_PREFIX = $"(AddNewLine) Error al agregar nueva linea. ";

            }
        }
    }
}
