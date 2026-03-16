using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AisIntrusionDetection.Algorithms
{
    // Klasa reprezentująca "limfocyt" (detektor anomalii)
    public class Detector
    {
        public float[] Coordinates { get; set; } // Środek detektora (współrzędne)
        public float Radius { get; set; }        // Zasięg detekcji

        public Detector(float[] coordinates, float radius)
        {
            Coordinates = coordinates;
            Radius = radius;
        }

        // Funkcja sprawdzająca czy pakiet znajduje się w zasięgu detektora (MATCH)
        public bool IsMatch(float[] networkPacket)
        {
            float distance = 0;

            // Twierdzenie Pitagorasa dla wielu wymiarów (Odległość euklidesowa)
            for (int i = 0; i < Coordinates.Length; i++)
            {
                float diff = Coordinates[i] - networkPacket[i];
                distance += diff * diff;
            }
            distance = (float)Math.Sqrt(distance);

            // Jeśli odległość jest mniejsza lub równa promieniowi - mamy Match!
            return distance <= Radius;
        }
    }
}
