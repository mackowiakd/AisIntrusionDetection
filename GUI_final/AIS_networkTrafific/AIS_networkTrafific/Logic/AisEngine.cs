using AisIntrusionDetection.Algorithms;
using AisIntrusionDetection.Algos;
using AisIntrusionDetection.Interop;
using AisIntrusionDetection.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AisIntrusionDetection.Algos.ModelEvaluator;

namespace AIS_networkTrafific.UI.Logic
{
    public class AisEngine
    {
        // Dane surowe (41D)
        public List<Antigen> FullTrainSet { get; private set; } =new List<Antigen>();
        public List<Antigen> FullTestSet { get; private set; } = new List<Antigen>();

        // Dane po PCA
        public List<Antigen> PcaTrainSet { get; private set; } = new List<Antigen>();
        public List<Antigen> PcaTestSet { get; private set; } = new List<Antigen>();
        public int CurrentPcaDimensions { get; private set; }

        public bool IsDataLoaded => FullTrainSet != null && FullTrainSet.Count > 0;

        // Metoda ładująca dane - wywoływana raz przy starcie lub po wyborze pliku
        
        // Domyślnie ładujemy 20k wierszy z pliku, 5k zdrowych idzie na trening, 10k mieszanych na testy
        public void LoadData(string filePath, int maxRowsToLoad = 20000)
        {
            int trainSize = (int)(maxRowsToLoad * 0.35); // 35% zdrowych na trening
            int testSize = (int)(maxRowsToLoad * 0.5); // 50% na testy
            var loader = new DataLoader(filePath, maxRowsToLoad, 41);
            var allData = loader.LoadData();

            // Dzielimy na zdrowe i ataki
            var allNormal = allData.Where(a => a.Attack == false).ToList();
            var allAttacks = allData.Where(a => a.Attack == true).ToList();

            // Zbiór Treningowy: TYLKO zdrowy ruch (z definicji NSA)
            FullTrainSet = allNormal.Take(trainSize).ToList();

            // Zbiór Testowy: Ruch zdrowy (którego system nie widział) + Ataki
            var unseenNormal = allNormal.Skip(trainSize).ToList();

            var mixedTestSet = new List<Antigen>();
            mixedTestSet.AddRange(unseenNormal);
            mixedTestSet.AddRange(allAttacks);

            // Pobieramy żądaną pulę testową (będzie miała i zdrowe, i wirusy)
            FullTestSet = mixedTestSet.Take(testSize).ToList();
        }

        // Metoda wykonująca PCA
        public void RunPcaTransformation(int targetDimensions)
        {
            CurrentPcaDimensions = targetDimensions;
            var pca = new PcaTransformer();
            pca.Fit(FullTrainSet, targetDimensions);

            PcaTrainSet = pca.Transform(FullTrainSet);
            PcaTestSet = pca.Transform(FullTestSet);

            // KRYTYCZNE: Normalizacja po PCA
            pca.NormalizePcaDataTo01(PcaTrainSet, PcaTestSet, targetDimensions);
        }

     

        // --- Metody do obsługi Algorytmów ---

        public EvaluationMetrics RunV0(float radius, int count)
        {
            var nsa = new NegativeSelection();
            var detectors = nsa.GenerateDetectors_v0(FullTrainSet, 41, count, radius);
            return new ModelEvaluator().Evaluate(detectors, FullTestSet);
        }

        public EvaluationMetrics RunV2(int count, float minRadius, bool usePca)
        {
            var nsa = new NegativeSelection();
            var train = usePca ? PcaTrainSet : FullTrainSet;
            var test = usePca ? PcaTestSet : FullTestSet;
            var dim = usePca ? CurrentPcaDimensions : 41;

            var detectors = nsa.GenerateDetectors_v2(train, dim, count, minRadius);
            return new ModelEvaluator().Evaluate(detectors, test);
        }

        public EvaluationMetrics RunV3Pca(int count, float minRadius)
        {
            var nsa = new NegativeSelection();
            // Wywołujemy Twoją nową metodę V3 dedykowaną dla PCA
            var detectors = nsa.GenerateDetectors_V3pca(PcaTrainSet, CurrentPcaDimensions, count, minRadius);
            return new ModelEvaluator().Evaluate(detectors, PcaTestSet);
        }
    }
}