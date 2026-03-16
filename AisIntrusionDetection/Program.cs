using AisIntrusionDetection.Algorithms;
using AisIntrusionDetection.Interop;
using AisIntrusionDetection.Models;

namespace AisIntrusionDetection
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine("--- SYSTEM DETEKCJI INTRUZÓW (BIAI) ---");
            Console.WriteLine("Tryb: FAZA UCZENIA (Sztuczne dane)\n");

            // 1. Definiujemy "Sztuczny" zbiór zdrowego ruchu (Self set)
            // Zakładamy, że wyciągamy z pakietu 3 cechy (np. czas trwania, protokół, liczba bajtów)
            List<float[]> mockSelfSet = new List<float[]>
            {
                new float[] { 0.2f, 0.2f, 0.2f },
                new float[] { 0.25f, 0.21f, 0.19f },
                new float[] { 0.8f, 0.8f, 0.8f },
                new float[] { 0.78f, 0.82f, 0.85f }
            };

            // 2. Parametry algorytmu
            int featuresCount = 3;           // Mamy 3 kolumny/cechy
            int detectorsToGenerate = 10;    // Chcemy wyhodować 10 detektorów
            float detectorRadius = 0.15f;    // Promień detekcji (jak blisko musi być pakiet, by podnieść alarm)

            // 3. Uruchamiamy Negative Selection Algorithm
            NegativeSelection nsa = new NegativeSelection();
            List<Detector> matureDetectors = nsa.GenerateDetectors(mockSelfSet, featuresCount, detectorsToGenerate, detectorRadius);

            // 4. Wyniki
            Console.WriteLine("\nGOTOWE DETEKTORY (Współrzędne x, y, z):");
            for (int i = 0; i < matureDetectors.Count; i++)
            {
                var coords = matureDetectors[i].Coordinates;
                Console.WriteLine($"Detektor {i + 1:D2}: [{coords[0]:F3}, {coords[1]:F3}, {coords[2]:F3}]");
            }

            Console.ReadLine();
        }
    }
}
