using AIS_networkTrafific.UI.Logic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AIS_networkTrafific
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AisEngine _engine = new AisEngine();

        public MainWindow()
        {
            InitializeComponent();
        }

        // Obsługa przycisku ładowania danych
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            _engine.LoadData("KDDTrain+_20Percent.arff");
           // StatusText.Text = "Dane wczytane pomyślnie!";
        }


        // ZMIANA : liczba detektorow  i minradius powinny byc parametrami, a nie na sztywno wpisane! to samo do  v0 i v1 (tez maja liczbe detektorow )
        // jednie mozna ustawic te liczby jako domyslne przy wpiywaniu w GUI!

       
        // // Obsługa przycisku V2 w zakładce 41D 
        private async void BtnRunV2_Click(object sender, RoutedEventArgs e)
        {
            if (!_engine.IsDataLoaded) return;

            // Blokujemy UI na czas obliczeń
            //BtnRunV2.IsEnabled = false;

            //// Wykonujemy ciężkie obliczenia w tle
            //var results = await Task.Run(() => _engine.RunV2(5000, 0.05f, usePca: false));

            //// Wyświetlamy wyniki
            //V2_Accuracy.Text = $"{results.Accuracy:F2}%";
            //V2_TP.Text = results.TP.ToString();
            //V2_FP.Text = results.FP.ToString();

            //BtnRunV2.IsEnabled = true;
        }

        /* finalnie dodac opcje step do parametrow kluczowych wykonac wykresy?? */
    }
}