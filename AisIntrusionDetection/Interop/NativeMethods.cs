using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AisIntrusionDetection.Interop
{
    public static class NativeMethods
    {
        // --- KONFIGURACJA ŚCIEŻEK ---
        // Definiujemy stałe wewnątrz klasy. Dzięki temu są widoczne dla DllImport.

#if DEBUG
        // Ścieżki dla trybu DEBUG
        private const string CppPath = @"C:\Users\Dominika\source\repos\JA\AisIntrusionDetection\x64\Debug\TrafficParser.dll";

#else
        // Ścieżki dla trybu RELEASE (obok siebie z plikiem .exe)
        private const string CppPath = "TrafficParserCpp.dll"
       
#endif

        // Nazwa DLL musi się zgadzać z nazwą skompilowanego pliku C++
        // CallingConvention.Cdecl jest standardem dla C++
        [DllImport(CppPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessNetworkRecord(string rawRecord, float[] outputVector, int vectorSize);
    }
}
