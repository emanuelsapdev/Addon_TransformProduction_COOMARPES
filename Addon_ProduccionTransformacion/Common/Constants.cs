namespace Addon_TransformProduction.Common
{
    public static class CONSTANTS_GLOBALS
    {
        // NUEVO: Constantes globales del Addon de Transformación de Producción
        public static class DOCS_MARKETING
        {
            public static class TABLES
            {
                public const string IGN1 = "IGN1"; // Goods Receipt PO Lines
                public const string IGE1 = "IGE1"; // Goods Issue Lines
            }

            public static class UDFs
            {
                public const string TransformProductionEntry = "ITPS_TP_Entry";
                public const string TransformProductionLineId = "ITPS_TP_LineId";
            }

        }


        ///////////////////////////////////////////////////////////////////////

        // NO SE USARAN MAS
        public static class Addon
        {
            public const string Name = "Addon_TransformProduction";
            public const string Identifier = "Addon_TransformProduction";
        }

        public static class FormTypes
        {

            public const string TransformProduction = "UDO_FT_ITPS_TRANSFPROD";
        }

        public static class Menus
        {

            public const string ParentToolsMenuUID = "43520";

            public const string ParentInventoryMenuUID = "3072";

            public const string OpenActualizacionCostosUID = "ITPS_TRANSFPROD_MENU";
        }

        public static class Tables
        {
            public const string ConfigsDevelopment = "CONFIGS_DEVELOPMENT";
            public const string ConfigsDevelopmentWithAt = "@" + ConfigsDevelopment;

            public const string TransformProductionHead = "ITPS_TRANSFPROD_HEAD";
            public const string TransformProductionHeadWithAt = "@" + TransformProductionHead;

            public const string TransformProductionLine = "ITPS_TRANSFPROD_LIN";
            public const string TransformProductionLineWithAt = "@" + TransformProductionLine;

        }


        public static class UdoTransformProduction
        {
            public const string ObjectCode = "ITPS_TRANSFPROD";
            public const string ObjectName = "Producción / Transformación";
        }

        public static class StagingStatus
        {
            public const string Draft = "Borrador";
            public const string Pending = "Pendiente";
            public const string Processing = "Procesando";
            public const string Completed = "Completado";
            public const string Error = "Error";
            public const string Cancelled = "Cancelado";
        }

        public static class FixedValues
        {
            public const string Yes = "Y";
            public const string No = "N";
        }

        public static class Messages
        {
            public const string ConnectedOk = "Addon_TransformProduction conectado correctamente.";
            public const string FatalErrorPrefix = "Error fatal en Addon_TransformProduction: ";

            public const string FinalizingAddon = "Finalizando Addon_TransformProduction...";
            public const string RestartingAddon = "Reiniciando Addon_TransformProduction...";

            public const string UiApiNotDefined = "UIAPI no definido";
            public const string DiApiNotDefined = "DIAPI no definido";

            public const string MissingSapConnectionString = "No se recibió el string de conexión de SAP Business One. Ejecute el AddOn desde SAP (Add-On Administration).";
            public const string UiApiConnectErrorPrefix = "No fue posible conectarse a SAP Business One UI API (SboGuiApi.Connect). ";
            public const string UiApiGetApplicationErrorPrefix = "No fue posible obtener la instancia de SAP Business One UI API (SboGuiApi.GetApplication). ";
            public const string UiApiApplicationNull = "SboGuiApi.GetApplication devolvió null. Verifique que SAP Business One esté abierto y que el AddOn se ejecute desde SAP.";

            public const string EventRouterInitErrorPrefix = "Error inicializando eventos: ";
            public const string ItemEventErrorPrefix = "ItemEvent: ";
            public const string RightClickEventErrorPrefix = "RightClickEvent: ";
            public const string FormDataEventErrorPrefix = "FormDataEvent: ";
            public const string MenuEventErrorPrefix = "MenuEvent: ";
            public const string AppEventErrorPrefix = "AppEvent: ";
            public const string MenuRegisterErrorPrefix = "Error registrando el menú del addon: ";

            public const string ConfigInfraErrorPrefix = "Error creando tabla de configuraciones de desarrollos: ";



            public const string TxtFilePathErrorPrefix = "GetTxtFilePath Error  -> ";
            public const string ExcelFilePathErrorPrefix = "GetExcelFilePath Error -> ";
            public const string ExcelMissingColumns = "El Excel no contiene las columnas esperadas: \"Código Artículo\", \"Almacén\" y \"Costo Nuevo\" en la fila 1.";

            public const string ValidationItemCodeRequired = "Falta el código de artículo.";
            public const string ValidationItemNotFound = "El artículo no existe en el maestro de artículos.";
            public const string ValidationWarehouseRequired = "Falta el código de almacén.";
            public const string ValidationWarehouseNotFound = "El almacén no existe.";
            public const string ValidationItemManagesBatchesOrSerials = "El artículo maneja lotes/series — no soportado en esta versión.";
            public const string ValidationNewCostInvalid = "El costo nuevo debe ser mayor a cero.";
            public const string ValidationScheduledDateRequired = "Debe indicar la fecha de programación.";
            public const string ValidationScheduledTimeInvalid = "La hora programada debe tener formato HH:mm.";
            public const string ValidationScheduledDateTimePast = "La fecha/hora programada debe ser futura.";
            public const string ValidationNoWarehouseSelected = "Seleccioná el almacén a procesar.";
            public const string ValidationPendingErrorsInGrid = "Hay filas con errores para el almacén seleccionado. Corregí el Excel y volvé a importarlo antes de confirmar.";
            public const string ValidationNoValidRowsToStage = "No hay filas válidas para programar en el almacén seleccionado.";
            public const string ValidationInvalidWarehouseCodesPrefix = "El Excel contiene almacenes que no existen: ";
            public const string ValidationRowWarehouseMismatch = "No corresponde al almacén seleccionado.";

            public const string CostUpdateScheduledOk = "Tarea de actualización de costos programada correctamente.";
            public const string CostUpdateScheduleErrorPrefix = "Error programando la actualización de costos: ";
        }
    }
}
