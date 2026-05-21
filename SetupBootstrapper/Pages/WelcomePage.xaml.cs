using System.Windows;
using System.Windows.Controls;

namespace SetupBootstrapper.Pages
{
    public partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.NavigateTo(new InstallLocationPage());
        }
    }
}
