using AisIntrusionDetection.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AisIntrusionDetection.Interop
{
    public class DataLoader
    {
        // Importujemy naszą funkcję z C++
        [DllImport("TrafficParserCpp.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int LoadAndParseDataset(
            string filePath,
            [Out] float[] outputArray, // [Out] podpowiada kompilatorowi, że C++ będzie tu wpisywać dane
            int maxRows,
            int featuresCount);

        // Elegancka metoda dla reszty programu w C#
        public List<Antigen> LoadData(string filePath, int maxRows, int featuresCount)
        {
            Console.WriteLine($"[DataLoader] Alokacja pamięci dla {maxRows} wierszy...");

            // 1. ALOKACJA W C#: Tworzymy gigantyczną płaską tablicę
            // Jeśli mamy 1000 wierszy po 40 cech, robimy tablicę na 40 000 elementów.
            float[] flatArray = new float[maxRows * featuresCount];

            // 2. MAGIA P/INVOKE: Przekazujemy tablicę do C++
            // C# wstrzymuje oddech, wysyła wskaźnik do C++, C++ mieli plik i wpisuje liczby
            int rowsLoaded = LoadAndParseDataset(filePath, flatArray, maxRows, featuresCount);

            if (rowsLoaded <= 0)
            {
                Console.WriteLine("[DataLoader] Błąd! C++ nie wczytało żadnych danych.");
                return new List<Antigen>();
            }

            Console.WriteLine($"[DataLoader] C++ przetworzyło {rowsLoaded} wierszy. Konwersja na obiekty...");

            // 3. KONWERSJA na eleganckie obiekty Antigen
            List<Antigen> dataset = new List<Antigen>(rowsLoaded);

            for (int i = 0; i < rowsLoaded; i++)
            {
                float[] features = new float[featuresCount - 1]; // -1, bo ostatnią kolumną będzie nasz TrueLabel!

                // Kopiujemy jeden wiersz z płaskiej tablicy do małej tablicy cech
                Array.Copy(flatArray, i * featuresCount, features, 0, featuresCount - 1);

                // SPOSÓB NA ETYKIETY:
                // Załóżmy, że umówiłaś się ze swoim C++, że OSTATNIA LICZBA w wierszu 
                // to zakodowana etykieta (np. 0.0f to "normal", a 1.0f to "attack")
                float labelCode = flatArray[(i * featuresCount) + (featuresCount - 1)];
                string label = (labelCode == 0.0f) ? "normal" : "attack";

                dataset.Add(new Antigen(features, label));
            }

            return dataset;
        }
    }
}
