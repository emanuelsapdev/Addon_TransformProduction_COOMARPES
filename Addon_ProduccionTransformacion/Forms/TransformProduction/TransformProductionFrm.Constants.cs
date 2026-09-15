namespace Addon_TransformProduction.Forms.TransformProduction
{
    partial class TransformProductionFrm
    {
        public static class CONSTANTS
        {
            public const string FORM_TYPE = "UDO_FT_ITPS_TRANSFPROD";

            public const string MENU_UID = "ITPS_TRANSFPROD_MENU";

            public const string PARENT_TOOLS_MENU_UID = "43520";
            public const string PARENT_INVENTORY_MENU_UID = "3072";

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
                }

                public static class FIELDS_HEAD_DB
                {
                    public const string DOCENTRY = "DocEntry";
                    public const string STATUS = "U_" + FIELDS_HEAD.STATUS;
                    public const string ITEMCODE = "U_" + FIELDS_HEAD.ITEMCODE;
                    public const string QUANTITY = "U_" + FIELDS_HEAD.QUANTITY;
                }

                public static class FIELDS_LINE
                {
                    public const string SUBPRODUCT = "ITPS_SubItemCode";
                    public const string SUBPRODUCT_NAME = "ITPS_SubItemName";
                    public const string QUANTITY_OBTAINED = "ITPS_QtyObt";
                    public const string UNIT_MEASUREMENT = "ITPS_UoM";
                    public const string LAST_PUR_PRICE = "ITPS_LastPurPrice";
                    public const string PRICE = "ITPS_Price";
                }

                public static class FIELDS_LINE_DB
                {
                    public const string DOCENTRY = "DocEntry";
                    public const string LINENUM = "LineNum";
                    public const string SUBPRODUCT = "U_" + FIELDS_LINE.SUBPRODUCT;
                    public const string SUBPRODUCT_NAME = "U_" + FIELDS_LINE.SUBPRODUCT_NAME;
                    public const string QUANTITY_OBTAINED = "U_" + FIELDS_LINE.QUANTITY_OBTAINED;
                    public const string UNIT_MEASUREMENT = "U_" + FIELDS_LINE.UNIT_MEASUREMENT;
                    public const string LAST_PUR_PRICE = "U_" + FIELDS_LINE.LAST_PUR_PRICE;
                    public const string PRICE = "U_" + FIELDS_LINE.PRICE;
                }

                public static class FIELDS_HEAD_DESC
                {
                    public const string STATUS = "Estado";
                    public const string ITEMCODE = "Código de Artículo";
                    public const string QUANTITY = "Cantidad Consumida";
                }

                public static class FIELDS_LINE_DESC
                {
                    public const string SUBPRODUCT = "Subproducto"; 
                    public const string SUBPRODUCT_NAME = "Nombre"; 
                    public const string QUANTITY_OBTAINED = "Cantidad Obtenida";
                    public const string UNIT_MEASUREMENT = "Unidad";
                    public const string LAST_PUR_PRICE = "Precio (Últ. Compra)";
                    public const string PRICE = "Precio";
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

                }

                public static class BUTTONS
                {
                    public const string CREATE = "1";
                    public const string CANCEL = "2";
                    public const string REVERT = "Item_3";
                    public const string LOTE_SELECT = "Item_0";
                }

                public static class GRID_COLUMNS
                {
                    public const string SUBPRODUCT = "C_0_1";
                    public const string SUBPRODUCT_NAME = "C_0_2";
                    public const string QUANTITY_OBTAINED = "C_0_3";
                    public const string UNIT_MEASUREMENT = "C_0_4";
                    public const string LAST_PUR_PRICE = "C_0_5";
                    public const string PRICE = "C_0_6";
                }
            }

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
                public static string FORM_OPEN_ERROR_PREFIX = "Error abriendo un documento nuevo de producción/transformación: ";
                public static string FORM_INIT_ERROR_PREFIX = "Error inicializando el formulario de producción/transformación: ";
            }

        }
    }
}
