using AIS_networkTrafific.UI.Logic;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AIS_networkTrafific
{
    public partial class MainWindow : Window
    {
        private AisEngine _engine = new AisEngine();

        public MainWindow()
        {
            InitializeComponent();
        }

        // ==========================================
        // NOWOŚĆ: Wybór pliku z okna Windows
        // ==========================================
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Pliki Weka ARFF (*.arff)|*.arff|Wszystkie pliki (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                TxtFilePath.Text = openFileDialog.FileName;
            }
        }

        // ==========================================
        // 1. ŁADOWANIE DANYCH
        // ==========================================
        private void BtnLoadData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = TxtFilePath.Text;
                int trainSize = int.Parse(TxtTrainSize.Text);
                int testSize = int.Parse(TxtTestSize.Text);

                TxtGlobalStatus.Text = "Ładowanie i skanowanie ARFF...";
                TxtGlobalStatus.Foreground = Brushes.Orange;

                // Przekazujemy dynamiczną ścieżkę do pliku!
                _engine.LoadData(filePath, trainSize, testSize);

                TxtGlobalStatus.Text = $"Wczytano [{_engine.OriginalFeatureCount} cech] | Train: {trainSize}, Test: {testSize}";
                TxtGlobalStatus.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd: " + ex.Message, "Błąd Krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtGlobalStatus.Text = "Błąd!";
                TxtGlobalStatus.Foreground = Brushes.Red;
            }
        }
        // ==========================================
        // 2. OBSŁUGA V0 (Ślepe Losowanie)
        // ==========================================
        private async void BtnRunV0_Click(object sender, RoutedEventArgs e)
        {
            if (!_engine.IsDataLoaded) { MessageBox.Show("Najpierw wczytaj dane!"); return; }

            // 1. Czytamy z GUI parametry
            int count = int.Parse(TxtCountV0.Text);
            float radius = float.Parse(TxtRadiusV0.Text, CultureInfo.InvariantCulture);

            BtnRunV0.IsEnabled = false;
            TxtStatusBar.Text = $"Uruchamiam V0 (Detektory: {count}, Promień: {radius})...";

            // 2. Liczymy w tle!
            var results = await Task.Run(() => _engine.RunV0(radius, count));

            // 3. Wypisujemy wyniki z powrotem do GUI
            ResV0_TP.Text = results.TP.ToString();
            ResV0_FP.Text = results.FP.ToString();
            ResV0_Acc.Text = $"{results.Accuracy:F2}%";

            BtnRunV0.IsEnabled = true;
            TxtStatusBar.Text = "Gotowy.";
        }

        // ==========================================
        // 3. OBSŁUGA V1 (Profilowanie / Grawitacja)
        // ==========================================
        private async void BtnRunV1_Click(object sender, RoutedEventArgs e)
        {
            if (!_engine.IsDataLoaded) { MessageBox.Show("Najpierw wczytaj dane!"); return; }

            int count = int.Parse(TxtCountV1.Text);
            float radius = float.Parse(TxtRadiusV1.Text, CultureInfo.InvariantCulture);

            BtnRunV1.IsEnabled = false;
            TxtStatusBar.Text = $"Uruchamiam V1 (Detektory: {count}, Promień: {radius})...";

            var results = await Task.Run(() => _engine.RunV1(radius, count));

            ResV1_TP.Text = results.TP.ToString();
            ResV1_FP.Text = results.FP.ToString();
            ResV1_Acc.Text = $"{results.Accuracy:F2}%";

            BtnRunV1.IsEnabled = true;
            TxtStatusBar.Text = "Gotowy.";
        }

        // ==========================================
        // 4. OBSŁUGA V2 (Adaptive - Twój mistrz dla 41D)
        // ==========================================
        private async void BtnRunV2_Click(object sender, RoutedEventArgs e)
        {
            if (!_engine.IsDataLoaded) { MessageBox.Show("Najpierw wczytaj dane!"); return; }

            int count = int.Parse(TxtCountV2.Text);
            float minRadius = float.Parse(TxtRadiusMinV2.Text, CultureInfo.InvariantCulture);

            BtnRunV2.IsEnabled = false;
            TxtStatusBar.Text = $"Uruchamiam V2 Adaptive (Detektory: {count}, Min Radius: {minRadius})...";

            var results = await Task.Run(() => _engine.RunV2(count, minRadius, usePca: false));

            ResV2_TP.Text = results.TP.ToString();
            ResV2_FP.Text = results.FP.ToString();
            ResV2_Acc.Text = $"{results.Accuracy:F2}%";

            // To na dole można wyciągnąć, jeśli Twój ModelEvaluator zacząłby zwracać np. MaxRadius
            ResV2_MaxRad.Text = "Automatyczny";

            BtnRunV2.IsEnabled = true;
            TxtStatusBar.Text = "Gotowy.";
        }

        // ==========================================
        // 5. ZASTOSOWANIE PCA
        // ==========================================
        private async void BtnApplyPca_Click(object sender, RoutedEventArgs e)
        {
            if (!_engine.IsDataLoaded) { MessageBox.Show("Najpierw wczytaj dane!"); return; }

            try
            {
                int dimensions = int.Parse(TxtPcaDim.Text);

                BtnApplyPca.IsEnabled = false;
                TxtPcaStatus.Text = "Przetwarzanie PCA...";
                TxtPcaStatus.Foreground = Brushes.Orange;

                // PCA może chwilę potrwać dla dużej ilości danych, więc też wrzucamy w Task
                await Task.Run(() => _engine.RunPcaTransformation(dimensions));

                TxtPcaStatus.Text = $"Gotowe ({dimensions}D)";
                TxtPcaStatus.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas PCA: " + ex.Message);
                TxtPcaStatus.Text = "Błąd";
                TxtPcaStatus.Foreground = Brushes.Red;
            }
            finally
            {
                BtnApplyPca.IsEnabled = true;
            }
        }

        // ==========================================
        // 6. OBSŁUGA V2 (Gravity) PO PCA
        // ==========================================
        private async void BtnRunV2Pca_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.PcaTrainSet == null || _engine.PcaTrainSet.Count == 0) { MessageBox.Show("Najpierw zastosuj PCA!"); return; }

            int count = int.Parse(TxtCountV2Pca.Text);
            float minRadius = float.Parse(TxtRadiusMinV2Pca.Text, CultureInfo.InvariantCulture);

            BtnRunV2Pca.IsEnabled = false;
            TxtStatusBar.Text = $"Uruchamiam V2 w przestrzeni PCA ({count} detektorów)...";

            // Zwróć uwagę na argument usePca: true
            var results = await Task.Run(() => _engine.RunV2(count, minRadius, usePca: true));

            ResV2Pca_TP.Text = results.TP.ToString();
            ResV2Pca_FP.Text = results.FP.ToString();
            ResV2Pca_Acc.Text = $"{results.Accuracy:F2}%";

            BtnRunV2Pca.IsEnabled = true;
            TxtStatusBar.Text = "Gotowy.";
        }

        // ==========================================
        // 7. OBSŁUGA V3 (Uniform) PO PCA - ZWYCIĘZCA
        // ==========================================
        private async void BtnRunV3Pca_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.PcaTrainSet == null || _engine.PcaTrainSet.Count == 0) { MessageBox.Show("Najpierw zastosuj PCA!"); return; }

            int count = int.Parse(TxtCountV3Pca.Text);
            float minRadius = float.Parse(TxtRadiusMinV3Pca.Text, CultureInfo.InvariantCulture);

            BtnRunV3Pca.IsEnabled = false;
            TxtStatusBar.Text = $"Uruchamiam V3 w przestrzeni PCA ({count} detektorów)...";

            var results = await Task.Run(() => _engine.RunV3Pca(count, minRadius));

            ResV3Pca_TP.Text = results.TP.ToString();
            ResV3Pca_FP.Text = results.FP.ToString();
            ResV3Pca_Acc.Text = $"{results.Accuracy:F2}%";

            BtnRunV3Pca.IsEnabled = true;
            TxtStatusBar.Text = "Gotowy.";
        }
    }
}