namespace Addon_TransformProduction.Forms.TransformProduction
{
    partial class TransformProductionFrm
    {
        public static class CONSTANTS
        {
            public const string FORM_TYPE = "UDO_FT_ITPS_TRANSFPROD";

            public const string MENU_UID = "47627";

            public const string PARENT_TOOLS_MENU_UID = "43520";
            public const string PARENT_INVENTORY_MENU_UID = "3072";

            /// <summary>Menús estándar de SAP que cambian el modo del formulario.</summary>
            public static class SAP_MENUS
            {
                /// <summary>Gestión → Tipos de cambio e índices.</summary>
                public const string EXCHANGE_RATES = "3333";
                public const string FIND = "1281";
                public const string ADD = "1282";
            }

            public static class TABLES
            {
                public const string TRANSFORM_PRODUCTION_HEAD = "ITPS_TRANSFPROD_CAB";
                public const string TRANSFORM_PRODUCTION_HEAD_DESC = "Prod/Transf Cabecera";
                public const string TRANSFORM_PRODUCTION_HEAD_WITH_AT = "@" + TRANSFORM_PRODUCTION_HEAD;

                public const string TRANSFORM_PRODUCTION_LINE = "ITPS_TRANSFPROD_LIN";
                public const string TRANSFORM_PRODUCTION_LINE_DESC = "Prod/Transf Detalle";
                public const string TRANSFORM_PRODUCTION_LINE_WITH_AT = "@" + TRANSFORM_PRODUCTION_LINE;

                public static class FIELDS_HEAD
                {
                    public const string STATUS = "ITPS_Status";
                    public const string ITEMCODE = "ITPS_ItemCode";
                    public const string QUANTITY = "ITPS_Qty";
                    public const string ENTRY_DOC_ENTRY = "ITPS_EntryDocEntry";
                    public const string EXIT_DOC_ENTRY = "ITPS_ExitDocEntry";
                    public const string ENTRY_REV_DOC_ENTRY = "ITPS_EntryRevDocEntry";
                    public const string EXIT_REV_DOC_ENTRY = "ITPS_ExitRevDocEntry";
                }

                public static class FIELDS_HEAD_DB
                {
                    public const string DOCENTRY = "DocEntry";
                    public const string STATUS = "U_" + FIELDS_HEAD.STATUS;
                    public const string ITEMCODE = "U_" + FIELDS_HEAD.ITEMCODE;
                    public const string QUANTITY = "U_" + FIELDS_HEAD.QUANTITY;
                    public const string ENTRY_DOC_ENTRY = "U_" + FIELDS_HEAD.ENTRY_DOC_ENTRY;
                    public const string EXIT_DOC_ENTRY = "U_" + FIELDS_HEAD.EXIT_DOC_ENTRY;
                    public const string ENTRY_REV_DOC_ENTRY = "U_" + FIELDS_HEAD.ENTRY_REV_DOC_ENTRY;
                    public const string EXIT_REV_DOC_ENTRY = "U_" + FIELDS_HEAD.EXIT_REV_DOC_ENTRY;
                }

                public static class FIELDS_LINE
                {
                    public const string SUBPRODUCT = "ITPS_SubItemCode";
                    public const string SUBPRODUCT_NAME = "ITPS_SubItemName";
                    public const string QUANTITY_OBTAINED = "ITPS_QtyObt";
                    public const string UNIT_MEASUREMENT = "ITPS_UoM";
                    public const string WAREHOUSE = "ITPS_WhsCode";
                    public const string LAST_PUR_PRICE = "ITPS_LastPurPrice";
                    public const string LAST_PUR_CUR = "ITPS_LastPurCur";
                    public const string PRICE = "ITPS_Price";
                    public const string CURRENT = "ITPS_Curr";
                    public const string BATCH_NUM = "ITPS_BatchNum";
                    public const string EXTDATE_BATCH = "ITPS_ExpDateBatch";
                    public const string MNFDATE_BATCH = "ITPS_MnfDateBatch";
                    public const string INDATE_BATCH = "ITPS_InDateBatch";
                }

                public static class FIELDS_LINE_DB
                {
                    public const string DOCENTRY = "DocEntry";
                    public const string LINENUM = "LineNum";
                    public const string SUBPRODUCT = "U_" + FIELDS_LINE.SUBPRODUCT;
                    public const string SUBPRODUCT_NAME = "U_" + FIELDS_LINE.SUBPRODUCT_NAME;
                    public const string QUANTITY_OBTAINED = "U_" + FIELDS_LINE.QUANTITY_OBTAINED;
                    public const string UNIT_MEASUREMENT = "U_" + FIELDS_LINE.UNIT_MEASUREMENT;
                    public const string WAREHOUSE = "U_" + FIELDS_LINE.WAREHOUSE;
                    public const string LAST_PUR_PRICE = "U_" + FIELDS_LINE.LAST_PUR_PRICE;
                    public const string LAST_PUR_CUR = "U_" + FIELDS_LINE.LAST_PUR_CUR;
                    public const string PRICE = "U_" + FIELDS_LINE.PRICE;
                    public const string CURRENT = "U_" + FIELDS_LINE.CURRENT;
                    public const string BATCH_NUM = "U_" + FIELDS_LINE.BATCH_NUM;
                    public const string EXTDATE_BATCH = "U_" + FIELDS_LINE.EXTDATE_BATCH;
                    public const string MNFDATE_BATCH = "U_" + FIELDS_LINE.MNFDATE_BATCH;
                    public const string INDATE_BATCH = "U_" + FIELDS_LINE.INDATE_BATCH;
                }

                public static class FIELDS_HEAD_DESC
                {
                    public const string STATUS = "Estado";
                    public const string ITEMCODE = "Código de Artículo";
                    public const string QUANTITY = "Cantidad Consumida";
                    public const string ENTRY_DOC_ENTRY = "Entrada DocEntry";
                    public const string EXIT_DOC_ENTRY = "Salida DocEntry";
                    public const string ENTRY_REV_DOC_ENTRY = "Reversión Entrada DocEntry";
                    public const string EXIT_REV_DOC_ENTRY = "Reversión Salida DocEntry";
                }

                public static class FIELDS_LINE_DESC
                {
                    public const string SUBPRODUCT = "Subproducto"; 
                    public const string SUBPRODUCT_NAME = "Nombre"; 
                    public const string QUANTITY_OBTAINED = "Cantidad Obtenida";
                    public const string UNIT_MEASUREMENT = "Unidad";
                    public const string WAREHOUSE = "Almacén";
                    public const string LAST_PUR_PRICE = "Precio (Últ. Compra)";
                    public const string LAST_PUR_CUR = "Moneda (Últ. Compra)";
                    public const string PRICE = "Precio (nuevo)";
                    public const string CURRENT = "Moneda (nuevo)";
                    public const string BATCH_NUM = "Número de Lote";
                    public const string EXTDATE_BATCH = "Fecha de Expiración del Lote";
                    public const string MNFDATE_BATCH = "Fecha de Fabricación del Lote";
                    public const string INDATE_BATCH = "Fecha de Ingreso del Lote";
                }
            }

            public static class UDO
            {
                public const string OBJECT_CODE = "ITPS_TRANSFPROD";
                public const string OBJECT_NAME = "Producción / Transformación";
            }

            public static class UID
            {
                public const string FORM = "UDO_F_ITPS_TRANSFPROD";
                public const string GRID = "0_U_G";
                public const string FOLDER = "0_U_FD";

                public static class HEADER
                {
                    public const string DOC_ENTRY = "0_U_E";
                    public const string CREATE_DATE = "12_U_E";
                    public const string STATUS = "20_U_Cb";
                    public const string CREATOR = "18_U_E";
                    public const string ITEM_CODE = "21_U_E";
                    public const string QUANTITY = "22_U_E";
                    public const string REMARK = "19_U_E";
                    public const string LOTE_LABEL = "Item_1";
                    public const string ENTRY_DOCENTRY = "Item_4";
                    public const string ENTRY_REV_DOCENTRY = "Item_6";
                    public const string EXIT_DOCENTRY = "Item_7";
                    public const string EXIT_REV_DOCENTRY = "Item_8";

                }

                public static class BUTTONS
                {
                    public const string CREATE = "1";
                    public const string CANCEL = "2";
                    public const string REVERT = "Item_3";
                    public const string LOTE_SELECT = "Item_0";
                    public const string CONFIRM_PROD = "Item_2";
                    public const string SHOW_DOCUMENTS = "Item_5";
                }

                /// <summary>
                /// Campo espejo de "Producto": el EditText original (HEADER.ITEM_CODE) está bound al
                /// UDF U_ITPS_ItemCode, vinculado a Artículos, y SAP le pone su propia ayuda de
                /// búsqueda, que no se puede filtrar ni reemplazar ("Invalid item" / el CFL del
                /// evento es el del UDO). Por eso se crea por código un EditText propio encima,
                /// bound a un UserDataSource y con un CFL de artículos (OITM) filtrado a los que
                /// tienen lista de materiales de producción (OITM."TreeType" = 'P', mismo criterio
                /// que ITPS_VW_TRANSFPROD_LOTES). El original queda oculto y el valor se sincroniza
                /// con el DBDataSource (ver FormReader).
                /// </summary>
                public static class CHOOSE_FROM_LIST
                {
                    public const string ITEM_CODE_MIRROR = "itpsArt";
                    public const string ITEM_CODE_LABEL = "21_U_S";
                    public const string USER_DATASOURCE = "itpsUArt";
                    public const string UID = "itpsCflA";
                    public const string OBJECT_TYPE = "4";
                    public const string ALIAS = "ItemCode";
                    public const string TREE_TYPE_ALIAS = "TreeType";
                    public const string TREE_TYPE_PRODUCTION = "P";
                }

                public static class GRID_COLUMNS
                {
                    public const string SUBPRODUCT = "C_0_1";
                    public const string SUBPRODUCT_NAME = "C_0_2";
                    public const string QUANTITY_OBTAINED = "C_0_3";
                    public const string UNIT_MEASUREMENT = "C_0_4";
                    public const string WAREHOUSE = "C_0_5";
                    public const string LAST_PUR_PRICE = "C_0_6";
                    public const string LAST_PUR_CUR = "C_0_7";
                    public const string PRICE = "C_0_8";
                    public const string CURRENT = "C_0_9";
                    public const string BATCH_NUM = "C_0_10";
                    public const string EXTDATE_BATCH = "C_0_11";
                    public const string MNFDATE_BATCH = "C_0_12";
                    public const string INDATE_BATCH = "C_0_13";
                }
            }

            /// <summary>
            /// Diferencia admitida entre la suma de la Cantidad Obtenida del detalle y la Cantidad
            /// Consumida de la cabecera (±5%).
            /// </summary>
            public const double TOLERANCIA_CANTIDAD_OBTENIDA = 0.05;

            public static class STAGING_STATUS
            {
                public const string DRAFT = "Borrador";
                public const string PENDING = "Pendiente";
                public const string PROCESSING = "Procesando";
                public const string COMPLETED = "Completado";
                public const string ERROR = "Error";
                public const string CANCELLED = "Cancelado";
                public const string REVERT = "Revertido";
            }

            public static class MESSAGES
            {
                public static string UDO_REGISTER_ERROR_PREFIX = $"Error registrando el UDO de {UDO.OBJECT_NAME}: ";

                public static string TABLE_HEAD_INFRA_ERROR_PREFIX = $"Error creando tabla {TABLES.TRANSFORM_PRODUCTION_HEAD}: ";
                public static string TABLE_LINE_INFRA_ERROR_PREFIX = $"Error creando tabla {TABLES.TRANSFORM_PRODUCTION_LINE}: ";

                public static string MENU_REGISTER_ERROR_PREFIX = "Error registrando el menú del addon: ";
                public static string CREATE_INVALID_CONSUMED_QTY = "La cantidad consumida debe ser un número mayor que cero.";
                public static string CREATE_QTY_OUT_OF_TOLERANCE = "La suma de la cantidad obtenida ({0}) no coincide con la cantidad consumida ({1}): se admite una diferencia de ±{2} (entre {3} y {4}).";
                public static string CREATE_NO_DETAIL_LINES = "Debe cargar al menos una línea de detalle (subproducto).";
                public static string CREATE_LINE_WITHOUT_BATCH = "La línea {0} (subproducto {1}) no tiene número de lote.";
                public static string CREATE_LINE_BATCH_NOT_MANAGED = "La línea {0} (subproducto {1}) no se maneja por lotes: deje el número de lote vacío.";
                public static string CREATE_LINE_INVALID_QTY = "La línea {0} (subproducto {1}): la cantidad obtenida debe ser mayor que cero.";
                public static string CREATE_LINE_WITHOUT_WHS = "La línea {0} (subproducto {1}) no tiene almacén.";
                public static string CREATE_LINE_WHS_NOT_FOUND = "La línea {0} (subproducto {1}): el almacén {2} no existe.";
                public static string CREATE_LINE_INVALID_PRICE = "La línea {0} (subproducto {1}): el precio nuevo debe ser mayor que cero.";
                public static string CREATE_LINE_CURRENCY_NOT_FOUND = "La línea {0} (subproducto {1}): la moneda {2} no existe en SAP.";
                public static string EXCHANGE_RATE_MISSING = "Falta el tipo de cambio de hoy para: {0}. Cárguelo en la ventana de tipos de cambio y vuelva a intentar.";
                public static string CREATE_LINE_WITHOUT_CURRENCY = "La línea {0} (subproducto {1}) no tiene moneda.";
                public static string CREATE_LINE_WITHOUT_DATE = "La línea {0} (subproducto {1}) no tiene {2}.";
                public static string CREATE_NO_ENTRY_LINES = "Debe haber al menos una línea de subproducto con lote y cantidad obtenida mayor que cero.";
                public static string CREATE_DOCS_FAILED_PENDING = "El documento se grabó en estado " + STAGING_STATUS.PENDING + " porque no se pudieron crear la Entrada/Salida de mercancía. Corrija el problema y use Confirmar producción.";
                public static string COMPLETE_UDO_UPDATE_ERROR = "No se pudo actualizar el documento a " + STAGING_STATUS.COMPLETED + ": se deshicieron la Entrada/Salida de mercancía.";
                public static string REFERENCE_DOC_NOT_FOUND = "No se encontró el documento {0} para vincularlo (Documentos referenciados).";
                public static string REFERENCE_UPDATE_ERROR = "No se pudo vincular el documento {0} (Documentos referenciados): ({1}) {2}";
                public static string CONFIRM_QUESTION = "¿Confirma la producción/transformación? Se generarán la Entrada y la Salida de mercancía y el documento pasará a " + STAGING_STATUS.COMPLETED + ".";
                public static string CONFIRM_NOT_SAVED = "Guarde el documento antes de confirmarlo.";
                public static string CONFIRM_ALREADY_DONE = "El documento ya tiene Entrada/Salida de mercancía: no se puede confirmar de nuevo.";
                public static string CONFIRM_NOT_PENDING = "Solo se puede confirmar un documento en estado " + STAGING_STATUS.PENDING + ". Estado actual: ";
                public static string CFL_ITEM_BUILD_ERROR_PREFIX = "Error creando el campo Producto con la lista de artículos con lista de materiales: ";

                public static string REVERT_NOT_COMPLETED = "Solo se puede revertir un documento en estado " + STAGING_STATUS.COMPLETED + ". Estado actual: ";
                public static string REVERT_ALREADY_DONE = "La producción/transformación ya fue revertida (existen documentos de reversión).";
                public static string REVERT_NOT_SAVED = "Guarde el documento antes de revertirlo.";
                public static string REVERT_UDO_UPDATE_ERROR = "No se pudo actualizar el estado del documento a " + STAGING_STATUS.REVERT + ".";
            }

        }
    }
}
