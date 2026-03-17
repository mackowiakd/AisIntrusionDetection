using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AisIntrusionDetection.Algorithms
{

    // Klasa reprezentująca pojedynczy pakiet sieciowy (wiersz z datasetu)
    public class Antigen
    {
        // 1. Znormalizowane cechy (to, co widzi i bada algorytm - nasz float[])
        public float[] Data { get; set; }

        // 2. Oryginalna etykieta z pliku CSV (np. "normal", "neptune", "smurf")
        // To jest kluczowe! Algorytm tego NIE WIDZI podczas działania, 
        // ale my tego użyjemy po fakcie, żeby sprawdzić, czy algorytm miał rację!
        public bool Attack { get; set; }



        public Antigen(float[] features, bool attack)
        {
            Data = features;
            Attack = attack;


        }
    }
}
