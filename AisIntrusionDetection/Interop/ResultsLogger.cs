using AisIntrusionDetection.Algorithms;
using AisIntrusionDetection.Algos;
using AisIntrusionDetection.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


        /*@ Test 
         * 
         2. Krzywa Uczenia (Wpływ rozmiaru populacji detektorów)
         * parametry: 
         *  - Liczba wygenerowanych detektorow (np. 1k, 5k, 10k, 20k)
         *  - Oś Y1: Accuracy ; Oś Y2: Wykryte Ataki (TP)
         *
         3.Analiza progu czułości V-Detectora (Kompromis TP vs FP)
         * parametry: 
         *  - minRadius, 
         *  - Liczba pakietów (Jedna linia dla TP, druga dla FP)
         * 
         * */
        public void LogToCsv_DetSensitvLCurve( int detectorsCount, float minRadius, int attempts, EvaluationMetrics metrics)
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

        public void run_DetSensitvity( EvaluationMetrics metrics, List<Antigen> trainSet, List<Antigen> testSet, int featuresCount )
        {
            int[] sizesToTest = { 1000, 5000, 10000, 20000 };
            
            foreach (int numDetectors in sizesToTest)
            {
                float radius = 0.1f;
                NegativeSelection nsa = new NegativeSelection();

                // Uczymy
                List<Detector> detectors = nsa.GenerateDetectors_v2(trainSet, featuresCount - 1, numDetectors, radius);

                // Testujemy (pobieramy obiekt metrics)
                ModelEvaluator evaluator = new ModelEvaluator();
                 metrics = evaluator.Evaluate(detectors, testSet);

                // Zapisujemy przez naszą nową klasę!
                // Uwaga: musisz dodać do klasy NegativeSelection publiczną właściwość 'Attempts', 
                // która trzyma liczbę prób z ostatniego uruchomienia, żeby ją tu przekazać.
                ResultsLogger.LogToCsv_DetSensitvLCurve(csvPath, numDetectors, radius, nsa.LastAttempts, metrics);
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
           

            bool fileExists = File.Exists(filePath);

            // Używamy StreamWriter w bloku using (automatycznie zamyka plik)
            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                // Jeśli plik jest nowy, dodaj nagłówki kolumn
                if (!fileExists)
                {
                    sw.WriteLine("RadiusHist,MinRadius,Attempts,TP,FP,TN,FN,Accuracy");
                }

                // Wpisujemy wiersz z danymi. InvariantCulture zapewnia kropki zamiast przecinków w ułamkach
                string line = $"{detCount},{minRadius.ToString(CultureInfo.InvariantCulture)},{attempts}," +
                              $"{metrics.TP},{metrics.FP},{metrics.TN},{metrics.FN},{metrics.Accuracy.ToString(CultureInfo.InvariantCulture)}";

                sw.WriteLine(line);
            }
        }
    }
}
