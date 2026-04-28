using AisIntrusionDetection.Algorithms;
using AisIntrusionDetection.Algos;
using AisIntrusionDetection.Interop;
using AisIntrusionDetection.Models;
using System;
using System.Collections.Generic;
using System.IO;
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

        public int OriginalFeatureCount { get; private set; }
        public bool IsDataLoaded => FullTrainSet != null && FullTrainSet.Count > 0;
        private int DetectFeatureCount(string filePath)
        {
            int attributeCount = 0;
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string lowerLine = line.Trim().ToLower();
                    if (lowerLine.StartsWith("@data")) break; // Koniec nagłówka
                    if (lowerLine.StartsWith("@attribute")) attributeCount++;
                }
            }
            // Zwracamy ilość atrybutów minus 1 (bo ostatni to klasa/Label)
            return attributeCount > 0 ? attributeCount  : throw new Exception("Nie znaleziono tagów @attribute w pliku!");
        }

        // Metoda ładująca dane - wywoływana raz przy starcie lub po wyborze pliku

        public void LoadData(string filePath, int maxRowsToLoad = 20000, int trainSize = 5000, int testSize = 10000)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Nie znaleziono pliku: {filePath}");

            // 1. Zliczamy wymiary z nagłówka
            OriginalFeatureCount = DetectFeatureCount(filePath);

            // 2. Ładujemy dane przez C++
            var loader = new DataLoader(filePath, maxRowsToLoad, OriginalFeatureCount);
            var allData = loader.LoadData();

            if (allData == null || allData.Count == 0)
                throw new Exception("Z biblioteki C++ wróciło 0 rekordów. Upewnij się, że TrafficParser.dll jest w folderze!");

            var allNormal = allData.Where(a => a.Attack == false).ToList();
            var allAttacks = allData.Where(a => a.Attack == true).ToList();

            if (allNormal.Count < trainSize)
                throw new Exception($"Za mało zdrowego ruchu w pliku. Chcesz {trainSize}, a jest {allNormal.Count}.");

            FullTrainSet = allNormal.Take(trainSize).ToList();

            var unseenNormal = allNormal.Skip(trainSize).ToList();
            var mixedTestSet = new List<Antigen>();
            mixedTestSet.AddRange(unseenNormal);
            mixedTestSet.AddRange(allAttacks);

            if (mixedTestSet.Count < testSize) testSize = mixedTestSet.Count; // Zabezpieczenie

            FullTestSet = mixedTestSet.Take(testSize).ToList();
        }

        // Metoda wykonująca PCA na danych - wywoływana po załadowaniu danych i przed uruchomieniem algorytmów
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
            var detectors = nsa.GenerateDetectors_v0(FullTrainSet, OriginalFeatureCount-1, count, radius);
            return new ModelEvaluator().Evaluate(detectors, FullTestSet);
        }

        public EvaluationMetrics RunV1(float radius, int count)
        {
            var nsa = new NegativeSelection();
            var detectors = nsa.GenerateDetectors_v1(FullTrainSet, OriginalFeatureCount-1  , count, radius);
            return new ModelEvaluator().Evaluate(detectors, FullTestSet);
        }

        public EvaluationMetrics RunV2(int count, float minRadius, bool usePca)
        {
            var nsa = new NegativeSelection();
            var train = usePca ? PcaTrainSet : FullTrainSet;
            var test = usePca ? PcaTestSet : FullTestSet;
            var dim = usePca ? CurrentPcaDimensions : OriginalFeatureCount-1;

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