using AisIntrusionDetection.Algorithms;
using AisIntrusionDetection.Algos;
using AisIntrusionDetection.Models;
using System.Globalization;
using System.Net.Security;
using static AisIntrusionDetection.Algos.ModelEvaluator;

namespace AisIntrusionDetection.Interop
{
    /* Ta klasa jest odpowiedzialna za logowanie wyników do pliku CSV. 
     * wszystkie wykresy maja miec tu metody opwiedzialne za zebranie danych i zapis do pliku.
     */
    
    public static class ResultsLogger
    {
        
       
        /* @ Test
         * 0. Krzywa Nasycenia (Wpływ rozmiaru populacji detektorów)
         * (Wpływ rozmiaru populacji detektorów)
        * Cel: Pokazanie momentu zjawiska overfittingu / nasycenia dla V0, V1 i V2.
        */
        public static void LearningCurve(List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {
            string filePath = "Wykres0_Krzywa_Uczenia.csv";
            int[] sizesToTest = { 100, 500, 1000, 2500, 5000, 7500, 10000, 15000, 20000 };

            Console.WriteLine("\n==============================================");
            Console.WriteLine("[LCurve] KROK 1: Sondowanie gęstości przestrzeni...");
            NegativeSelection nsaProbe = new NegativeSelection();

            // Sztywny promień dla głupich algorytmów (V0, V1) - 95. percentyl dystansów
            float optimalRadiusV0V1 = nsaProbe.CalculateRobustMaxRadius(trainSet, featuresCount - 1, 5000);

            // Minimalny, mikroskopijny promień startowy dla V2 (Adaptive)
            float minRadiusV2 = 0.001f;

            Console.WriteLine($"[LCurve] Wyliczony promień dla V0/V1: {optimalRadiusV0V1:F4}");
            Console.WriteLine($"[LCurve] Promień startowy dla V2: {minRadiusV2:F4}");
            Console.WriteLine("==============================================\n");

            // UWAGA: append = false. Zawsze czyścimy plik przed nowym testem, by uniknąć sieczki!
            using (StreamWriter sw = new StreamWriter(filePath, append: false))
            {
                // Dodana kolumna "Algorithm"!
                sw.WriteLine("Algorithm,DetectorsCount,Radius,TP,FP,Accuracy");

                foreach (int detCount in sizesToTest)
                {
                    Console.WriteLine($"\n[LCurve] Trenowanie armii o rozmiarze: {detCount} detektorów...");
                    ModelEvaluator evaluator = new ModelEvaluator();

                    // --- V0 Uniform ---
                    NegativeSelection nsaV0 = new NegativeSelection();
                    var detV0 = nsaV0.GenerateDetectors_v0(trainSet, featuresCount - 1, detCount, optimalRadiusV0V1);
                    var metV0 = evaluator.Evaluate(detV0, testSet);
                    sw.WriteLine($"V0_Uniform,{detCount},{optimalRadiusV0V1.ToString(CultureInfo.InvariantCulture)},{metV0.TP},{metV0.FP},{metV0.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    // --- V1 Gravity ---
                    NegativeSelection nsaV1 = new NegativeSelection();
                    var detV1 = nsaV1.GenerateDetectors_v1(trainSet, featuresCount - 1, detCount, optimalRadiusV0V1);
                    var metV1 = evaluator.Evaluate(detV1, testSet);
                    sw.WriteLine($"V1_Gravity,{detCount},{optimalRadiusV0V1.ToString(CultureInfo.InvariantCulture)},{metV1.TP},{metV1.FP},{metV1.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    // --- V2 Adaptive ---
                    NegativeSelection nsaV2 = new NegativeSelection();
                    var detV2 = nsaV2.GenerateDetectors_v2(trainSet, featuresCount - 1, detCount, minRadiusV2);
                    var metV2 = evaluator.Evaluate(detV2, testSet);
                    sw.WriteLine($"V2_Adaptive,{detCount},{minRadiusV2.ToString(CultureInfo.InvariantCulture)},{metV2.TP},{metV2.FP},{metV2.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    sw.Flush(); // Zapis w locie
                }
            }

            Console.WriteLine($"\n[LCurve] Zakończono z sukcesem! Plik gotowy do wykresu: {filePath}");
        }




        /* @ Test 
        * 1. a (Radius vs Accuracy)
        * Cel: Udowodnienie istnienia "Sweet Spotu" i wykazanie przewagi V2 (Adaptive).
        * Parametry: Promień od 0.05 do 1.5 (krok 0.1). Stała liczba detektorów.
        */
        public static void RadiusSensitivityAnalysis(List<Antigen> trainSet, List<Antigen> testSet, int featuresCount, int detCountV0, int detCountV1, int detCountV2)
        {
            string filePath = "Wykres1_Analiza_Promienia.csv";
            bool fileExists = File.Exists(filePath);

            Console.WriteLine("\n==============================================");
            Console.WriteLine($"[Radius Analysis] Start testu wrażliwości (Cel: {detCountV0}/{detCountV1}/{detCountV2} detektorów)");
            Console.WriteLine("==============================================\n");

            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                if (!fileExists)
                {
                    // Dodana kolumna "DetectorsGenerated", by udowodnić, że V0/V1 "duszą się" przy dużych promieniach!
                    sw.WriteLine("Version,Radius,DetectorsGenerated,TP,FP,Accuracy");
                    fileExists = true;
                }

                // Badamy pełne spektrum promieni: od mikroskopijnych (0.05) po gigantyczne (1.5)
                for (float r = 0.05f; r <= 1.55f; r += 0.1f)
                {
                    float radius = (float)Math.Round(r, 2); // Zaokrąglamy, by uniknąć krzaków typu 0.15000001
                    Console.WriteLine($"\n--- Testowanie promienia bazowego: {radius:F2} ---");

                    // ==========================================
                    // TEST V0: Ślepe Losowanie (Sztywny Promień)
                    // ==========================================
                    Console.WriteLine("-> Generowanie V0...");
                    NegativeSelection nsaV0 = new NegativeSelection();
                    var detV0 = nsaV0.GenerateDetectors_v0(trainSet, featuresCount - 1, detCountV0, radius);
                    var evalV0 = new ModelEvaluator().Evaluate(detV0, testSet);

                    sw.WriteLine($"V0_Uniform,{radius.ToString(CultureInfo.InvariantCulture)},{detV0.Count},{evalV0.TP},{evalV0.FP},{evalV0.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    // ==========================================
                    // TEST V1: Profilowanie Grawitacyjne (Sztywny Promień)
                    // ==========================================
                    Console.WriteLine("-> Generowanie V1...");
                    NegativeSelection nsaV1 = new NegativeSelection();
                    var detV1 = nsaV1.GenerateDetectors_v1(trainSet, featuresCount - 1, detCountV1, radius);
                    var evalV1 = new ModelEvaluator().Evaluate(detV1, testSet);

                    sw.WriteLine($"V1_Gravity,{radius.ToString(CultureInfo.InvariantCulture)},{detV1.Count},{evalV1.TP},{evalV1.FP},{evalV1.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    // ==========================================
                    // TEST V2: Adaptive V-Detector (Min Promień)
                    // ==========================================
                    Console.WriteLine("-> Generowanie V2...");
                    NegativeSelection nsaV2 = new NegativeSelection();
                    var detV2 = nsaV2.GenerateDetectors_v2(trainSet, featuresCount - 1, detCountV2, radius);
                    var evalV2 = new ModelEvaluator().Evaluate(detV2, testSet);

                    sw.WriteLine($"V2_Adaptive,{radius.ToString(CultureInfo.InvariantCulture)},{detV2.Count},{evalV2.TP},{evalV2.FP},{evalV2.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    sw.Flush(); // Zapisujemy na bieżąco, żeby nie stracić danych w razie przerwania!
                }
            }

            Console.WriteLine($"\n[Radius Analysis] Zakończono z sukcesem! Plik gotowy do wykresu: {filePath}");
        }

        //OLD

      

        /*@ Test
         4. Rozkład Przestrzenny Detektorów (Histogram Promieni)
         * parametry:
         *  -Przedziały wielkości wyliczonego promienia (np. 0.0-0.2, 0.2-0.4, 0.4-0.6...)
         *  - Liczba detektorów w każdym przedziale ktore osignely dany promien
         */
        public static void RadiusHist(List<Antigen> trainSet, int featuresCount, int detCountsize)
        {
            string filePath = "Wykres4_Histogram.csv";
            int[] sizesToTest = { detCountsize }; //BIERZEMY DET SIZE NA PODSTAWIE WYKRESU 0 -> Detector Count vs Accuracy 
            bool fileExists = File.Exists(filePath);

            foreach (int detCount in sizesToTest)
            {
                NegativeSelection nsa = new NegativeSelection();

                // 1. GENERUJEMY TYLKO RAZ DLA DANEJ WIELKOŚCI!
                // Ustawiamy próg na mikroskopijny (0.01f), żeby złapać absolutnie wszystkie balony do statystyk
                List<Detector> detectors = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detCount, 0.01f);

                //  Dynamiczne szukanie przedziałów na podst maxRadius, żeby histogram był dobrze rozłożony (nie za szerokie, nie za wąskie)
                float maxRadius = detectors.Max(d => d.Radius);
                float step = maxRadius / 5.0f; // Dzielimy przestrzeń na 5 równych koszyków

                // LINQ precyzyjnie sortuje detektory na podstawie wyliczonego kroku
                int bin1 = detectors.Count(d => d.Radius >= 0.0f && d.Radius <= step);
                int bin2 = detectors.Count(d => d.Radius > step && d.Radius <= 2 * step);
                int bin3 = detectors.Count(d => d.Radius > 2 * step && d.Radius <= 3 * step);
                int bin4 = detectors.Count(d => d.Radius > 3 * step && d.Radius <= 4 * step);
                int bin5 = detectors.Count(d => d.Radius > 4 * step);

                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    if (!fileExists)
                    {
                        // Zapisujemy też MaxRadius i Step, żeby skrypt w Pythonie wiedział, jak opisać oś X!
                        sw.WriteLine("DetectorsCount,MaxRadius,Step,Bin1,Bin2,Bin3,Bin4,Bin5");
                        fileExists = true;
                    }

                    string line = $"{detCount}," +
                                  $"{maxRadius.ToString(CultureInfo.InvariantCulture)}," +
                                  $"{step.ToString(CultureInfo.InvariantCulture)}," +
                                  $"{bin1},{bin2},{bin3},{bin4},{bin5}";
                    sw.WriteLine(line);
                }
            }
        }

        public static void PcaBenchmarkAnalysis(List<Antigen> rawTrainSet, List<Antigen> rawTestSet)
        {
            string filePath = "Wykres5_PCA_A_B_Testing.csv";
            bool fileExists = File.Exists(filePath);

            // Testujemy wymiary od mocnej kompresji (5) po baseline (41)
            int[] dimensionsToTest = { 5, 10, 15, 20, 25, 30, 41 };
            int runs = 3;
            int detCount = 5000;

            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                if (!fileExists)
                {
                    // DODANA KOLUMNA: Version (V2_Gravity lub V3_Uniform)
                    sw.WriteLine("Dimensions,Version,RunID,Attempts,TP,FP,TN,FN,Accuracy");
                    fileExists = true;
                }

                foreach (int dim in dimensionsToTest)
                {
                    Console.WriteLine($"\n==============================================");
                    Console.WriteLine($"[PCA Benchmark] Rozpoczynam test dla {dim} wymiarów");
                    Console.WriteLine($"==============================================");

                    List<Antigen> compressedTrain;
                    List<Antigen> compressedTest;
                    float minRadius;

                    if (dim < 41)
                    {
                        var pca = new PcaTransformer();
                        pca.Fit(rawTrainSet, dim);
                        compressedTrain = pca.Transform(rawTrainSet);
                        compressedTest = pca.Transform(rawTestSet);
                        pca.NormalizePcaDataTo01(compressedTrain, compressedTest, dim);
                        minRadius = 0.05f;
                    }
                    else
                    {
                        compressedTrain = rawTrainSet;
                        compressedTest = rawTestSet;
                        minRadius = 0.20f;
                    }

                    for (int r = 1; r <= runs; r++)
                    {
                        Console.WriteLine($"\n--- Przebieg: {r}/{runs} dla {dim}D ---");

                        // 1. Zbadajmy przestrzeń (jeden sprawiedliwy limit promienia dla obu algorytmów!)
                        NegativeSelection nsaSonda = new NegativeSelection();
                        //  float dynamicMaxRadius = nsaSonda.CalculateRobustMaxRadius(compressedTrain, dim, detCount);
                        // ==========================================
                        // TEST 0: V0 (Czyste losowanie - bez grawitacji i dynamiczengo pormienia)
                        // ==========================================
                        NegativeSelection nsaV0 = new NegativeSelection();
                        Console.WriteLine("-> Start V0 (basic)...");
                        List<Detector> detectorsV0 = nsaV0.GenerateDetectors_v0(compressedTrain, dim, detCount, minRadius);
                        ModelEvaluator evalV0 = new ModelEvaluator();
                        var metricsV0 = evalV0.Evaluate(detectorsV0, compressedTest);

                        sw.WriteLine($"{dim},V0_Basic,{r},{nsaV0.attempts},{metricsV0.TP},{metricsV0.FP},{metricsV0.TN},{metricsV0.FN},{metricsV0.Accuracy.ToString(CultureInfo.InvariantCulture)}");


                        // ==========================================
                        // TEST 1: V3 (Czyste losowanie - bez grawitacji (dynamiczny promien))
                        // ==========================================
                        NegativeSelection nsaV3 = new NegativeSelection();
                        Console.WriteLine("-> Start V3 (Uniform)...");
                        List<Detector> detectorsV3 = nsaV3.GenerateDetectors_V3pca(compressedTrain, dim, detCount, minRadius);
                        ModelEvaluator evalV3 = new ModelEvaluator();
                        var metricsV3 = evalV3.Evaluate(detectorsV3, compressedTest);

                        sw.WriteLine($"{dim},V3_Uniform,{r},{nsaV3.attempts},{metricsV3.TP},{metricsV3.FP},{metricsV3.TN},{metricsV3.FN},{metricsV3.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                        // ==========================================
                        // TEST 2: V2 (Grawitacja - Twój główny silnik)
                        // ==========================================
                        NegativeSelection nsaV2 = new NegativeSelection();
                        Console.WriteLine("-> Start V2 (Gravity)...");
                        // UWAGA: upewnij się, że Twój oryginalny GenerateDetectors_v2 sam sobie nie wylicza znowu MaxRadiusa,
                        // albo po prostu pozwól mu działać normalnie (on pod maską używa CalculateRobustMaxRadius).

                        List<Detector> detectorsV2 = nsaV2.GenerateDetectors_v2(compressedTrain, dim, detCount, minRadius);
                        ModelEvaluator evalV2 = new ModelEvaluator();
                        var metricsV2 = evalV2.Evaluate(detectorsV2, compressedTest);

                        sw.WriteLine($"{dim},V2_Gravity,{r},{nsaV2.attempts},{metricsV2.TP},{metricsV2.FP},{metricsV2.TN},{metricsV2.FN},{metricsV2.Accuracy.ToString(CultureInfo.InvariantCulture)}");


                        // ==========================================
                        // TEST 3: V1 (Grawitacja, dynamic radius)
                        // ==========================================
                        NegativeSelection nsaV1 = new NegativeSelection();
                        Console.WriteLine("-> Start V1 (Gravity, Dynamic Radius)...");
                        // UWAGA: upewnij się, że Twój oryginalny GenerateDetectors_v1 sam sobie nie wylicza znowu MaxRadiusa,
                        // albo po prostu pozwól mu działać normalnie (on pod maską używa CalculateRobustMaxRadius).

                        List<Detector> detectorsV1 = nsaV1.GenerateDetectors_v1(compressedTrain, dim, detCount, minRadius);
                        ModelEvaluator evalV1 = new ModelEvaluator();
                        var metricsV1 = evalV1.Evaluate(detectorsV1, compressedTest);

                        sw.WriteLine($"{dim},V1_Gravity_Dynamic,{r},{nsaV1.attempts},{metricsV1.TP},{metricsV1.FP},{metricsV1.TN},{metricsV1.FN},{metricsV1.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                        sw.Flush();
                    }
                }
            }
            Console.WriteLine("\n[PCA Benchmark] Zakończono! Zapisano dane do: " + filePath);
        }

        public static void PcaRadi_detCount(List<Antigen> rawTrainSet, List<Antigen> rawTestSet)
        {
            string filePath = "PCA_radiDetCountTesting.csv";
            bool fileExists = File.Exists(filePath);

            int runs = 3;
            int[] detCount = { 5000, 10000, 15000 };
            float[] radii = { 0.05f, 0.10f, 0.20f };
            int dim = 20; // Stały wymiar PCA do testów tarczy

            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                if (!fileExists)
                {
                    // POPRAWKA: Dodane kolumny DetectorCount oraz MinRadius do nagłówka
                    sw.WriteLine("Dimensions,Version,DetectorCount,MinRadius,RunID,Attempts,TP,FP,TN,FN,Accuracy");
                    fileExists = true;
                }

             
                
                var pca = new PcaTransformer();
                pca.Fit(rawTrainSet, dim);
                var compressedTrain = pca.Transform(rawTrainSet);
                var compressedTest = pca.Transform(rawTestSet);
                pca.NormalizePcaDataTo01(compressedTrain, compressedTest, dim);

                for (int r = 1; r <= runs; r++)
                {
                    foreach (int det in detCount)
                    {
                        Console.WriteLine($"\n--- Przebieg: {r}/{runs} dla {dim}D ---");
                        foreach (float radius in radii)
                        {
                            string radiusStr = radius.ToString("F2", CultureInfo.InvariantCulture);
                            Console.WriteLine($"\n--- Parametry: Promień min = {radiusStr}, Detektory = {det} ---");

                            // ==========================================
                            // TEST 0: V0 (Basic - promień statyczny)
                            // ==========================================
                            NegativeSelection nsaV0 = new NegativeSelection();
                            Console.WriteLine("-> Start V0 (Basic)...");
                            // POPRAWKA: Przekazujemy zmienną 'radius' z pętli, zamiast minRadius!
                            List<Detector> detectorsV0 = nsaV0.GenerateDetectors_v0(compressedTrain, dim, det, radius);
                            ModelEvaluator evalV0 = new ModelEvaluator();
                            var metricsV0 = evalV0.Evaluate(detectorsV0, compressedTest);

                            // POPRAWKA: Zapisujemy dane łącznie z det i radiusStr
                            sw.WriteLine($"{dim},V0_Basic,{det},{radiusStr},{r},{nsaV0.attempts},{metricsV0.TP},{metricsV0.FP},{metricsV0.TN},{metricsV0.FN},{metricsV0.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                            // ==========================================
                            // TEST 1: V3 (Uniform - dynamiczny promień)
                            // ==========================================
                            NegativeSelection nsaV3 = new NegativeSelection();
                            Console.WriteLine("-> Start V3 (Uniform)...");
                            // POPRAWKA: Przekazujemy zmienną 'radius' z pętli!
                            List<Detector> detectorsV3 = nsaV3.GenerateDetectors_V3pca(compressedTrain, dim, det, radius);
                            ModelEvaluator evalV3 = new ModelEvaluator();
                            var metricsV3 = evalV3.Evaluate(detectorsV3, compressedTest);

                            sw.WriteLine($"{dim},V3_Uniform,{det},{radiusStr},{r},{nsaV3.attempts},{metricsV3.TP},{metricsV3.FP},{metricsV3.TN},{metricsV3.FN},{metricsV3.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                            // ==========================================
                            // TEST 2: V2 (Gravity - oryginalny)
                            // ==========================================
                            NegativeSelection nsaV2 = new NegativeSelection();
                            Console.WriteLine("-> Start V2 (Gravity)...");
                            List<Detector> detectorsV2 = nsaV2.GenerateDetectors_v2(compressedTrain, dim, det, radius);
                            ModelEvaluator evalV2 = new ModelEvaluator();
                            var metricsV2 = evalV2.Evaluate(detectorsV2, compressedTest);

                            sw.WriteLine($"{dim},V2_Gravity,{det},{radiusStr},{r},{nsaV2.attempts},{metricsV2.TP},{metricsV2.FP},{metricsV2.TN},{metricsV2.FN},{metricsV2.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                            // ==========================================
                            // TEST 3: V1 (Gravity + Dynamic Radius)
                            // ==========================================
                            NegativeSelection nsaV1 = new NegativeSelection();
                            Console.WriteLine("-> Start V1 (Gravity, Dynamic Radius)...");
                            List<Detector> detectorsV1 = nsaV1.GenerateDetectors_v1(compressedTrain, dim, det, radius);
                            ModelEvaluator evalV1 = new ModelEvaluator();
                            var metricsV1 = evalV1.Evaluate(detectorsV1, compressedTest);

                            sw.WriteLine($"{dim},V1_Gravity_Dynamic,{det},{radiusStr},{r},{nsaV1.attempts},{metricsV1.TP},{metricsV1.FP},{metricsV1.TN},{metricsV1.FN},{metricsV1.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                            // Wymuszamy natychmiastowy zapis na dysk, żeby nie stracić danych w razie awarii
                            sw.Flush();
                        }
                    }
                }
            }
            Console.WriteLine("\n[PCA Benchmark] Zakończono! Zapisano dane do: " + filePath);
        }

        // OPTIONAL //

        /* @ Test
        * 
        1. Wpływ Profilowania na Koszt Generowania (Problem Pustej Przestrzeni)
        * parametry: 
        *  - Oś X Wersja Algorytm (Wersja 0: Ślepe losowanie vs. Wersja 1: Profilowanie)
        *  - Oś Y Logarytmiczna liczba prób (Attempts) potrzebnych do wygenerowania 5000 detektorów
        * 
        * */
        public static void ProfilingVsAttempts(List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {

            string filePath = "Wykres1_Koszt_Ewolucyjny.csv";
            bool fileExists = File.Exists(filePath);
            int[] sizesToTest = { 1000, 5000, 10000, 20000 };

            foreach (int detCount in sizesToTest)
            {

                NegativeSelection nsa = new NegativeSelection();
                // wartosc promienia tzw "SWEET SPOT" Z WYKRESU NR 3!
                float radius = 1.0f;
                // Test Wersji 0 (Ślepe losowanie)
                nsa.GenerateDetectors_v0(trainSet, featuresCount - 1, detCount, radius);
                int attemptsV0 = nsa.attempts;

                // Test Wersji 1 (Profilowanie)
                nsa.GenerateDetectors_v1(trainSet, featuresCount - 1, detCount, radius);
                int attemptsV1 = nsa.attempts;

                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    if (!fileExists)
                    {
                        sw.WriteLine("Version,DetectorsCount,Radius,Attempts");
                        fileExists = true;
                    }

                    string radStr = radius.ToString(CultureInfo.InvariantCulture);
                    sw.WriteLine($"V.0,{detCount},{radStr},{attemptsV0}");
                    sw.WriteLine($"V.1,{detCount},{radStr},{attemptsV1}");
                }
            }


        }
            /* @ Test 
         * 3. Wielki Benchmark Algorytmów (V0 vs V1 vs V2)
         * Cel: Ostateczne zestawienie skuteczności przy różnych rozmiarach populacji.
         */
        public static void RunFullBenchmark(List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {
            string filePath = "Benchmark_Wyniki_Koncowe.csv";
            bool fileExists = File.Exists(filePath);

            // 1. USTALAMY PARAMETRY BADAWCZE
            int[] detectorCounts = { 1000, 3000, 5000, 10000 };

            Console.WriteLine("\n==============================================");
            Console.WriteLine("[Benchmark] KROK 1: Analiza przestrzeni do wyznaczenia uczciwego promienia dla V0/V1...");

            // Obliczamy OBIEKTYWNY promień na podstawie danych (95. percentyl dystansów)
            NegativeSelection nsaProbe = new NegativeSelection();
            float fixedRadiusV0V1 = nsaProbe.CalculateRobustMaxRadius(trainSet, featuresCount - 1, 5000);

            Console.WriteLine($"[Benchmark] Obliczono optymalny, sztywny promień: {fixedRadiusV0V1:F4}");

            // V2 jest Adaptive, więc dajemy mu promień mikroskopijny, pozwalając na pełną elastyczność
            float minRadiusV2 = 0.001f;
            Console.WriteLine($"[Benchmark] Promień minimalny dla V2 (Adaptive) ustawiono na: {minRadiusV2:F4}");
            Console.WriteLine("==============================================\n");

            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                if (!fileExists)
                {
                    sw.WriteLine("Algorithm,DetectorsTarget,DetectorsGenerated,TP,FP,Accuracy");
                    fileExists = true;
                }

                foreach (int count in detectorCounts)
                {
                    Console.WriteLine($"\n--- Uruchamianie testów dla {count} detektorów ---");
                    ModelEvaluator evaluator = new ModelEvaluator();

                    // --- TEST V0 (Uniform) ---
                    Console.WriteLine("-> Trenowanie V0...");
                    NegativeSelection nsaV0 = new NegativeSelection();
                    var detV0 = nsaV0.GenerateDetectors_v0(trainSet, featuresCount - 1, count, fixedRadiusV0V1);
                    var evalV0 = evaluator.Evaluate(detV0, testSet);
                    sw.WriteLine($"V0_Uniform,{count},{detV0.Count},{evalV0.TP},{evalV0.FP},{evalV0.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    // --- TEST V1 (Gravity) ---
                    Console.WriteLine("-> Trenowanie V1...");
                    NegativeSelection nsaV1 = new NegativeSelection();
                    var detV1 = nsaV1.GenerateDetectors_v1(trainSet, featuresCount - 1, count, fixedRadiusV0V1);
                    var evalV1 = evaluator.Evaluate(detV1, testSet);
                    sw.WriteLine($"V1_Gravity,{count},{detV1.Count},{evalV1.TP},{evalV1.FP},{evalV1.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    // --- TEST V2 (Adaptive) ---
                    Console.WriteLine("-> Trenowanie V2...");
                    NegativeSelection nsaV2 = new NegativeSelection();
                    var detV2 = nsaV2.GenerateDetectors_v2(trainSet, featuresCount - 1, count, minRadiusV2);
                    var evalV2 = evaluator.Evaluate(detV2, testSet);
                    sw.WriteLine($"V2_Adaptive,{count},{detV2.Count},{evalV2.TP},{evalV2.FP},{evalV2.Accuracy.ToString(CultureInfo.InvariantCulture)}");

                    // Zapis w locie
                    sw.Flush();
                }
            }
            Console.WriteLine("\n[Benchmark] Zakończono! Wyniki zrzucono do CSV.");
        }

        /*@ Test 
        * 
        2. Krzywa Uczenia (Wpływ rozmiaru populacji detektorów)
        * parametry: 
        *  - Liczba wygenerowanych detektorow (np. 1k, 5k, 10k, 20k)
        *  - Oś Y1: Accuracy ; Oś Y2: Wykryte Ataki (TP)
        * 
        * */
        //public static void LCurve(EvaluationMetrics metrics, List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        //{
        //    string filePath = "Wykres0_Krzywa_Uczenia.csv";
        //    // Dodaliśmy wartości pośrednie, żeby krzywa była płynna!
        //    int[] sizesToTest = { 100, 500, 1000, 2500, 5000, 7500, 10000, 15000, 20000 };
        //    bool fileExists = File.Exists(filePath);
        //    float radius = 1.0f;

        //    foreach (int detCount in sizesToTest)
        //    {

        //        NegativeSelection nsa = new NegativeSelection();

        //        // Uczymy
        //        List<Detector> detectors = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detCount, radius);

        //        // Testujemy (pobieramy obiekt metrics)
        //        ModelEvaluator evaluator = new ModelEvaluator();
        //        metrics = evaluator.Evaluate(detectors, testSet);

        //        // Używamy StreamWriter w bloku using (automatycznie zamyka plik)
        //        using (StreamWriter sw = new StreamWriter(filePath, append: true))
        //        {
        //            // Jeśli plik jest nowy, dodaj nagłówki kolumn
        //            if (!fileExists)
        //            {
        //                sw.WriteLine("DetectorsCount,Radius,TP,Accuracy");
        //                fileExists = true;
        //            }

        //            // Wpisujemy kolejno: detCount, radius, TP, Accuracy
        //            string line = $"{detCount},{radius.ToString(CultureInfo.InvariantCulture)},{metrics.TP},{metrics.Accuracy.ToString(CultureInfo.InvariantCulture)}";

        //            sw.WriteLine(line);
        //        }
        //    }
        //}

    }
}
