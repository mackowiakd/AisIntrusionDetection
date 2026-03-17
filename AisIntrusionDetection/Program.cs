using AisIntrusionDetection.Algorithms;
using AisIntrusionDetection.Algos;
using AisIntrusionDetection.Interop;
using AisIntrusionDetection.Models;
using System.Globalization;

namespace AisIntrusionDetection
{
    internal class Program
    {
        static void Main()
        {
            string dataFilePath = "NSL-KDD.arff"; // Ścieżka do pliku z danymi
            int maxRowsToLoad = 20000; // Maksymalna liczba wierszy do załadowania
            int featuresCount = 41; // Liczba cech (kolumn) do załadowania
            int detectorsToGenerate = 50; // Liczba detektorów do wygenerowania
            float detectorRadius = 0.5f; // Promień detektora (próg dopasowania)

            // 1. Wczytujemy WSZYSTKO przez  Parser C++
            DataLoader loader = new DataLoader(dataFilePath,maxRowsToLoad, featuresCount);
            List<Antigen> allData = loader.LoadData();

            // 2. PODZIAŁ DANYCH (np. 80% do nauki, 20% do testów)
            // W prawdziwym projekcie zrobilibyśmy losowy podział, tu dla przykładu bierzemy filtry:
            //List<Antigen> trainSet = allData.Where(antigen => antigen.Attack == false).ToList();
            List<Antigen> trainSet = allData.Where(a => a.Attack == false).Take(5000).ToList();
            List<Antigen> testSet = allData.Skip(5000).Take(2000).ToList();

            Console.WriteLine($"Wczytano {allData.Count} pakietów. Z tego {trainSet.Count} to ruch prawidłowy (Self).");

            // 3. Odpalamy trening TYLKO na czystych, zdrowych danych
            NegativeSelection nsa = new NegativeSelection();
            List<Detector> matureDetectors = nsa.GenerateDetectors(trainSet, featuresCount-1, detectorsToGenerate, detectorRadius);

            // 4. FAZA TESTOWANIA I OCENY MODELU
            ModelEvaluator evaluator = new ModelEvaluator();
            evaluator.Evaluate(matureDetectors, testSet);

            Console.WriteLine("\nKoniec działania programu.");
            Console.ReadLine();
        }

    }
}
