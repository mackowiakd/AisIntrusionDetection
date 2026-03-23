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
    
    public class ResultsLogger
    {
        
        public string filePath { get; set; }   

        public ResultsLogger(string fileName) {
            this.filePath = fileName;
        }
        /* @ Test
         * 
         1. Wpływ Profilowania na Koszt Generowania (Problem Pustej Przestrzeni)
         * parametry: 
         *  - Oś X Wersja Algorytm (Wersja 0: Ślepe losowanie vs. Wersja 1: Profilowanie)
         *  - Oś Y Logarytmiczna liczba prób (Attempts) potrzebnych do wygenerowania 5000 detektorów
         * 
         * */
        public void ProfilingVsAttempts(List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {
       
            string headerLine = "Version,DetectorsCount,Radius";
            bool fileExists = File.Exists(this.filePath);

            int[] sizesToTest = { 1000, 5000, 10000, 20000 };

            foreach (int detCount in sizesToTest)
            {
                float radius = 0.25f;
                NegativeSelection nsa = new NegativeSelection();

          
                nsa.GenerateDetectors_v0(trainSet, featuresCount - 1, detCount, radius);
                int attemptsV0 = nsa.attempts;
                nsa.GenerateDetectors_v1(trainSet, featuresCount - 1, detCount, radius);
                int attemptsV1 = nsa.attempts;


                // Testujemy (pobieramy obiekt metrics)
                ModelEvaluator evaluator = new ModelEvaluator();


                // Używamy StreamWriter w bloku using (automatycznie zamyka plik)
                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    // Jeśli plik jest nowy, dodaj nagłówki kolumn
                    if (!fileExists)
                    {
                        sw.WriteLine(headerLine);
                        fileExists = true;
                    }

                    // Wpisujemy wiersz z danymi. InvariantCulture zapewnia kropki zamiast przecinków w ułamkach
                    string line = $" V.0,{detCount},{radius.ToString(CultureInfo.InvariantCulture)},{attemptsV0}";
                    sw.WriteLine(line);
                    line = $" V.1,{detCount},{radius.ToString()},{attemptsV1}";
                    sw.WriteLine(line);
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
        public void lCurve(EvaluationMetrics metrics, List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {
            int[] sizesToTest = { 1000, 5000, 10000, 20000 };
            bool fileExists = File.Exists(this.filePath);
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
                        sw.WriteLine("DetectorsCount,Radius,TP ,Accuracy");
                        fileExists = true;
                    }

                    // Wpisujemy wiersz z danymi. InvariantCulture zapewnia kropki zamiast przecinków w ułamkach
                    string line = $"{detCount},{radius.ToString(CultureInfo.InvariantCulture)}" +
                                  $"{metrics.TP}, {metrics.Accuracy.ToString(CultureInfo.InvariantCulture)}";

                    sw.WriteLine(line);
                }
            }
        }
       

       
         /*
         3.Analiza progu czułości V-Detectora(Kompromis TP vs FP)
         * parametry: 
         *  - minRadius, 
         *  - Liczba pakietów(Jedna linia dla TP, druga dla FP)
         */
        public void SensitivityThresholdAnalysis(EvaluationMetrics metrics, List<Antigen> trainSet, List<Antigen> testSet, int featuresCount)
        {
            int[] sizesToTest = { 1000, 5000, 10000, 20000 };
            float[] radiusSize = { 0.05f, 0.1f, 0.15f, 0.2f };

            bool fileExists = File.Exists(this.filePath);

            foreach (int detCount in sizesToTest)
            {
                foreach (float minRadius in radiusSize)
                {
                    NegativeSelection nsa = new NegativeSelection();

                    // Uczymy
                    List<Detector> detectors = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detCount, minRadius);

                    // Testujemy (pobieramy obiekt metrics)
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

                        // Wpisujemy wiersz z danymi. InvariantCulture zapewnia kropki zamiast przecinków w ułamkach
                        string line = $"{detCount},{minRadius.ToString(CultureInfo.InvariantCulture)}" +
                                      $"{metrics.TP}, {metrics.FP}";

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
        public void RadiusHist(List<Antigen> trainSet, int featuresCount)
        {
            int[] sizesToTest = { 1000, 5000, 10000, 20000 };
            bool fileExists = File.Exists(this.filePath);

            foreach (int detCount in sizesToTest)
            {
                NegativeSelection nsa = new NegativeSelection();

                // 1. GENERUJEMY TYLKO RAZ DLA DANEJ WIELKOŚCI!
                // Ustawiamy próg na mikroskopijny (0.01f), żeby złapać absolutnie wszystkie balony do statystyk
                List<Detector> detectors = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, detCount, 0.01f);

                // 2. ROBIMY HISTOGRAM (LINQ grupuje i zlicza detektory w ułamku sekundy)
                int bin1 = detectors.Count(d => d.Radius > 0.0f && d.Radius <= 0.05f);
                int bin2 = detectors.Count(d => d.Radius > 0.05f && d.Radius <= 0.10f);
                int bin3 = detectors.Count(d => d.Radius > 0.10f && d.Radius <= 0.15f);
                int bin4 = detectors.Count(d => d.Radius > 0.15f && d.Radius <= 0.20f);
                int bin5 = detectors.Count(d => d.Radius > 0.20f); // Wszystkie gigantyczne balony

                // 3. ZAPISUJEMY WYNIK DO EXCELA (Jeden wiersz dla danej populacji)
                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    if (!fileExists)
                    {
                        sw.WriteLine("DetectorsCount,Bin_0_05,Bin_05_10,Bin_10_15,Bin_15_20,Bin_Over_20");
                        fileExists = true;
                    }

                    // Zapisujemy policzone koszyki (biny)
                    string line = $"{detCount},{bin1},{bin2},{bin3},{bin4},{bin5}";
                    sw.WriteLine(line);
                }
            }
        }
      
    }
}
