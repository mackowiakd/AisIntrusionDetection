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

        // Funkcja trenująca - wypluwa listę Dojrzałych Detektorów
        public List<Detector> GenerateDetectors(List<float[]> selfSet, int numberOfFeatures, int requiredDetectors, float radius)
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

                // KROK 2 ze schematu: Match (porównanie z Self set)
                bool isMatch = false;
                foreach (var selfPacket in selfSet)
                {
                    if (candidate.IsMatch(selfPacket))
                    {
                        isMatch = true;
                        break; // Zahaczył o zdrowy ruch! Przerywamy sprawdzanie.
                    }
                }

                // KROK 3 ze schematu: Reject / Add to Detector set (M)
                if (isMatch)
                {
                    // MATCH == YES -> REJECT (Nic nie robimy, kandydat jest ignorowany)
                }
                else
                {
                    // MATCH == NO -> Detektor przetrwał! Dodajemy go do armii.
                    matureDetectors.Add(candidate);
                }
            }

            Console.WriteLine($"[NSA] Ukończono! Wygenerowano {requiredDetectors} detektorów w {attempts} próbach losowania.");
            return matureDetectors;
        }
    }
}
