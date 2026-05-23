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

        public void LoadData(string filePath, ref int requestedTrainSize, int requestedTestSize)
        {
            int originalTrainCount = 25192;
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Nie znaleziono pliku: {filePath}");

            // 1. Zliczamy wymiary z nagłówka
            OriginalFeatureCount = DetectFeatureCount(filePath);

            // 2. Ładujemy dane przez C++
            // 100 000 to bezpieczny, wbudowany limit wewnętrzny silnika dla C++, 
            // aby wciągnął cały sklejony plik bez zająknięcia.
            var loader = new DataLoader(filePath, 100000, OriginalFeatureCount);
            var allData = loader.LoadData();

            if (allData == null || allData.Count == 0)
                throw new Exception("Z biblioteki C++ wróciło 0 rekordów. Upewnij się, że TrafficParser.dll jest w folderze!");

            // ==========================================
            // ETAP 1: TWARDY PODZIAŁ (Kategoryczny zakaz tasowania!)
            // ==========================================
            // Odcinamy to, co było starym plikiem TRAIN
            var rawTrainData = allData.Take(originalTrainCount).ToList();

            // Reszta to nasz wklejony plik KDDTest+
            var rawTestData = allData.Skip(originalTrainCount).ToList();

            Random rng = new Random(42); // Stałe ziarno dla powtarzalności

            // ==========================================
            // ETAP 2: PRZYGOTOWANIE TRENINGU (Tylko z rawTrainData!)
            // ==========================================
            // Z oryginalnego zbioru treningowego wyciągamy TYLKO zdrowy ruch.
            // Tutaj TASUJEMY, żeby algorytm dostał dobry przekrój do zbudowania PCA.
            var normalTrainData = rawTrainData
                .Where(a => a.Attack == false)
                .OrderBy(x => rng.Next())
                .ToList();

            // HACK: Jeśli trainSize wynosi -1, bierzemy absolutnie wszystko!
            int actualTrainSize = requestedTrainSize == -1 ? normalTrainData.Count : requestedTrainSize;
            requestedTrainSize = actualTrainSize; //updating in GUI
            if (normalTrainData.Count < actualTrainSize)
                throw new Exception($"Za mało zdrowego ruchu. Chcesz {actualTrainSize}, a jest {normalTrainData.Count}.");

            FullTrainSet = normalTrainData.Take(actualTrainSize).ToList();


            // ==========================================
            // ETAP 3: PRZYGOTOWANIE TESTU 
            // ==========================================
            // Tasujemy cały zbiór testowy, by ataki i zdrowy ruch leciały naprzemiennie
            FullTestSet = rawTestData.OrderBy(x => rng.Next()).ToList();
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

        public EvaluationMetrics RunV0(float radius, int count, bool usePca)
        {
            var nsa = new NegativeSelection();
            var train = usePca ? PcaTrainSet : FullTrainSet;
            var test = usePca ? PcaTestSet : FullTestSet;
            var dim = usePca ? CurrentPcaDimensions : OriginalFeatureCount - 1;

            var detectors = nsa.GenerateDetectors_v0(train, dim, count, radius);
            return new ModelEvaluator().Evaluate(detectors, test);
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