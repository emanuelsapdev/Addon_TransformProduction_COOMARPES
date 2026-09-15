using Addon_TransformProduction.Common;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Addon_TransformProduction.Services
{
    /// <summary>
    /// Infraestructura de la DI API: creación idempotente de UDTs, UDFs, vistas y registro del UDO.
    /// Todos los nombres de objetos SAP usan prefijo ITPS_ / itps.
    /// </summary>
    public class InfraDataService
    {
        public class ValidValueOption
        {
            public string Value { get; set; }
            public string Description { get; set; }
        }

        /// <summary>Crea una tabla de usuario (UDT) si todavía no existe.</summary>
        public static void CrearTablaUsuario(string name, string desc, BoUTBTableType type)
        {
            var oTableMd = (UserTablesMD)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.oUserTables);
            try
            {
                var exists = oTableMd.GetByKey(name);
                if (!exists)
                {
                    oTableMd.TableName = name;  // sin @
                    oTableMd.TableDescription = desc;
                    oTableMd.TableType = type;

                    int ret = oTableMd.Add();
                    //if (ret != 0)
                    //{
                    //    string err = ConnectionSDK.DIAPI.GetLastErrorDescription();
                    //    throw new Exception($"Error creando tabla {name}: {err}");
                    //}
                }
            }
            finally
            {
                if (oTableMd != null)
                {
                    try { Marshal.FinalReleaseComObject(oTableMd); }
                    catch { }
                    oTableMd = null;
                }
            }
        }

        /// <summary>Crea un campo de usuario (UDF) en la tabla indicada si todavía no existe.</summary>
        public static void CrearCampoUsuario(string tableName,
                                   string fieldName,
                                   string desc,
                                   BoFieldTypes type,
                                   int size = 0,
                                   BoFldSubTypes subType = BoFldSubTypes.st_None,
                                   string linkedTable = null,
                                   string linkedUDO = null,
                                   UDFLinkedSystemObjectTypesEnum linkedSystemObject = UDFLinkedSystemObjectTypesEnum.ulNone,
                                   IEnumerable<ValidValueOption> validValues = null,
                                   string defaultValue = null)
        {
            if (ExisteCampo(tableName, fieldName))
                return;

            var oFieldMd = (UserFieldsMD)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.oUserFields);

            try
            {
                oFieldMd.TableName = tableName;    // sin @ para tablas UDO / estándar
                oFieldMd.Name = fieldName;         // sin U_
                oFieldMd.Description = desc;
                oFieldMd.Type = type;
                oFieldMd.SubType = subType;

                if (size > 0)
                    oFieldMd.EditSize = size;

                if (!string.IsNullOrEmpty(linkedTable))
                {
                    oFieldMd.LinkedTable = linkedTable;
                }
                else if (!string.IsNullOrEmpty(linkedUDO))
                {
                    oFieldMd.LinkedUDO = linkedUDO;
                }
                else if (linkedSystemObject != UDFLinkedSystemObjectTypesEnum.ulNone)
                {
                    oFieldMd.LinkedSystemObject = linkedSystemObject;
                }

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    oFieldMd.DefaultValue = defaultValue;
                }

                if (validValues != null)
                {
                    var values = validValues
                        .Where(v => v != null && !string.IsNullOrWhiteSpace(v.Value))
                        .ToList();

                    for (int i = 0; i < values.Count; i++)
                    {
                        if (i > 0)
                            oFieldMd.ValidValues.Add();

                        oFieldMd.ValidValues.SetCurrentLine(i);
                        oFieldMd.ValidValues.Value = values[i].Value;
                        oFieldMd.ValidValues.Description = string.IsNullOrWhiteSpace(values[i].Description)
                            ? values[i].Value
                            : values[i].Description;
                    }
                }

                int ret = oFieldMd.Add();
                //if (ret != 0)
                //    throw new Exception($"Error creando campo {tableName}.U_{fieldName}: {ConnectionSDK.DIAPI.GetLastErrorDescription()}");
            }
            finally
            {
                if (oFieldMd != null)
                {
                    try { Marshal.FinalReleaseComObject(oFieldMd); }
                    catch { }
                    oFieldMd = null;
                }
            }
        }

        /// <summary>Indica si un UDF ya existe en la tabla (CUFD).</summary>
        private static bool ExisteCampo(string tableName, string fieldName)
        {
            var rs = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                rs.DoQuery($@"
                SELECT 1
                  FROM CUFD
                 WHERE ""TableID"" = '{tableName.Replace("'", "''")}'
                   AND ""AliasID"" = '{fieldName.Replace("'", "''")}'");

                return !rs.EoF;
            }
            finally
            {
                Marshal.ReleaseComObject(rs);
            }
        }

        /// <summary>Crea una vista HANA si todavía no existe (SYS.VIEWS).</summary>
        public static void CrearVistaSiNoExiste(string viewName, string viewBody)
        {
            var rs = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                rs.DoQuery($@"SELECT 1 FROM SYS.VIEWS WHERE ""VIEW_NAME"" = '{viewName}' AND ""SCHEMA_NAME"" = CURRENT_SCHEMA");

                if (!rs.EoF)
                    return;

                Marshal.ReleaseComObject(rs);
                rs = null;

                var rsCreate = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                try
                {
                    rsCreate.DoQuery($"CREATE VIEW \"{viewName}\" AS {viewBody}");
                }
                finally
                {
                    Marshal.ReleaseComObject(rsCreate);
                }
            }
            finally
            {
                if (rs != null)
                    Marshal.ReleaseComObject(rs);
            }
        }

        /// <summary>
        /// Registra un UDO de tipo Document (cabecera + una tabla hija de líneas) si todavía no
        /// existe. Las tablas UDT de cabecera y líneas deben existir de antemano (creadas con
        /// <see cref="CrearTablaUsuario"/>) — este método solo las asocia como UDO.
        /// </summary>
        /// <param name="objectCode">Object Code del UDO.</param>
        /// <param name="objectName">Nombre visible del UDO.</param>
        /// <param name="headerTableName">Tabla UDT de cabecera, sin "@".</param>
        /// <param name="lineTableName">Tabla UDT de líneas (hija), sin "@".</param>
        /// <param name="menuUID">
        /// UID del menú del addon que abre el formulario de este UDO. Se asocia vía
        /// <c>UserObjectsMD.Forms</c> para que SAP identifique ese menú como el punto de entrada del
        /// objeto. Opcional: si viene vacío, el UDO se registra igual pero sin esa asociación.
        /// </param>
        public static void RegistrarUdoSiNoExiste(string objectCode, string objectName, string headerTableName, string lineTableName, string menuUID = null)
        {
            var oUdoMd = (UserObjectsMD)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.oUserObjectsMD);
            try
            {
                if (oUdoMd.GetByKey(objectCode))
                    return; // ya registrado

                oUdoMd.Code = objectCode;
                oUdoMd.Name = objectName;
                oUdoMd.TableName = headerTableName; // sin "@"
                oUdoMd.ObjectType = BoUDOObjType.boud_Document;
                oUdoMd.CanCancel = BoYesNoEnum.tNO;
                oUdoMd.CanClose = BoYesNoEnum.tNO;
                oUdoMd.CanFind = BoYesNoEnum.tYES;
                oUdoMd.CanLog = BoYesNoEnum.tYES;

                // El formulario de este UDO lo diseñamos en B1 Studio y el addon lo carga desde su
                // .xml exportado — sin esto, SAP intenta generar/ofrecer además su propio formulario
                // automático genérico para el UDO.
                oUdoMd.CanCreateDefaultForm = BoYesNoEnum.tNO;

                oUdoMd.ChildTables.TableName = lineTableName; // sin "@"
                oUdoMd.ChildTables.Add();

                if (!string.IsNullOrEmpty(menuUID))
                {
                    oUdoMd.MenuUID = menuUID;
                }

                int ret = oUdoMd.Add();
                if (ret != 0)
                    throw new Exception($"Error registrando UDO {objectCode}: {ConnectionSDK.DIAPI.GetLastErrorDescription()}");
            }
            finally
            {
                if (oUdoMd != null)
                {
                    try { Marshal.FinalReleaseComObject(oUdoMd); }
                    catch { }
                    oUdoMd = null;
                }
            }
        }
    }
}