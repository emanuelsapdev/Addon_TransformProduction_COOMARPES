namespace Addon_TransformProduction.Forms.ActualizacionCostos
{
    public partial class ActualizacionCostosFrm
    {
        /// <summary>UIDs de los controles propios de este formulario (no van en Common/Constants.cs
        /// porque son de uso exclusivo de esta pantalla).
        /// Todos los valores se mantienen en un máximo de 10 caracteres.</summary>
        /// <remarks>
        /// Estos UIDs tienen que coincidir EXACTO con los <c>uid</c> de cada item/columna en
        /// Forms/ActualizacionCostos/B1 Studio/ActualizacionCostosArtsFrm.xml (el formulario ya no
        /// se arma por código: se carga ese .xml, ver ActualizacionCostosFrm.UIBuilder.BuildForm).
        /// Si el formulario se vuelve a exportar/editar desde B1 Studio, revisar que estos valores
        /// sigan coincidiendo — en particular los UIDs de columna (C_0_1..C_0_9), que B1 Studio
        /// autogenera y pueden cambiar de orden si se agregan/borran/reordenan columnas en el diseño.
        /// </remarks>
        private static class UIDs
        {
            public const string BtnSelectExcel = "itpsBtnXls";
            public const string LblExcelPath = "itpsLblPth";

            public const string MatrixItems = "itpsMtxItm";

            
            public const string ColItemCode = "C_0_2";
            public const string ColItemName = "C_0_3";
            public const string ColWhsCode = "C_0_4";
            public const string ColWhsName = "C_0_5";
            public const string ColCurrentCost = "C_0_6";
            public const string ColNewCost = "C_0_7";
            public const string ColStatus = "C_0_8";
            public const string ColError = "C_0_9";

            public const string LblWarehouse = "itpsLblWhs";

            /// <summary>Selector único de almacén a procesar. En el .xml es un EditText (type="16",
            /// NO un ComboBox) con un ChooseFromList propio enlazado (ver ChooseFromListCollection
            /// del .xml: <c>ChooseFromList UniqueID="CFL_0" ObjectType="64"</c> — 64 = Warehouses),
            /// databind directo a "@ITPS_COSTUPD_HEAD".U_ITPS_WhsCode y
            /// <c>ChooseFromListAlias="WhsCode"</c>: SAP completa el campo solo con el código
            /// elegido en el CFL, no hace falta poblar nada por código (a diferencia de la versión
            /// anterior con ComboBox, que sí necesitaba `PopulateWarehouseCombo` con
            /// `WarehouseRepository.GetAll()`).</summary>
            public const string EdtWarehouse = "itpsEdtWhs";

            /// <summary>StaticText (sin databind) donde se muestra el nombre del almacén elegido en
            /// <see cref="EdtWarehouse"/> — se completa por código, no lo puebla SAP solo (a
            /// diferencia del código en sí, que sí lo completa el ChooseFromList). Ver
            /// RefreshWarehouseNameLabel en ActualizacionCostosFrm.FormReader.cs.</summary>
            public const string LblWhsName = "lblWhsName";

            public const string LblSchedDate = "itpsLblDat";

            /// <summary>Campo de fecha programada. En el .xml es un EditText enlazado (databind)
            /// directo al UDF "@ITPS_COSTUPD_HEAD".U_ITPS_SchedDate — a diferencia de la versión
            /// anterior de este formulario (construida a mano por código), ya no hace falta un
            /// UserDataSource intermedio para bindearlo.</summary>
            public const string EdtSchedDate = "itpsDsDate";

            public const string LblSchedTime = "itpsLblTim";
            public const string EdtSchedTime = "itpsEdtTim";

            /// <summary>Botón "Confirmar y Programar" (caption en el .xml, aunque en B1 Studio ya
            /// se lo renombró a "Procesar y programar"). Cambió de uid propio ("itpsBtnCnf") a "1"
            /// al reexportar desde B1 Studio — B1 Studio le asigna uids numéricos autogenerados a
            /// los botones estándar Aceptar/Cancelar en vez de mantener uids custom. Si no se
            /// actualiza acá para que coincida, <c>HandleItemPressed</c> (ActualizacionCostosFrm.cs)
            /// nunca matchea <c>pVal.ItemUID</c> contra este valor y el botón queda sin hacer nada al
            /// hacer clic.</summary>
            public const string BtnConfirm = "1";

            /// <summary>Botón "Cerrar". Mismo caso que <see cref="BtnConfirm"/>: cambió de
            /// "itpsBtnCnc" a "2" al reexportar desde B1 Studio.</summary>
            public const string BtnCancel = "2";
        }
    }
}
