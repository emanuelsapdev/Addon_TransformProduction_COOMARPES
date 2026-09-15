using Addon_TransformProduction.Configuration;

namespace Addon_TransformProduction.Common
{
    /// <summary>
    /// Ejecuta tareas de inicialización al arranque del addon:
    /// creación de infraestructura (tablas UDT, campos UDF), registro de UDOs,
    /// y carga de configuraciones.
    /// </summary>
    public class ExecutionsApp
    {
        public ExecutionsApp()
        {
            // Crear infraestructura (tablas y campos UDF) si no existen.
            //new ConfigsDevelopmentInfra().ProcessInfrastructure();
            new TransformProductionInfra().ProcessInfrastructure();
            new DocsMarketingInfra().ProcessInfrastructure();

            // Registrar menús del addon (idempotente: se saltan si ya existen)
            Forms.TransformProduction.TransformProductionFrm.RegistrarMenus();
        }
    }
}