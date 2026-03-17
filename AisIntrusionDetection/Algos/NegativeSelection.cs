using AisIntrusionDetection.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AisIntrusionDetection.Models
{
    //Główna logika: generowanie detektorów, liczenie odległości i sprawdzanie, czy detektor pokrywa się ze zdrowym ruchem.
    public class NegativeSelection
    {
        private Random _random = new Random();

        /*musi przyjmowac liste obiekto typu anitigen  */

        // ZMIANA: Z List<Antigen[]> na List<Antigen>
        public List<Detector> GenerateDetectors(List<Antigen> selfSet, int numberOfFeatures, int requiredDetectors, float radius)
        {
            List<Detector> matureDetectors = new List<Detector>();
            int attempts = 0;

            Console.WriteLine($"[NSA] Rozpoczynam generowanie {requiredDetectors} detektorów...");

            while (matureDetectors.Count < requiredDetectors)
            {
                attempts++;
                float[] candidateCoordinates = new float[numberOfFeatures];
                for (int i = 0; i < numberOfFeatures; i++)
                {
                    candidateCoordinates[i] = (float)_random.NextDouble();
                }

                Detector candidate = new Detector(candidateCoordinates, radius);
                bool isMatch = false;

                Parallel.ForEach(selfSet, (selfPacket, state) =>
                {
                    // Przekazujemy do IsMatch SAMĄ tablicę z cechami (Data)
                    if (candidate.IsMatch(selfPacket.Data))
                    {
                        isMatch = true;
                        state.Break();
                    }
                });

                if (!isMatch)
                {
                    matureDetectors.Add(candidate);
                }
            }

            Console.WriteLine($"[NSA] Ukończono! Wygenerowano {requiredDetectors} detektorów w {attempts} próbach losowania.");
            return matureDetectors;
        }
    }
}
