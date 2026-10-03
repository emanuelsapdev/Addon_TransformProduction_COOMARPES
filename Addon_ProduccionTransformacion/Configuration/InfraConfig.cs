using Addon_TransformProduction.Forms.TransformProduction;
using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Addon_TransformProduction.Configuration
{
    /// <summary>
    /// Infraestructura de inicialización: crea tablas UDT, campos UDF
    /// y siembra datos de configuración en @CONFIGS_DEVELOPMENT.
    /// </summary>
    public class ConfigsDevelopmentInfra
    {
        /// <summary>Ejecuta la creación de infraestructura de configuraciones (tabla + campos).</summary>
        public void ProcessInfrastructure()
        {
            CrearTablaConfiguracionesDesarrollo();
        }

        private void CrearTablaConfiguracionesDesarrollo()
        {
            //string table = "CONFIGS_DEVELOPMENT";
            //try
            //{
            //    // Crear tabla de configuraciones
            //    InfraDataService.CrearTablaUsuario(table, "Configuraciones de desarrollos", BoUTBTableType.bott_NoObject);

            //    // Crear campos de la tabla
            //    InfraDataService.CrearCampoUsuario($"@{table}", "ITPS_Prop", "Propiedad", BoFieldTypes.db_Alpha, 30);
            //    InfraDataService.CrearCampoUsuario($"@{table}", "ITPS_Value", "Valor", BoFieldTypes.db_Memo);
            //    InfraDataService.CrearCampoUsuario($"@{table}", "ITPS_Integration", "Nombre Integración", BoFieldTypes.db_Alpha, 40);
            //    InfraDataService.CrearCampoUsuario($"@{table}", "ITPS_Author", "Desarrollador", BoFieldTypes.db_Alpha, 40);
            //    InfraDataService.CrearCampoUsuario($"@{table}", "ITPS_UpdateDate", "Fecha modificación", BoFieldTypes.db_Date);
            //    InfraDataService.CrearCampoUsuario($"@{table}", "ITPS_DescriptionProp", "Descripción de propiedad", BoFieldTypes.db_Memo);
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception(Constants.Messages.ConfigInfraErrorPrefix + ex.Message, ex);
            //}
        }
    }

    public class TransformProductionInfra
    {
        public void ProcessInfrastructure()
        {
            CrearTablaCabecera();
            CrearTablaLineas();
            RegistrarUdo();
        }

        private void RegistrarUdo()
        {
            try
            {
                InfraDataService.RegistrarUdoSiNoExiste(
                  TransformProductionFrm.CONSTANTS.UDO.OBJECT_CODE,
                  TransformProductionFrm.CONSTANTS.UDO.OBJECT_NAME,
                  TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD,
                  TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE,
                  TransformProductionFrm.CONSTANTS.MENU_UID);
            }
            catch (Exception ex)
            {
                throw new Exception(TransformProductionFrm.CONSTANTS.MESSAGES.UDO_REGISTER_ERROR_PREFIX + ex.Message, ex);
            }
        }

        private void CrearTablaCabecera()
        {
            try
            {
                InfraDataService.CrearTablaUsuario(
                    name: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_DESC,
                    type: BoUTBTableType.bott_Document);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD.STATUS,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD_DESC.STATUS,
                    type: BoFieldTypes.db_Alpha,
                    size: 20,
                    validValues: ObtenerValoresValidosDesdeTipo(typeof(TransformProductionFrm.CONSTANTS.STAGING_STATUS)),
                    defaultValue: TransformProductionFrm.CONSTANTS.STAGING_STATUS.PENDING);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD.ITEMCODE,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD_DESC.ITEMCODE,
                    type: BoFieldTypes.db_Alpha,
                    linkedSystemObject: UDFLinkedSystemObjectTypesEnum.ulItems);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD.QUANTITY,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD_DESC.QUANTITY,
                    type: BoFieldTypes.db_Float,
                    subType: BoFldSubTypes.st_Quantity);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD.ENTRY_DOC_ENTRY,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD_DESC.ENTRY_DOC_ENTRY,
                    type: BoFieldTypes.db_Numeric,
                    size: 11);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD.EXIT_DOC_ENTRY,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD_DESC.EXIT_DOC_ENTRY,
                    type: BoFieldTypes.db_Numeric,
                    size: 11);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD.ENTRY_REV_DOC_ENTRY,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD_DESC.ENTRY_REV_DOC_ENTRY,
                    type: BoFieldTypes.db_Numeric,
                    size: 11);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD.EXIT_REV_DOC_ENTRY,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_HEAD_DESC.EXIT_REV_DOC_ENTRY,
                    type: BoFieldTypes.db_Numeric,
                    size: 11);
            }
            catch (Exception ex)
            {
                throw new Exception(TransformProductionFrm.CONSTANTS.MESSAGES.TABLE_HEAD_INFRA_ERROR_PREFIX + ex.Message, ex);
            }
        }

        private void CrearTablaLineas()
        {
            try
            {
                InfraDataService.CrearTablaUsuario(
                    name: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_DESC,
                    type: BoUTBTableType.bott_DocumentLines);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.SUBPRODUCT,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.SUBPRODUCT,
                    type: BoFieldTypes.db_Alpha,
                    linkedSystemObject: UDFLinkedSystemObjectTypesEnum.ulItems);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.SUBPRODUCT_NAME,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.SUBPRODUCT_NAME,
                    type: BoFieldTypes.db_Alpha,
                    size: 100);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.QUANTITY_OBTAINED,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.QUANTITY_OBTAINED,
                    type: BoFieldTypes.db_Float,
                    subType: BoFldSubTypes.st_Quantity);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.UNIT_MEASUREMENT,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.UNIT_MEASUREMENT,
                    type: BoFieldTypes.db_Alpha,
                    size: 20);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.WAREHOUSE,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.WAREHOUSE,
                    type: BoFieldTypes.db_Alpha,
                    linkedSystemObject: UDFLinkedSystemObjectTypesEnum.ulWarehouses);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.LAST_PUR_PRICE,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.LAST_PUR_PRICE,
                    type: BoFieldTypes.db_Float,
                    subType: BoFldSubTypes.st_Price);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.LAST_PUR_CUR,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.LAST_PUR_CUR,
                    type: BoFieldTypes.db_Alpha,
                    size: 10);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.PRICE,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.PRICE,
                    type: BoFieldTypes.db_Float,
                    subType: BoFldSubTypes.st_Price);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.CURRENT,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.CURRENT,
                    type: BoFieldTypes.db_Alpha,
                    size: 10);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.BATCH_NUM,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.BATCH_NUM,
                    type: BoFieldTypes.db_Alpha,
                    size: 50);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.EXTDATE_BATCH,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.EXTDATE_BATCH,
                    type: BoFieldTypes.db_Date);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.MNFDATE_BATCH,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.MNFDATE_BATCH,
                    type: BoFieldTypes.db_Date);

                InfraDataService.CrearCampoUsuario(
                    tableName: TransformProductionFrm.CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT,
                    fieldName: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE.INDATE_BATCH,
                    desc: TransformProductionFrm.CONSTANTS.TABLES.FIELDS_LINE_DESC.INDATE_BATCH,
                    type: BoFieldTypes.db_Date);
            }
            catch (Exception ex)
            {
                throw new Exception(TransformProductionFrm.CONSTANTS.MESSAGES.TABLE_LINE_INFRA_ERROR_PREFIX + ex.Message, ex);
            }
        }

        /// <summary>
        /// Obtiene una lista de opciones válidas a partir del tipo `STAGING_STATUS`.
        /// Soporta tanto `enum` como clases con campos `public static` (const/readonly).
        /// Cada opción usa el mismo texto para Value y Description.
        /// </summary>
        private static List<InfraDataService.ValidValueOption> ObtenerValoresValidosDesdeTipo(Type type)
        {
            var list = new List<InfraDataService.ValidValueOption>();

            if (type == null)
            {
                return list;
            }

            if (type.IsEnum)
            {
                foreach (var name in Enum.GetNames(type))
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    list.Add(new InfraDataService.ValidValueOption
                    {
                        Value = name,
                        Description = name
                    });
                }

                return list;
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var f in fields)
            {
                object raw;
                try
                {
                    raw = f.GetValue(null);
                }
                catch
                {
                    continue;
                }

                if (raw == null)
                    continue;

                var text = raw.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                list.Add(new InfraDataService.ValidValueOption
                {
                    Value = text,
                    Description = text
                });
            }

            return list;
        }
    }

    public class DocsMarketingInfra
    {
        public void ProcessInfrastructure()
        {
            CrearCamposUdfLineasDocumentos();
        }

        /// <summary>
        /// Crea los UDFs de vinculación con el UDO ITPS_TRANSFPROD en las tablas
        /// estándar de documentos de marketing (IGN1 - Good Receipt PO, IGE1 - Good Issue).
        /// </summary>
        public void CrearCamposUdfLineasDocumentos()
        {
            CrearCampoVincularUdo(
                CONSTANTS_GLOBALS.DOCS_MARKETING.TABLES.IGN1,
                CONSTANTS_GLOBALS.DOCS_MARKETING.UDFs.TransformProductionEntry,
                "TP Interno",
                BoFieldTypes.db_Alpha,
                linkedUDO: TransformProductionFrm.CONSTANTS.UDO.OBJECT_CODE);

            CrearCampoNumerico(
                CONSTANTS_GLOBALS.DOCS_MARKETING.TABLES.IGN1,
                CONSTANTS_GLOBALS.DOCS_MARKETING.UDFs.TransformProductionLineId,
                "TP Line ID",
                size: 11);

            CrearCampoVincularUdo(
                CONSTANTS_GLOBALS.DOCS_MARKETING.TABLES.IGE1,
                CONSTANTS_GLOBALS.DOCS_MARKETING.UDFs.TransformProductionEntry,
                "TP Interno",
                BoFieldTypes.db_Alpha,
                linkedUDO: TransformProductionFrm.CONSTANTS.UDO.OBJECT_CODE);

            CrearCampoNumerico(
                CONSTANTS_GLOBALS.DOCS_MARKETING.TABLES.IGE1,
                CONSTANTS_GLOBALS.DOCS_MARKETING.UDFs.TransformProductionLineId,
                "TP Line ID",
                size: 11);
        }

        private void CrearCampoVincularUdo(string tabla, string campo, string desc, BoFieldTypes type, string linkedUDO)
        {
            InfraDataService.CrearCampoUsuario(
                tableName: tabla,
                fieldName: campo,
                desc: desc,
                type: type,
                linkedUDO: linkedUDO);
        }

        private void CrearCampoNumerico(string tabla, string campo, string desc, int size)
        {
            InfraDataService.CrearCampoUsuario(
                tableName: tabla,
                fieldName: campo,
                desc: desc,
                type: BoFieldTypes.db_Numeric,
                size: size);
        }
    }
}