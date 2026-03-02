using System;
using System.Runtime.InteropServices;

namespace B1TuneUp.Utils
{
    public static class ComObjectManager
    {
        /// <summary>
        /// Libera un objeto COM de forma segura.
        /// </summary>
        public static void Release(object obj) // Updated method signature
        {
            if (obj != null && Marshal.IsComObject(obj))
            {
                try
                {
                    Marshal.ReleaseComObject(obj);
                }
                catch { }
                finally
                {
                    obj = null;
                }
            }
        }

        /// <summary>
        /// Libera múltiples objetos COM.
        /// </summary>
        public static void ReleaseAll(params object[] objects)
        {
            foreach (var obj in objects)
            {
                Release(obj);
            }
        }

        // Overload to accept dynamic/COM arrays from SDK
        public static void Release(object obj, bool ignoreNull) { Release(obj); }
    }
}
