using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AisIntrusionDetection.Algos.ModelEvaluator;

namespace AisIntrusionDetection.Interop
{
    public static class ResultsLogger
    {
        /* Ta klasa jest odpowiedzialna za logowanie wyników do pliku CSV. 
         * wszystkie wykresy maja miec tu metody opwiedzialne za zebranie danych i zapis do pliku.
         */
        public static void VDetSensitvity(string filePath, int detectorsCount, float minRadius, int attempts, EvaluationMetrics metrics)
        {
            bool fileExists = File.Exists(filePath);

            // Używamy StreamWriter w bloku using (automatycznie zamyka plik)
            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                // Jeśli plik jest nowy, dodaj nagłówki kolumn
                if (!fileExists)
                {
                    sw.WriteLine("DetectorsCount,MinRadius,Attempts,TP,FP,TN,FN,Accuracy");
                }

                // Wpisujemy wiersz z danymi. InvariantCulture zapewnia kropki zamiast przecinków w ułamkach
                string line = $"{detectorsCount},{minRadius.ToString(CultureInfo.InvariantCulture)},{attempts}," +
                              $"{metrics.TP},{metrics.FP},{metrics.TN},{metrics.FN},{metrics.Accuracy.ToString(CultureInfo.InvariantCulture)}";

                sw.WriteLine(line);
            }
        }

        public static void DetectorsCount(string filePath, List<int> det_sets, EvaluationMetrics metrics)
        {

        }
    }
}
