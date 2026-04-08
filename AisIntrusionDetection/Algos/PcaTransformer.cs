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
            int numSamples = trainingData.Count;
            int numFeatures = trainingData[0].Data.Length;

            // 1. Inicjalizacja macierzy DANYCH (X) oraz wektora ŚREDNICH
            // MathNet potrafi stworzyć macierz podając mu wprost, jak ma wyciągnąć floaty z Antigen!
            var matrixX = DenseMatrix.Create(numSamples, numFeatures, (i, j) => trainingData[i].Data[j]);

            // Inicjalizacja pustego wektora MathNet:
            _meanVector = Vector<double>.Build.Dense(numFeatures, 0.0);

            // 2. Centrowanie (Elegancki sposób MathNet)
            // Zamiast ręcznych pętli, możemy wyciągać całe kolumny i liczyć ich średnią:
            for (int j = 0; j < numFeatures; j++)
            {
                _meanVector[j] = matrixX.Column(j).Average();
            }

            // Odejmujemy średnią od każdego wiersza macierzy
            var centeredMatrix = DenseMatrix.Create(numSamples, numFeatures, 0.0);
            for (int i = 0; i < numSamples; i++)
            {
                centeredMatrix.SetRow(i, matrixX.Row(i) - _meanVector);
            }

            // 3. Macierz Kowariancji
            // Twój kod był tu IDEALNY. TransposeThisAndMultiply to X^T * X
            var covarianceMatrix = centeredMatrix.TransposeThisAndMultiply(centeredMatrix) / (numSamples - 1);

            // 4. Dekompozycja EVD
            var evd = covarianceMatrix.Evd();
            var eigenValues = evd.EigenValues;
            var eigenVectors = evd.EigenVectors;

            // 5. Sortowanie i Tworzenie Macierzy W
            // Ten kod LINQ napisałaś rewelacyjnie!
            var sortedIndices = eigenValues.Select((value, index) => new { Value = value, Index = index })
                                           .OrderByDescending(x => x.Value.Real)
                                           .Take(targetDim)
                                           .Select(x => x.Index)
                                           .ToArray();

            // Budujemy ostateczną macierz projekcji
            _projectionMatrix = DenseMatrix.Build.DenseOfColumns(sortedIndices.Select(i => eigenVectors.Column(i)).ToArray());
        }

    }
    
}
