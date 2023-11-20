using cpu_net.Model;
using cpu_net.ViewModel;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// ConfigurationPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigurationPage : Page
    {

        SettingModel settingData = new SettingModel();

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
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                code.Text = settingData.Username;
                secret.Password = settingData.Password;
                AutoRun.IsChecked = settingData.IsAutoRun;
                AutoLogin.IsChecked = settingData.IsAutoLogin;
                AutoMin.IsChecked = settingData.IsAutoMin;
                SetLogin.IsChecked = settingData.IsSetLogin;
                switch (settingData.Carrier)
                {
                    case "cmcc":
                        carrier.SelectedIndex = 1;
                        break;
                    case "unicom":
                        carrier.SelectedIndex = 2;
                        break;
                    case "telecom":
                        carrier.SelectedIndex = 3;
                        break;
                    case "":
                        carrier.SelectedIndex = 0;
                        break;
                }
                switch (settingData.Mode)
                {
                    case 0:
                        ppp.IsChecked = true;
                        cpu.IsChecked = false;
                        auto.IsChecked = false;
                        break;
                    case 1:
                        ppp.IsChecked = false;
                        cpu.IsChecked = true;
                        auto.IsChecked = false;
                        break;
                    case 2:
                        ppp.IsChecked = false;
                        cpu.IsChecked = false;
                        auto.IsChecked = true;
                        break;
                    default:
                        ppp.IsChecked = true;
                        cpu.IsChecked = false;
                        auto.IsChecked = false;
                        break;
                }
            }
            else
            {
                code.Text = String.Empty;
                secret.Password = String.Empty;
                carrier.SelectedIndex = 0;
                AutoRun.IsChecked = false;
                AutoLogin.IsChecked = false;
                AutoMin.IsChecked = false;
                SetLogin.IsChecked = false;
                ppp.IsChecked = true;
                cpu.IsChecked = false;
                auto.IsChecked = false;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "https://github.com/SoraNoNeko/cpu_net");
        }
    }
}
