using cpu_net.Model;
using cpu_net.ViewModel;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// ConfigurationPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigurationPage : Page
    {
        public static ConfigurationPage instance;

        SettingModel settingData = new SettingModel();

        public static ConfigurationPage Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ConfigurationPage();
                }
                return instance;
            }
        }

        public ConfigurationPage()
        {
            InitializeComponent();
            if (!String.IsNullOrEmpty(code.Text))
            {
                UserViewModel vm = new();
                secret.Password = vm.Password;
            }
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                switch (settingData.Mode)
                {
                    case 0:
                        ppp.IsChecked = true; break;
                    case 1:
                        cpu.IsChecked = true; break;
                    case 2:
                        auto.IsChecked = true; break;
                    default:
                        ppp.IsChecked = true; break;
                }
            }
            else
            {
                ppp.IsChecked = true;
            }
        }

        private void Code_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9.-]+");
            e.Handled = re.IsMatch(e.Text);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            code.Text = String.Empty;
            secret.Password = String.Empty;
            carrier.SelectedIndex = 0;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            System.Diagnostics.Process.Start("explorer.exe", link.NavigateUri.AbsoluteUri);
        }
    }
}
