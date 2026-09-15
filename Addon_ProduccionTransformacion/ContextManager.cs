using Addon_TransformProduction.Models;
using System.Collections.Generic;

namespace Addon_TransformProduction
{
    public static class ContextManager
    {
        private static readonly Dictionary<string, TransformProductionContext> _contexts = new Dictionary<string, TransformProductionContext>();

        /// <summary>Obtiene el contexto de la empresa/documento indicado, creándolo si no existe.</summary>
        public static TransformProductionContext ObtenerOCrear(string TypeCount)
        {
            if (!_contexts.ContainsKey(TypeCount))
                _contexts[TypeCount] = new TransformProductionContext();

            return _contexts[TypeCount];
        }

        /// <summary>Obtiene el contexto asociado al FormUID indicado, o null si no existe.</summary>
        public static TransformProductionContext Obtener(string formUID)
        {
            _contexts.TryGetValue(formUID, out var ctx);
            return ctx;
        }

        /// <summary>Elimina el contexto asociado al FormUID indicado (al cerrar el formulario).</summary>
        public static void Eliminar(string formUID)
        {
            _contexts.Remove(formUID);
        }
    }
}