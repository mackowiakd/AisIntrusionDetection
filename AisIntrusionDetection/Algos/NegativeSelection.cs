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

        // NOWA FUNKCJA: Profiluje dane i zwraca idealne potęgi dla każdej ze 41 kolumn
        private float[] CalculateFeatureExponents(List<Antigen> selfSet, int numberOfFeatures)
        {
            float[] exponents = new float[numberOfFeatures];
            int totalRecords = selfSet.Count;

            for (int i = 0; i < numberOfFeatures; i++)
            {
                // 1. Liczymy średnią dla danej kolumny
                float sum = 0;
                foreach (var packet in selfSet)
                {
                    sum += packet.Data[i];
                }
                float mean = sum / totalRecords;

                // Zabezpieczenie przed dzieleniem przez zero (gdy kolumna ma same zera)
                if (mean < 0.01f) mean = 0.01f;
                if (mean > 0.99f) mean = 0.99f;

                // 2. Magiczny wzór na idealną potęgę dystrybucji
                exponents[i] = (1.0f / mean) - 1.0f;
            }

            return exponents;
        }
        public List<Detector> GenerateDetectors(List<Antigen> selfSet, int numberOfFeatures, int requiredDetectors, float minAllowedRadius)
        {
            List<Detector> matureDetectors = new List<Detector>();
            int attempts = 0;
            float[] featureExponents = CalculateFeatureExponents(selfSet, numberOfFeatures);

            Console.WriteLine($"[NSA] Rozpoczynam generowanie {requiredDetectors} detektorów...");

            while (matureDetectors.Count < requiredDetectors)
            {
                attempts++;
                float[] candidateCoordinates = new float[numberOfFeatures];
                for (int i = 0; i < numberOfFeatures; i++)
                {
                    // ZMIANA: Używamy dynamicznej potęgi wyliczonej dla konkretnej kolumny (i)
                    candidateCoordinates[i] = (float)Math.Pow(_random.NextDouble(), featureExponents[i]);
                }

                // Tworzymy kandydata z promieniem 0 (zaraz go nadmuchamy!)
                Detector candidate = new Detector(candidateCoordinates, 0f);

                float nearestSelfDistance = float.MaxValue;
                object syncLock = new object(); // Obiekt do bezpiecznej wielowątkowości

                // Szukamy najbliższego sąsiada ze zdrowego ruchu (Najbliższego Ziemi)
                Parallel.ForEach(selfSet, (selfPacket) =>
                {
                    float dist = candidate.CalculateDistance(selfPacket.Data);

                    // Aktualizacja minimum z zabezpieczeniem dla wielowątkowości
                    if (dist < nearestSelfDistance)
                    {
                        lock (syncLock)
                        {
                            if (dist < nearestSelfDistance)
                            {
                                nearestSelfDistance = dist;
                            }
                        }
                    }
                });

                // CZY BALON JEST WYSTARCZAJĄCO DUŻY?
                if (nearestSelfDistance >= minAllowedRadius)
                {
                    // BINGO! Ustawiamy promień tak, by detektor "dotykał" najbliższego zdrowego pakietu
                    // Odejmujemy mikroskopijny margines (np. 0.001f) dla bezpieczeństwa!
                    candidate.Radius = nearestSelfDistance - 0.001f;

                    matureDetectors.Add(candidate);
                }
            }
                Console.WriteLine($"[NSA] Ukończono! Wygenerowano {requiredDetectors} detektorów w {attempts} próbach losowania.");
            return matureDetectors;

        }
    }
}
