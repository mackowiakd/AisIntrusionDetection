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
                _engine.LoadData(filePath, ref trainSize, testSize);

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
        // 2. OBSŁUGA METODY V0 (BASIC STATIC PCA)
        // ==========================================
        private async void BtnRunV0Pca_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.PcaTrainSet == null || _engine.PcaTrainSet.Count == 0)
            {
                MessageBox.Show("Wykonaj najpierw procedurę dopasowania i transformacji PCA!");
                return;
            }

            int count = int.Parse(TxtCountV0Pca.Text);
            float radius = float.Parse(TxtRadiusV0Pca.Text, CultureInfo.InvariantCulture);

            BtnRunV0Pca.IsEnabled = false;
            TxtStatusBar.Text = "Obliczanie tarczy detektorów V0 Basic w przestrzeni PCA...";

            // Wywołanie asynchroniczne silnika z flagą aktywującą podstawowy algorytm losowy
            var results = await Task.Run(() => _engine.RunV0(radius, count, usePca:true));

            ResV0Pca_TP.Text = results.TP.ToString();
            ResV0Pca_FP.Text = results.FP.ToString();
            ResV0Pca_Acc.Text = $"{results.Accuracy:F2}%";

            BtnRunV0Pca.IsEnabled = true;
            TxtStatusBar.Text = "Ewaluacja V0 ukończona.";
        }

        // ==========================================
        // 3. OBSŁUGA METODY V2 (GRAVITY STATIC PCA)
        // ==========================================
        private async void BtnRunV2Pca_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.PcaTrainSet == null || _engine.PcaTrainSet.Count == 0)
            {
                MessageBox.Show("Wykonaj najpierw procedurę dopasowania i transformacji PCA!");
                return;
            }

            int count = int.Parse(TxtCountV2Pca.Text);
            float minRadius = float.Parse(TxtRadiusMinV2Pca.Text, CultureInfo.InvariantCulture);

            BtnRunV2Pca.IsEnabled = false;
            TxtStatusBar.Text = "Uruchamianie heurystyki przyciągania grawitacyjnego V2...";

            var results = await Task.Run(() => _engine.RunV2(count, minRadius, usePca: true));

            ResV2Pca_TP.Text = results.TP.ToString();
            ResV2Pca_FP.Text = results.FP.ToString();
            ResV2Pca_Acc.Text = $"{results.Accuracy:F2}%";

            BtnRunV2Pca.IsEnabled = true;
            TxtStatusBar.Text = "Ewaluacja V2 ukończona.";
        }

        // ==========================================
        // 4. OBSŁUGA METODY V3 (UNIFORM ADAPTIVE PCA)
        // ==========================================
        private async void BtnRunV3Pca_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.PcaTrainSet == null || _engine.PcaTrainSet.Count == 0)
            {
                MessageBox.Show("Wykonaj najpierw procedurę dopasowania i transformacji PCA!");
                return;
            }

            int count = int.Parse(TxtCountV3Pca.Text);
            float minRadius = float.Parse(TxtRadiusMinV3Pca.Text, CultureInfo.InvariantCulture);

            BtnRunV3Pca.IsEnabled = false;
            TxtStatusBar.Text = "Generowanie optymalnej powłoki detektorów adaptacyjnych V3...";

            var results = await Task.Run(() => _engine.RunV3Pca(count, minRadius));

            ResV3Pca_TP.Text = results.TP.ToString();
            ResV3Pca_FP.Text = results.FP.ToString();
            ResV3Pca_Acc.Text = $"{results.Accuracy:F2}%";

            BtnRunV3Pca.IsEnabled = true;
            TxtStatusBar.Text = "Ewaluacja V3 ukończona. Wynik gotowy do prezentacji.";
        }
    }
}