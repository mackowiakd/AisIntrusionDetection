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
            string dataFilePath = @"C:\Users\Dominika\source\repos\JA\AisIntrusionDetection\prototype_cmd\KDDTrain+_20Percent.arff"; // Ścieżka do pliku z danymi
            int maxRowsToLoad = 20000; // Maksymalna liczba wierszy do załadowania
            int trainSetSize = 5000;
            int testSetSize = 5000;

            int featuresCount = 42; // Liczba cech (kolumn) do załadowania
            int detectorsToGenerate = 5000; // Liczba detektorów do wygenerowania
            float detectorRadius = 0.1f; // Promień detektora (próg dopasowania)
            float pcaRadius = 0.05f; // Promień detektora po redukcji PCA (może być mniejszy, bo dane są skompresowane)
            int dimPca = 20; // Docelowa liczba wymiarów po PCA
            // 1. Wczytujemy WSZYSTKO przez  Parser C++
            DataLoader loader = new DataLoader(dataFilePath,maxRowsToLoad, featuresCount);
            Console.WriteLine("\n[DEBUG] Cechy pierwszego pakietu:");
           
            List<Antigen> allData = loader.LoadData();
            var firstPacketFeatures = allData[0].Data.Select(f => f.ToString("F4"));
            Console.WriteLine(string.Join(", ", firstPacketFeatures) + "\n");

            // 2. PODZIAŁ DANYCH (np. 80% do nauki, 20% do testów)
            // ->1. Dzielimy surowe dane na dwa koszyki
            var allNormal = allData.Where(a => a.Attack == false).ToList();
            var allAttacks = allData.Where(a => a.Attack == true).ToList();

            // 2. ZBIÓR TRENINGOWY: Uczymy system TYLKO na zdrowym ruchu (np. pierwsze 5000)
            List<Antigen> trainSet = allNormal.Take(trainSetSize).ToList();

            // 3. ZBIÓR TESTOWY: Mieszamy niewidziany zdrowy ruch z atakami
            // List<Antigen> testSet = new List<Antigen>();

            // Bierzemy kolejne 5000 zdrowych pakietów (Skip omija te, które poszły do treningu!)
            List<Antigen> testSet = allNormal.Skip(trainSetSize).Take(testSetSize).ToList();

            // Dorzucamy 5000 ataków
            testSet.AddRange(allAttacks.Take(5000));

            Console.WriteLine($"Wczytano {allData.Count} pakietów. Z tego {trainSet.Count} to ruch prawidłowy (Self).");

            /*
            

            // 3. Odpalamy trening TYLKO na czystych, zdrowych danych
            NegativeSelection nsa = new NegativeSelection();
            List<Detector> matureDetectorsv1 = nsa.GenerateDetectors_v1(trainSet, featuresCount - 1, detectorsToGenerate, detectorRadius);
            List<Detector> matureDetectorsv0 = nsa.GenerateDetectors_v0(trainSet, featuresCount - 1, detectorsToGenerate, detectorRadius);
            List<Detector> matureDetectorsv2 = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detectorsToGenerate, detectorRadius);
           
            // 3.5.  Redukcja wymiarowości PCA
            var pca = new PcaTransformer();
            pca.Fit(trainSet, targetDim: dimPca);

            var compressedTraining = pca.Transform(trainSet);
            var compressedTesting = pca.Transform(testSet);
            // TO JEST KLUCZ DO SUKCESU - Skalujemy wynik PCA z powrotem do [0, 1] !!!
            pca.NormalizePcaDataTo01(compressedTraining, compressedTesting, dimPca);

            List<Detector> matureDetectorsv2_pca = nsa.GenerateDetectors_v2(compressedTraining, dimPca, detectorsToGenerate, pcaRadius);
            List<Detector> matureDetectorsV3pca = nsa.GenerateDetectors_V3pca(compressedTraining, dimPca, detectorsToGenerate, pcaRadius);


            // 4. FAZA TESTOWANIA I OCENY MODELU
            ModelEvaluator evaluator = new ModelEvaluator();
            Console.WriteLine("\n v2 \n :");
            evaluator.Evaluate(matureDetectorsv2, testSet);
            Console.WriteLine("\n v1 \n :");
            evaluator.Evaluate(matureDetectorsv1, testSet);
            Console.WriteLine("\n v0 \n :");
            evaluator.Evaluate(matureDetectorsv0, testSet);
            Console.WriteLine("\n PCA with v2 \n :");
            evaluator.Evaluate(matureDetectorsv2_pca, compressedTesting);
            Console.WriteLine("\n PCA with v3 \n :");
            evaluator.Evaluate(matureDetectorsV3pca, compressedTesting);



            */




            // 5. LOGOWANIE WYNIKÓW DO  WYKRESÓW
            // Uruchomienie ostatecznego benchmarku na surowych danych

            Console.WriteLine("\nGenerowanie danych do Wykresu PCA Benchmark Analysis...\n");
            ResultsLogger.PcaBenchmarkAnalysis(trainSet, testSet);
            //Console.WriteLine("\nGenerowanie danych do Wykresu 0...\n");
            //ResultsLogger.LearningCurve(trainSet, testSet, featuresCount);
            int detCountv0 = 5000;
            int detCountv1 = 2500;
            int detCountv2 = 1000;
            //Console.WriteLine("\nGenerowanie danych do Wykresu zestawienia v0 -v2...\n");
            //ResultsLogger.RunFullBenchmark(trainSet, testSet, featuresCount);

            //Console.WriteLine("\nGenerowanie danych do wykres 1 (Radius vs Accuracy liczba det na podsatwie wykresu 0...\n");
            //ResultsLogger.RadiusSensitivityAnalysis(trainSet, testSet, featuresCount, detCountv0, detCountv1, detCountv2);

            //ResultsLogger.RadiusHist(trainSet, featuresCount, detCountOptimal);



            /*

           Console.WriteLine("Generowanie danych do Wykresu 0...");
           ResultsLogger.ProfilingVsAttempts(trainSet, testSet, featuresCount);

           

           Console.WriteLine("Generowanie danych do Wykresu 3...");
           ResultsLogger.SensitivityThresholdAnalysis(new ModelEvaluator.EvaluationMetrics(), trainSet, testSet, featuresCount);

           Console.WriteLine("Generowanie danych do Wykresu 4...");
           ResultsLogger.RadiusHist(trainSet, featuresCount);

             * 
            */

        }

    }
}
