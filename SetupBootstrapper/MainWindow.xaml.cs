using System.Windows;
using System.Windows.Controls;

namespace SetupBootstrapper
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            NavigateTo(new Pages.WelcomePage());
        }

        public void NavigateTo(Page page)
        {
            MainFrame.Navigate(page);
        }
    }
}
