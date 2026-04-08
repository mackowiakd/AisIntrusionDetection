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
            float pcaRadius = 0.05f; // Promień detektora po redukcji PCA (może być mniejszy, bo dane są skompresowane)
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

            // 3. Odpalamy trening TYLKO na czystych, zdrowych danych
            NegativeSelection nsa = new NegativeSelection();
            List<Detector> matureDetectorsv2 = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detectorsToGenerate, detectorRadius);
            List<Detector> matureDetectorsv1 = nsa.GenerateDetectors_v1(trainSet, featuresCount - 1, detectorsToGenerate, detectorRadius);
            List<Detector> matureDetectorsv0 = nsa.GenerateDetectors_v0(trainSet, featuresCount - 1, detectorsToGenerate, detectorRadius);

            // 3.5.  Redukcja wymiarowości PCA
            var pca = new PcaTransformer();
            pca.Fit(trainSet, targetDim: 10);

            var compressedTraining = pca.Transform(trainSet);
            var compressedTesting = pca.Transform(testSet);
            // TO JEST KLUCZ DO SUKCESU - Skalujemy wynik PCA z powrotem do [0, 1] !!!
            pca.NormalizePcaDataTo01(compressedTraining, compressedTesting, 10);

            List<Detector> matureDetectorsv2_pca = nsa.GenerateDetectors_v2(compressedTraining, 10, detectorsToGenerate, pcaRadius);
            List<Detector> matureDetectorsv1_pca = nsa.GenerateDetectors_v1(compressedTraining, 10, detectorsToGenerate, pcaRadius);
            List<Detector> matureDetectorsv0_pca = nsa.GenerateDetectors_v0(compressedTraining, 10, detectorsToGenerate, pcaRadius);
           
            // 4. FAZA TESTOWANIA I OCENY MODELU
            ModelEvaluator evaluator = new ModelEvaluator();
            evaluator.Evaluate(matureDetectorsv2, testSet);
            evaluator.Evaluate(matureDetectorsv2_pca, compressedTesting);
            evaluator.Evaluate(matureDetectorsv1, testSet);
            evaluator.Evaluate(matureDetectorsv1_pca, compressedTesting);
            evaluator.Evaluate(matureDetectorsv0, testSet);
            evaluator.Evaluate(matureDetectorsv0_pca, compressedTesting);

            // I odpalasz stary algorytm na skompresowanych danych!


            /*
            
            // 5. LOGOWANIE WYNIKÓW DO  WYKRESÓW
           
           

            Console.WriteLine("Generowanie danych do Wykresu 1...");
            ResultsLogger.ProfilingVsAttempts(trainSet, testSet, featuresCount);

            Console.WriteLine("Generowanie danych do Wykresu 2...");
            ResultsLogger.LCurve(new ModelEvaluator.EvaluationMetrics(), trainSet, testSet, featuresCount);

            Console.WriteLine("Generowanie danych do Wykresu 3...");
            ResultsLogger.SensitivityThresholdAnalysis(new ModelEvaluator.EvaluationMetrics(), trainSet, testSet, featuresCount);

            Console.WriteLine("Generowanie danych do Wykresu 4...");
            ResultsLogger.RadiusHist(trainSet, featuresCount);

              * 
             */

        }

    }
}
