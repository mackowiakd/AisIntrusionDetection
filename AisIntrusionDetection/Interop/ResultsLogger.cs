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
                float radius = 0.5f;
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

        /*@ Test 
         * 
         2. Krzywa Uczenia (Wpływ rozmiaru populacji detektorów)
         * parametry: 
         *  - Liczba wygenerowanych detektorow (np. 1k, 5k, 10k, 20k)
         *  - Oś Y1: Accuracy ; Oś Y2: Wykryte Ataki (TP)
         * 
         * */
        public static void LCurve(EvaluationMetrics metrics, List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {
            string filePath = "Wykres2_Krzywa_Uczenia.csv";
            // Dodaliśmy wartości pośrednie, żeby krzywa była płynna!
            int[] sizesToTest = { 100, 500, 1000, 2500, 5000, 7500, 10000, 15000, 20000 };
            bool fileExists = File.Exists(filePath);
            float radius = 0.1f;

            foreach (int detCount in sizesToTest)
            {
                
                NegativeSelection nsa = new NegativeSelection();

                // Uczymy
                List<Detector> detectors = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detCount, radius);

                // Testujemy (pobieramy obiekt metrics)
                ModelEvaluator evaluator = new ModelEvaluator();
                metrics = evaluator.Evaluate(detectors, testSet);

                // Używamy StreamWriter w bloku using (automatycznie zamyka plik)
                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    // Jeśli plik jest nowy, dodaj nagłówki kolumn
                    if (!fileExists)
                    {
                        sw.WriteLine("DetectorsCount,Radius,TP,Accuracy");
                        fileExists = true;
                    }

                    // Wpisujemy kolejno: detCount, radius, TP, Accuracy
                    string line = $"{detCount},{radius.ToString(CultureInfo.InvariantCulture)},{metrics.TP},{metrics.Accuracy.ToString(CultureInfo.InvariantCulture)}";

                    sw.WriteLine(line);
                }
            }
        }
       

       
         /*
         3.Analiza progu czułości V-Detectora(Kompromis TP vs FP)
         * parametry: 
         *  - targetRadius, 
         *  - Liczba pakietów(Jedna linia dla TP, druga dla FP)
         */
        public static void SensitivityThresholdAnalysis(EvaluationMetrics metrics, List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {
            string filePath = "Wykres3_Analiza_Progu.csv";
            int[] sizesToTest = { 1000, 5000, 10000, 20000 };
            // 1. DYNAMICZNE SONDOWANIE rozstawu pakietow (promienie)
            NegativeSelection nsaProbe = new NegativeSelection();
            float robustMaxRadius = nsaProbe.CalculateRobustMaxRadius(trainSet, featuresCount - 1, 2000);

            // I tablicę opieramy teraz na tym bezpiecznym maksimum (nie musimy już ucinać * 0.95f)
            float[] radiusSize = {
            robustMaxRadius * 0.10f,
            robustMaxRadius * 0.25f,
            robustMaxRadius * 0.50f,
            robustMaxRadius * 0.75f,
            robustMaxRadius * 1.00f
             };

            bool fileExists = File.Exists(filePath);

            float globalScaleFactor = 1.0f;
            foreach (int detCount in sizesToTest)
            {
               
                foreach (float targetRadius in radiusSize)
                {
                    List<Detector> detectors = new List<Detector>();
                    NegativeSelection nsa = new NegativeSelection();

                    float currentRadius = targetRadius * globalScaleFactor;
                   

                   
                    //  pętla Fallback 
                    while (true)
                    {
                       
                        nsa = new NegativeSelection(); // Tworzymy nowy obiekt, żeby zresetować liczniki prób
                        detectors = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detCount, currentRadius);

                        // Sprawdzamy, czy algorytm dostarczył pełną armię
                        if (detectors.Count >= detCount)
                        {
                            break; // Sukces! Wychodzimy z pętli Fallback i idziemy testować
                        }
                        else
                        {
                            // Feedback z NSA: Przestrzeń była za ciasna.
                            Console.WriteLine($"\n[Adaptacja] Promień {currentRadius:F4} jest fizycznie za duży dla {detCount} detektorów!");

                            // Zmniejszamy wymagania o 5%  => tzreba przeskalowac cala tablcie radius!
                            globalScaleFactor *= 0.95f;
                            // Wyliczamy nowy promień na podstawie pomniejszonej skali
                            currentRadius = targetRadius * globalScaleFactor;

                            Console.WriteLine($"[Adaptacja] Zmniejszam promień o 5% -> Nowy cel: {currentRadius:F4}. Próbuję ponownie...");
                        }
                    }

                    // Kiedy w końcu się uda, testujemy i zapisujemy wynik
                    ModelEvaluator evaluator = new ModelEvaluator();
                    metrics = evaluator.Evaluate(detectors, testSet);



                    // Używamy StreamWriter w bloku using (automatycznie zamyka plik)
                    using (StreamWriter sw = new StreamWriter(filePath, append: true))
                    {
                        // Jeśli plik jest nowy, dodaj nagłówki kolumn
                        if (!fileExists)
                        {
                            sw.WriteLine("DetectorsCount,MinRadius,Attempts,TP,FP");
                            fileExists = true;
                        }

                        string line = $"{detCount},{targetRadius.ToString(CultureInfo.InvariantCulture)}," +
                                    $"{nsa.attempts},{metrics.TP},{metrics.FP}";
                        sw.WriteLine(line);
                    }
                }
            }
        }

        /*@ Test
         4. Rozkład Przestrzenny Detektorów (Histogram Promieni)
         * parametry:
         *  -Przedziały wielkości wyliczonego promienia (np. 0.0-0.2, 0.2-0.4, 0.4-0.6...)
         *  - Liczba detektorów w każdym przedziale ktore osignely dany promien
         */
        public static void RadiusHist(List<Antigen> trainSet, int featuresCount)
        {
            string filePath = "Wykres4_Histogram.csv";
            int[] sizesToTest = { 1000, 5000, 10000, 20000 };
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
      
    }
}
