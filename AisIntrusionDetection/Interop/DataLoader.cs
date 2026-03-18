using AisIntrusionDetection.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static AisIntrusionDetection.Interop.NativeMethods;

namespace AisIntrusionDetection.Interop
{
    public class DataLoader
    {
    
        public string filePath { get; private set; }
        public int maxRows = 1000; // Maksymalna liczba wierszy do wczytania (dla bezpieczeństwa)
        public int featuresCount = 40; // Liczba cech + 1 (dla etykiety)
        public int rowsLoaded = 0; // Licznik faktycznie przetworzonych wierszy (do debugowania)


        public DataLoader(string fp, int r, int fc){ 
            this.filePath = fp;
            this.maxRows = r;
            this.featuresCount = fc;    
        }
          
            
      
        public List<Antigen> LoadData()
        {
            Console.WriteLine($"[DataLoader] Alokacja pamięci dla {maxRows} wierszy...");

            // 1. ALOKACJA W C#: Tworzymy gigantyczną płaską tablicę
            // Jeśli mamy 1000 wierszy po 40 cech, robimy tablicę na 40 000 elementów.
            float[] flatArray = new float[maxRows * featuresCount];

            // 2. MAGIA P/INVOKE: Przekazujemy tablicę do C++
            // C# wstrzymuje oddech, wysyła wskaźnik do C++, C++ mieli plik i wpisuje liczby
            rowsLoaded = NativeMethods.LoadAndParseDataset(filePath, flatArray, maxRows, featuresCount);

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
                float[] features = new float[featuresCount - 1]; // -1, bo ostatnią kolumną będzie nasz Attack!

                // Kopiujemy jeden wiersz z płaskiej tablicy do małej tablicy cech
                Array.Copy(flatArray, i * featuresCount, features, 0, featuresCount - 1);

                // SPOSÓB NA ETYKIETY:
                // Załóżmy, że umówiłaś się ze swoim C++, że OSTATNIA LICZBA w wierszu 
                // to zakodowana etykieta (np. 0.0f to "normal", a 1.0f to "Attack")
                // Pobieramy wartość liczbową etykiety z tablicy
                float labelValue = flatArray[(i * featuresCount) + (featuresCount - 1)];

                // Tłumaczymy to na bool: jeśli wartość to 1.0f, to jest to atak (true)
                bool attack = (labelValue == 1.0f);


                dataset.Add(new Antigen(features, attack));
            }

            return dataset;
        }

       
    }
}
