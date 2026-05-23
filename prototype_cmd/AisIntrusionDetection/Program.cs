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
            string dataFilePath = @"C:\Users\Dominika\source\repos\JA\AisIntrusionDetection\prototype_cmd\KDD_Combined.arff"; // Ścieżka do pliku z danymi

            int originalTrainCount = 25192;
            if (!File.Exists(dataFilePath)) throw new FileNotFoundException($"Nie znaleziono pliku: {dataFilePath}");



            // List<Antigen> FullTrainSet = normalTrainData.Take(originalTrainCount).ToList(); tu byl blad chcialam 25tys normal data, a train set mial OGÓLEM 25 tys (w tym ataki!!)


            var loader = new DataLoader(dataFilePath, 100000, 41);
            var allData = loader.LoadData();

            if (allData == null || allData.Count == 0)
                throw new Exception("Z biblioteki C++ wróciło 0 rekordów.");

            // ==========================================
            // ETAP 1: TWARDY PODZIAŁ KAGGLE (Kategoryczny zakaz tasowania przed podziałem!)
            // ==========================================
            var rawTrainData = allData.Take(originalTrainCount).ToList();
            var rawTestData = allData.Skip(originalTrainCount).ToList();

            Random rng = new Random(42);

            // ==========================================
            // ETAP 2: PRZYGOTOWANIE TRENINGU (Tylko zdrowy ruch)
            // ==========================================
            var normalTrainData = rawTrainData
                .Where(a => a.Attack == false)
                .OrderBy(x => rng.Next())
                .ToList();

            int maxHealthy = normalTrainData.Count -500; // To wyniesie około 13449

            // Zabezpieczenie: bierzemy tyle ile chce GUI, ale nie więcej niż fizycznie mamy
            
            List <Antigen> FullTrainSet = normalTrainData.Take(maxHealthy).ToList();

            // ==========================================
            // ETAP 3: PRZYGOTOWANIE TESTU (Zdrowe + Ataki Zero-Day)
            // ==========================================
            int maxTest = rawTestData.Count; // To wyniesie około 22544

            // Zabezpieczenie: bierzemy tyle ile chce GUI, ale nie więcej niż plik Test+
          
            List<Antigen> FullTestSet = rawTestData.OrderBy(x => rng.Next()).Take(maxTest-1000).ToList();

            ResultsLogger.PcaRadi_detCount(FullTrainSet, FullTestSet);
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

            //Console.WriteLine("\nGenerowanie danych do Wykresu PCA Benchmark Analysis...\n");
            //ResultsLogger.PcaBenchmarkAnalysis(trainSet, testSet);
            ////Console.WriteLine("\nGenerowanie danych do Wykresu 0...\n");
            ////ResultsLogger.LearningCurve(trainSet, testSet, featuresCount);
            //int detCountv0 = 5000;
            //int detCountv1 = 2500;
            //int detCountv2 = 1000;
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
