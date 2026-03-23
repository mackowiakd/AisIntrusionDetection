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
            //string path= Directory.GetCurrentDirectory(); + filename - na przylosz sciezka wzgledna
            string dataFilePath = @"C:\Users\Dominika\source\repos\JA\AisIntrusionDetection\KDDTrain+_20Percent.arff"; // Ścieżka do pliku z danymi
            int maxRowsToLoad = 20000; // Maksymalna liczba wierszy do załadowania
            int featuresCount = 42; // Liczba cech (kolumn) do załadowania
            int detectorsToGenerate = 5000; // Liczba detektorów do wygenerowania
            float detectorRadius = 0.2f; // Promień detektora (próg dopasowania)

            // 1. Wczytujemy WSZYSTKO przez  Parser C++
            DataLoader loader = new DataLoader(dataFilePath,maxRowsToLoad, featuresCount);
            Console.WriteLine("\n[DEBUG] Cechy pierwszego pakietu:");
           
            List<Antigen> allData = loader.LoadData();
            var firstPacketFeatures = allData[0].Data.Select(f => f.ToString("F4"));
            Console.WriteLine(string.Join(", ", firstPacketFeatures) + "\n");

            // 2. PODZIAŁ DANYCH (np. 80% do nauki, 20% do testów)
            // W prawdziwym projekcie zrobilibyśmy losowy podział, tu dla przykładu bierzemy filtry:
            //List<Antigen> trainSet = allData.Where(antigen => antigen.Attack == false).ToList();
            List<Antigen> trainSet = allData.Where(a => a.Attack == false).Take(5000).ToList();
            List<Antigen> testSet = allData.Skip(5000).Take(2000).ToList();

            Console.WriteLine($"Wczytano {allData.Count} pakietów. Z tego {trainSet.Count} to ruch prawidłowy (Self).");

            //Data for charts
            int testNum = 1;
            string filePath = $"chart{testNum}.csv";
            
            ResultsLogger dataForCharts = new ResultsLogger(filePath);
            dataForCharts.ProfilingVsAttempts(trainSet, testSet, featuresCount);
            testNum++;
            dataForCharts.lCurve(new ModelEvaluator.EvaluationMetrics(), trainSet, testSet, featuresCount);
            testNum++;
            dataForCharts.SensitivityThresholdAnalysis(new ModelEvaluator.EvaluationMetrics(), trainSet, testSet, featuresCount);
            testNum++;
            dataForCharts.RadiusHist(trainSet, featuresCount);

            /*
             // 3. Odpalamy trening TYLKO na czystych, zdrowych danych
             NegativeSelection nsa = new NegativeSelection();
             List<Detector> matureDetectors = nsa.GenerateDetectors_v2(trainSet, featuresCount-1, detectorsToGenerate, detectorRadius);

             // 4. FAZA TESTOWANIA I OCENY MODELU
             ModelEvaluator evaluator = new ModelEvaluator();
             evaluator.Evaluate(matureDetectors, testSet);

             Console.WriteLine("\nKoniec działania programu.");
             Console.ReadLine();

             */
        }

    }
}
