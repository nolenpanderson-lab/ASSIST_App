
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

namespace ASSIST_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(contentTextBox.Text))
            {
                MessageBox.Show("Please enter content to analyze.");
                return;
            }

            string content = contentTextBox.Text.Trim();

            Analysis analysisWindow = new Analysis(content);

            analysisWindow.Show();

            this.Close();
        }

        private void contentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
