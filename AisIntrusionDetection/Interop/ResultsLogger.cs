using AisIntrusionDetection.Algorithms;
using AisIntrusionDetection.Algos;
using AisIntrusionDetection.Models;
using System.Globalization;
using static AisIntrusionDetection.Algos.ModelEvaluator;

namespace AisIntrusionDetection.Interop
{
    /* Ta klasa jest odpowiedzialna za logowanie wyników do pliku CSV. 
     * wszystkie wykresy maja miec tu metody opwiedzialne za zebranie danych i zapis do pliku.
     */
    public class ResultsLogger
    {
        private string csvPath = "eksperymenty_vdetector.csv";
        private string filePath;

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
                    }

                    // Wpisujemy wiersz z danymi. InvariantCulture zapewnia kropki zamiast przecinków w ułamkach
                    string line = $" V.0,{detCount},{radius.ToString()},{attemptsV0}";
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
                            sw.WriteLine("DetectorsCount,Radius,Attempts,TP,FP");
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
        public void RadiusHist( EvaluationMetrics metrics, List<float> radiusInterv, List<float> detCount )
        {
           

        

           
        }

        /*@ Uniwersalna funkcja do logowania wyników do CSV, która może być używana przez różne testy.
        * niech przyjmuje liste parametrow + ich nazw
        */
        public void LogToCsvUniversal(int detectorsCount, float minRadius, int attempts, EvaluationMetrics metrics)
        {
            bool fileExists = File.Exists(this.filePath);

            // Używamy StreamWriter w bloku using (automatycznie zamyka plik)
            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                // Jeśli plik jest nowy, dodaj nagłówki kolumn
                if (!fileExists)
                {
                    sw.WriteLine("RadiusHist,MinRadius,Attempts,TP,FP,TN,FN,Accuracy");
                }

                // Wpisujemy wiersz z danymi. InvariantCulture zapewnia kropki zamiast przecinków w ułamkach
                string line = $"{detectorsCount},{minRadius.ToString(CultureInfo.InvariantCulture)},{attempts}," +
                              $"{metrics.TP},{metrics.FP},{metrics.TN},{metrics.FN},{metrics.Accuracy.ToString(CultureInfo.InvariantCulture)}";

                sw.WriteLine(line);
            }
        }
    }
}
