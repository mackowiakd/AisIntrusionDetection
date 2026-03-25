using AisIntrusionDetection.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AisIntrusionDetection.Algos
{
    public class ModelEvaluator
    {
        public class EvaluationMetrics
        {
            public int TP { get; set; }
            public int FP { get; set; }
            public int TN { get; set; }
            public int FN { get; set; }
            public float Accuracy { get; set; }
        }
        public EvaluationMetrics Evaluate(List<Detector> matureDetectors, List<Antigen> testSet)
        {
            Console.WriteLine($"\n[Evaluator] Rozpoczynam testowanie {testSet.Count}z promieniem {matureDetectors[0].Radius} pakietów na \n{matureDetectors.Count} detektorach...");

            // Używamy bezpiecznych liczników wielowątkowych (Interlocked)
            int truePositives = 0;
            int falsePositives = 0;
            int trueNegatives = 0;
            int falseNegatives = 0;

            // Wielowątkowe testowanie każdego pakietu
            Parallel.ForEach(testSet, antigen =>
            {
                bool alarmRaised = false;

                // Sprawdzamy pakiet we wszystkich detektorach
                foreach (var detector in matureDetectors)
                {
                    if (detector.IsMatch(antigen.Data))
                    {
                        alarmRaised = true;
                        break; // Wystarczy jeden detektor, by podnieść alarm
                    }
                }

                // Bezpieczne dodawanie wyników z wielu rdzeni naraz
                if (alarmRaised && antigen.Attack)
                    Interlocked.Increment(ref truePositives);
                else if (alarmRaised && !antigen.Attack)
                    Interlocked.Increment(ref falsePositives);
                else if (!alarmRaised && !antigen.Attack)
                    Interlocked.Increment(ref trueNegatives);
                else if (!alarmRaised && antigen.Attack)
                    Interlocked.Increment(ref falseNegatives);
            });

            PrintMetrics(truePositives, falsePositives, trueNegatives, falseNegatives, testSet.Count);
            return new EvaluationMetrics
            {
                TP = truePositives,
                FP = falsePositives,
                TN = trueNegatives,
                FN = falseNegatives,
                Accuracy = ((float)(truePositives + trueNegatives) / testSet.Count) * 100.0f
            };
        }

        private void PrintMetrics(int tp, int fp, int tn, int fn, int total)
        {
            int correctPredictions = tp + tn;
            float accuracy = (float)correctPredictions / total * 100.0f;

            Console.WriteLine($"\n========== RAPORT EWALUACJI ==========");
            Console.WriteLine($"Wykryte ataki (True Positives):      {tp}");
            Console.WriteLine($"Fałszywe alarmy (False Positives):   {fp}");
            Console.WriteLine($"Puszczony zdrowy ruch (True Neg.):   {tn}");
            Console.WriteLine($"Przepuszczone ataki (False Neg.):    {fn}");
            Console.WriteLine($"--------------------------------------");
            Console.WriteLine($"TRAFNOŚĆ (ACCURACY):                 {accuracy:F2}%");
            Console.WriteLine($"======================================");
        }
    }
}
