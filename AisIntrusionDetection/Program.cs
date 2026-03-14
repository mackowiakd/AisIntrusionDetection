using AisIntrusionDetection.Interop;

namespace AisIntrusionDetection
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine("Inicjalizacja systemu IDS...");

            string dummyRecord = "0,tcp,http,SF,181,5450"; // Przykład surowego wiersza z KDD
            float[] vector = new float[3]; // Zakładamy, że wyciągamy 3 cechy

            // Wywołanie C++ z C#
            NativeMethods.ProcessNetworkRecord(dummyRecord, vector, vector.Length);

            Console.WriteLine($"Przetworzone dane przez C++: [{vector[0]}, {vector[1]}, {vector[2]}]");
        }
    }
}
