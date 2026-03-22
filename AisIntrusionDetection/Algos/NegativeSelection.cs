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

        /*Wersja 2 (V-Detector): Adaptacyjny rozkład potęgowy + dynamiczny promień. */
        public List<Detector> GenerateDetectors_v2(List<Antigen> selfSet, int numberOfFeatures, int requiredDetectors, float minAllowedRadius)
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


                if (nearestSelfDistance >= minAllowedRadius)
                {
                    // Ustaw promień tak, by detektor "dotykał" najbliższego zdrowego pakietu
                    // Odejmujemy mikroskopijny margines (np. 0.001f) dla bezpieczeństwa!
                    candidate.Radius = nearestSelfDistance - 0.001f;

                    matureDetectors.Add(candidate);
                }
            }
            Console.WriteLine($"[NSA] Ukończono! Wygenerowano {requiredDetectors} detektorów w {attempts} próbach losowania.");
            return matureDetectors;

        }

        /* Wersja 1 (Profilowanie): Adaptacyjny rozkład potęgowy + sztywny promień.*/
        public List<Detector> GenerateDetectors_v1(List<Antigen> selfSet, int numberOfFeatures, int requiredDetectors, float Radius)
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

                // Tworzymy kandydata z promieniem podanym 
                Detector candidate = new Detector(candidateCoordinates, Radius);
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

        /*Wersja 0 (Baseline): Ślepe losowanie + sztywny promień.*/
        public List<Detector> GenerateDetectors_v0(List<float[]> selfSet, int numberOfFeatures, int requiredDetectors, float radius)
        {
            List<Detector> matureDetectors = new List<Detector>();
            int attempts = 0; // Licznik prób, żeby sprawdzić jak bardzo algorytm się męczy

            Console.WriteLine($"[NSA] Rozpoczynam generowanie {requiredDetectors} detektorów...");

            // Pętla kręci się, aż nie wyhodujemy wymaganej liczby detektorów
            while (matureDetectors.Count < requiredDetectors)
            {
                attempts++;

                // KROK 1 ze schematu: Generate candidates (C)
                // Losujemy kandydata w przestrzeni znormalizowanej (od 0.0 do 1.0)
                float[] candidateCoordinates = new float[numberOfFeatures];
                for (int i = 0; i < numberOfFeatures; i++)
                {
                    candidateCoordinates[i] = (float)_random.NextDouble();
                }

                Detector candidate = new Detector(candidateCoordinates, radius);
                bool isMatch = false;

                Parallel.ForEach(selfSet, (selfPacket, state) =>
                {
                    if (candidate.IsMatch(selfPacket))
                    {
                        isMatch = true;
                        state.Break(); // To przerywa pętle na wszystkich rdzeniach, jeśli choć jeden znalazł wirusa (oszczędność czasu!)
                    }
                });

                // Zapis lub odrzucenie
                if (!isMatch)
                {
                    matureDetectors.Add(candidate); // MATCH == NO -> Detektor przeżywa
                }
            }

            Console.WriteLine($"[NSA] Ukończono! Wygenerowano {requiredDetectors} detektorów w {attempts} próbach losowania.");
            return matureDetectors;
        }

    }


}
