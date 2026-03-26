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
        public int requiredDetectors;
        public float actualRadius { get; private set; }
        public int attempts{ get; private set; }

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
            this.attempts = 0;
            this.requiredDetectors = requiredDetectors;
            float[] featureExponents = CalculateFeatureExponents(selfSet, numberOfFeatures);

            Console.WriteLine($"[NSA] Generowanie {requiredDetectors} det. (Promień >= {minAllowedRadius:F4})...");

            float minAllowedRadiusSq = minAllowedRadius * minAllowedRadius;

            // NOWOŚĆ: Licznik nieudanych prób Z RZĘDU (Early Stopping)
            int consecutiveFails = 0;

            // Jeśli 2000 razy pod rząd algorytm nie znajdzie luki, poddaje się NATYCHMIAST.
            int maxConsecutiveFails = 2000;
            float[] candidateCoordinates = new float[numberOfFeatures];
            while (matureDetectors.Count < requiredDetectors)
            {
                this.attempts++;

                // SZYBKI WENTYL BEZPIECZEŃSTWA
                if (consecutiveFails > maxConsecutiveFails)
                {
                    Console.WriteLine($"[NSA] Przestrzeń całkowicie zablokowana! Poddaję się po {maxConsecutiveFails} porażkach z rzędu. (Zrobiono {matureDetectors.Count}/{requiredDetectors})");
                    break; // Wychodzi z pętli w ułamek sekundy!
                }

               
                for (int i = 0; i < numberOfFeatures; i++)
                {
                    candidateCoordinates[i] = (float)Math.Pow(_random.NextDouble(), featureExponents[i]);
                }

                float nearestSelfDistanceSq = float.MaxValue;

                foreach (var selfPacket in selfSet)
                {
                    float distSq = 0f;
                    float[] selfData = selfPacket.Data;

                    for (int j = 0; j < numberOfFeatures; j++)
                    {
                        float diff = candidateCoordinates[j] - selfData[j];
                        distSq += diff * diff;
                    }

                    if (distSq < nearestSelfDistanceSq)
                    {
                        nearestSelfDistanceSq = distSq;
                    }
                }

                // Czy detektor przeżył?
                if (nearestSelfDistanceSq >= minAllowedRadiusSq)
                {
                    Detector candidate = new Detector(candidateCoordinates, 0f);
                    candidate.Radius = (float)Math.Sqrt(nearestSelfDistanceSq) - 0.001f;
                    this.actualRadius = candidate.Radius;
                    matureDetectors.Add(candidate);

                    // SUKCES! Resetujemy licznik porażek do zera
                    consecutiveFails = 0;
                }
                else
                {
                    // PORAŻKA: Zwiększamy licznik błędów pod rząd
                    consecutiveFails++;
                }
            }

            return matureDetectors;
        }
        /*public List<Detector> GenerateDetectors_v2(List<Antigen> selfSet, int numberOfFeatures, int requiredDetectors, float minAllowedRadius)
        {
            List<Detector> matureDetectors = new List<Detector>();
            this.attempts = 0;
            // ZMIANA: Określamy limit prób (np. 10 000 prób na każdy wymagany detektor)
            int maxAttemptsLimit = requiredDetectors * 10000;
            this.requiredDetectors = requiredDetectors;
            float[] featureExponents = CalculateFeatureExponents(selfSet, numberOfFeatures);

            Console.WriteLine($"[NSA] Rozpoczynam generowanie {requiredDetectors} detektorów...");

            while (matureDetectors.Count < requiredDetectors)
            {
                attempts++;
                float[] candidateCoordinates = new float[numberOfFeatures];
                attempts++;

                // WENTYL BEZPIECZEŃSTWA:
                if (attempts > maxAttemptsLimit)
                {
                    Console.WriteLine($"\n[UWAGA] Osiągnięto limit prób ({maxAttemptsLimit}). Wygenerowano tylko {matureDetectors.Count}/{requiredDetectors} detektorów. Przestrzeń jest zbyt ciasna na ten promień!");
                    break; // Przerywamy pętlę i zwracamy to, co mamy!
                }
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

                    // A minimum z zabezpieczeniem dla wielowątkowości
                    if (dist < nearestSelfDistance)
                    {
                        lock (syncLock) // po co mutex skoro kzady watek dostej inny detektor? 
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
                    this.actualRadius = candidate.Radius;

                    matureDetectors.Add(candidate);
                }
            }
           
            Console.WriteLine($"[NSA] Ukończono! Wygenerowano {requiredDetectors} detektorów w {attempts} próbach losowania.");
            return matureDetectors;

        }

        */

        /* Wersja 1 (Profilowanie): Adaptacyjny rozkład potęgowy + sztywny promień.*/
        public List<Detector> GenerateDetectors_v1(List<Antigen> selfSet, int numberOfFeatures, int requiredDetectors, float Radius)
        {
            List<Detector> matureDetectors = new List<Detector>();
            this.attempts = 0;
            this.requiredDetectors = requiredDetectors;
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
        public List<Detector> GenerateDetectors_v0(List<Antigen> selfSet, int numberOfFeatures, int requiredDetectors, float radius)
        {
            List<Detector> matureDetectors = new List<Detector>();
            this.attempts = 0;
            this.requiredDetectors = requiredDetectors;
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
                    if (candidate.IsMatch(selfPacket.Data))
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

        /*
         * zadaniem tej metody jest rzucenie (np. 2000) losowych rzutek w przestrzeń i sprawdzenie
         *  jaka jest największa możliwa dziura między zdrowymi pakietami.
         *  => moze jakis procentowy rozklad odległości między zdrowymi pakietami?
         */
        // Dodaj to wewnątrz klasy NegativeSelection
        public float CalculateRobustMaxRadius(List<Antigen> selfSet, int numberOfFeatures, int sampleSize = 2000)
        {
            //Najpierw wyliczamy potęgi dla przestrzeni!
            float[] featureExponents = CalculateFeatureExponents(selfSet, numberOfFeatures);

            // ZMIANA: Zamiast trzymać tylko Max, zapisujemy WSZYSTKIE znalezione luki
            List<float> foundRadii = new List<float>();

            for (int i = 0; i < sampleSize; i++)
            {
                float[] candidateCoordinates = new float[numberOfFeatures];
                for (int j = 0; j < numberOfFeatures; j++)
                {
                    candidateCoordinates[j] = (float)_random.NextDouble();
                }

                Detector candidate = new Detector(candidateCoordinates, 0f);
                float nearestSelfDistance = float.MaxValue;
                object syncLock = new object();

                Parallel.ForEach(selfSet, (selfPacket) =>
                {
                    float dist = candidate.CalculateDistance(selfPacket.Data);
                    if (dist < nearestSelfDistance)
                    {
                        lock (syncLock)
                        {
                            if (dist < nearestSelfDistance) nearestSelfDistance = dist;
                        }
                    }
                });

                foundRadii.Add(nearestSelfDistance);
            }

            // STATYSTYKA ROZKŁADU:
            foundRadii.Sort(); // Sortujemy od najmniejszej do największej dziury

            float min = foundRadii.First();
            float max = foundRadii.Last(); // To był nasz stary, podatny na anomalie Max
            float median = foundRadii[foundRadii.Count / 2]; // Środek rozkładu

            // 95. Percentyl: Odcinamy 5% największych anomalii!
            float percentile95 = foundRadii[(int)(foundRadii.Count * 0.95)];

            Console.WriteLine($"[Statystyka Przestrzeni]:");
            Console.WriteLine($" - Najmniejsza luka: {min:F4}");
            Console.WriteLine($" - Mediana (Typowa luka): {median:F4}");
            Console.WriteLine($" - 95. Percentyl: {percentile95:F4} (Uznajemy to za bezpieczny, twardy limit!)");
            Console.WriteLine($" - Absolutny Max (Możliwa anomalia!): {max:F4}\n");

            // Zwracamy 95. percentyl zamiast Maxa. 
            return percentile95;
        }
    }


}
