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
            Console.WriteLine("Tryb: FAZA UCZENIA (Obiekty Antigen + Wielowątkowość)\n");

            // 1. Zbiór mockowanych Antygenów (Teraz z etykietami!)
            List<Antigen> mockSelfSet = new List<Antigen>
            {
                new Antigen(new float[] { 0.2f, 0.2f, 0.2f }, "normal"),
                new Antigen(new float[] { 0.25f, 0.21f, 0.19f }, "normal"),
                new Antigen(new float[] { 0.8f, 0.8f, 0.8f }, "normal"),
                new Antigen(new float[] { 0.78f, 0.82f, 0.85f }, "normal")
            };

            int featuresCount = 3;
            int detectorsToGenerate = 10;
            float detectorRadius = 0.15f;

            // 2. Odpalenie NSA
            NegativeSelection nsa = new NegativeSelection();
            List<Detector> matureDetectors = nsa.GenerateDetectors(mockSelfSet, featuresCount, detectorsToGenerate, detectorRadius);

            // 3. Wypisanie wyników
            Console.WriteLine("\nGOTOWE DETEKTORY:");
            for (int i = 0; i < matureDetectors.Count; i++)
            {
                var coords = matureDetectors[i].Coordinates;
                Console.WriteLine($"Detektor {i + 1:D2}: [{coords[0]:F3}, {coords[1]:F3}, {coords[2]:F3}]");
            }

            Console.ReadLine();
        }
    
}
}
