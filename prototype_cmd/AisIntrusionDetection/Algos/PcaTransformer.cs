using AisIntrusionDetection.Algorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace AisIntrusionDetection.Algos
{
    public class PcaTransformer
    {
        public Vector<double> _meanVector;
        public Matrix<double> _projectionMatrix; // Nasza macierz $W$ (top $K$ wektorów własnych)
        int _targetDimensions; // Zmienna mówiąca, do ilu wymiarów schodzimy (np. 10)


        //wyliczenie wektora średnich i macierzy przejścia.
        public void Fit(List<Antigen> trainingData, int targetDim)
        {
            _targetDimensions = targetDim;

            // ZABEZPIECZENIE WYDAJNOŚCIOWE: 
            // Do wyliczenia macierzy Kowariancji nie potrzebujemy 50 tysięcy rekordów.
            // 3000 to aż nadto do znalezienia idealnych osi PCA na 41 wymiarach.
            int maxSamplesForFit = 3000;

            // Jeśli mamy więcej niż limit, bierzemy losową próbkę (ze stałym ziarnem, żeby testy były powtarzalne!)
            var sampleData = trainingData.Count > maxSamplesForFit
                ? trainingData.OrderBy(x => new Random(42).Next()).Take(maxSamplesForFit).ToList()
                : trainingData;

            int numSamples = sampleData.Count;
            int numFeatures = sampleData[0].Data.Length;

            // 1. Inicjalizacja macierzy DANYCH na podstawie próbki
            var matrixX = DenseMatrix.Create(numSamples, numFeatures, (i, j) => sampleData[i].Data[j]);
            _meanVector = Vector<double>.Build.Dense(numFeatures, 0.0);

            // 2. Centrowanie (Elegancki sposób MathNet)
            for (int j = 0; j < numFeatures; j++)
            {
                _meanVector[j] = matrixX.Column(j).Average();
            }

            var centeredMatrix = DenseMatrix.Create(numSamples, numFeatures, 0.0);
            for (int i = 0; i < numSamples; i++)
            {
                centeredMatrix.SetRow(i, matrixX.Row(i) - _meanVector);
            }

            // 3. Macierz Kowariancji (Teraz wykona się błyskawicznie, bo max to 3000 wierszy!)
            var covarianceMatrix = centeredMatrix.TransposeThisAndMultiply(centeredMatrix) / (numSamples - 1);

            // 4. Dekompozycja EVD
            var evd = covarianceMatrix.Evd();
            var eigenValues = evd.EigenValues;
            var eigenVectors = evd.EigenVectors;

            // 5. Sortowanie i Tworzenie Macierzy W
            var sortedIndices = eigenValues.Select((value, index) => new { Value = value, Index = index })
                                           .OrderByDescending(x => x.Value.Real)
                                           .Take(targetDim)
                                           .Select(x => x.Index)
                                           .ToArray();

            _projectionMatrix = DenseMatrix.Build.DenseOfColumns(sortedIndices.Select(i => eigenVectors.Column(i)).ToArray());
        }
        /*metoda  przyje dane wejściowe (zbiór 41-wymiarowy) i zwraca dane wyjściowe (zbiór zredukowany).*/
        public List<Antigen> Transform(List<Antigen> trainingData)
        {
            // Przygotowujemy nową listę na skompresowane pakiety
            List<Antigen> transformedData = new List<Antigen>();

            foreach (var packet in trainingData)
            {
                // 1. Zbuduj 41-wymiarowy wektor dla TEGO JEDNEGO pakietu
                double[] dataArray = new double[packet.Data.Length];
                for (int i = 0; i < packet.Data.Length; i++)
                {
                    dataArray[i] = (double)packet.Data[i];
                }
                Vector<double> vector = Vector<double>.Build.Dense(dataArray);

                // 2. Centrowanie: odejmujemy wektor średnich wyliczony w fazie FIT
                Vector<double> centredVector = vector - _meanVector;

                // 3. KOMPRESJA (PCA): Mnożymy 1x41 przez macierz 41x10
                // MathNet wypluje nam piękny, nowiutki wektor o długości 10!
                Vector<double> compressedVector = centredVector * _projectionMatrix;

                // 4. Pakujemy wynik z powrotem do obiektu Antigen
                float[] newData = new float[_targetDimensions];
                for (int i = 0; i < _targetDimensions; i++)
                {
                    // MathNet liczy w double, a nasz Antigen używa float
                    newData[i] = (float)compressedVector[i];
                }

                Antigen newPacket = new Antigen(newData,packet.isAnomaly()) ;

               
                transformedData.Add(newPacket);
            }

            return transformedData;
        }

        public void NormalizePcaDataTo01(List<Antigen> train, List<Antigen> test, int dimensions)
        {
            for (int i = 0; i < dimensions; i++)
            {
                float min = train.Min(x => x.Data[i]);
                float max = train.Max(x => x.Data[i]);
                float range = max - min;
                if (range == 0) range = 1f;

                foreach (var packet in train)
                    packet.Data[i] = (packet.Data[i] - min) / range;

                foreach (var packet in test)
                {
                    float scaled = (packet.Data[i] - min) / range;
                    if (scaled < 0f) scaled = 0f;
                    if (scaled > 1f) scaled = 1f;
                    packet.Data[i] = scaled;
                }
            }
        }
    }
    
}
