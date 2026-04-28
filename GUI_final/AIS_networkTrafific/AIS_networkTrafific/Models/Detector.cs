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

        // NOWA FUNKCJA: Wyciągamy samą matematykę Pitagorasa na zewnątrz
        public float CalculateDistance(float[] networkPacket)
        {
            float distance = 0;
            for (int i = 0; i < Coordinates.Length; i++)
            {
                float diff = Coordinates[i] - networkPacket[i];
                distance += diff * diff;
            }
            return (float)Math.Sqrt(distance);
        }

        public bool IsMatch(float[] networkPacket)
        {
            // Match jest wtedy, gdy policzona odległość jest mniejsza niż promień
            return CalculateDistance(networkPacket) <= Radius;
        }
    }
}
