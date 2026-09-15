using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Addon_TransformProduction.Tools
{
    /// <summary>
    /// Utilidades de liberación de objetos COM y gestión de memoria para la DI/UI API de SAP B1.
    /// </summary>
    public class MarshalGC
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);

        /// <summary>Libera un único objeto COM si es RCW válido.</summary>
        public static void LiberarComObject(object obj)
        {
            if (obj != null && Marshal.IsComObject(obj))
            {
                Marshal.ReleaseComObject(obj);
            }
        }

        /// <summary>Libera varios objetos COM pasados como parámetros.</summary>
        public static void LiberarComObjects(params object[] objects)
        {
            LiberarComObjectsValidos(objects);
        }

        /// <summary>
        /// Libera los objetos COM evitando forzar GC/Finalizers/WorkingSet trimming durante
        /// eventos de UI, que suele provocar inestabilidad con objetos COM de SAP B1
        /// (fallas RPC / RCWs inválidos).
        /// </summary>
        public static void LiberarComObjectsSuavemente(params object[] objects)
        {
            LiberarComObjectsValidos(objects);
        }

        /// <summary>Fuerza la recolección de memoria y recorta el working set del proceso.</summary>
        public static void MinimizarMemoria()
        {
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (UIntPtr)0xFFFFFFFF, (UIntPtr)0xFFFFFFFF);
        }

        private static bool LiberarComObjectsValidos(params object[] objects)
        {
            bool release = false;
            int index = 0;
            foreach (var obj in objects)
            {
                if (obj != null && Marshal.IsComObject(obj))
                {
                    Marshal.ReleaseComObject(obj);
                    objects[index] = null;
                    release = true;
                }
            }

            return release;
        }
    }
}