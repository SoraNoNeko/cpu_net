using cpu_net.Model;
using cpu_net.ViewModel;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// ConfigurationPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigurationPage : Page
    {
        BrushConverter brushConverter = new BrushConverter();
        Brush darkblue;
        Brush white;
        SettingModel settingData = new SettingModel();

        MainWindow parentWindow;
        public MainWindow ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }

        public ConfigurationPage()
        {
            InitializeComponent();
            
            if (!String.IsNullOrEmpty(code.Text))
            {
                secret.Password = settingData.Password;
            }
            
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                code.Text = settingData.Username;
                secret.Password = settingData.Password;
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
        private void ToHome()
        {
            //Debug.WriteLine("count");
            //MainViewModel mainViewModel = new MainViewModel();
            //mainViewModel.LoginOnline();

            Action invokeAction = new Action(ToHome);
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, invokeAction);
            }
            else
            {
                this.parentWindow.Home_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                //this.NavigationService.Source = new Uri("/Views/Pages/HomePage.xaml", UriKind.Relative);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int mode = 0;
            int Key = 0;
            string Carrier = "";
            switch (carrier.SelectedIndex)
            {
                case 0:
                    Key = 0;
                    Carrier = "";
                    break;
                case 1:
                    Key = 1;
                    Carrier = "cmcc";
                    break;
                case 2:
                    Key = 2;
                    Carrier = "unicom";
                    break;
                case 3:
                    Key = 3;
                    Carrier = "telecom";
                    break;
            }
            if (string.IsNullOrEmpty(code.Text) | string.IsNullOrEmpty(secret.Password))
            {
                MessageBox.Show("请输入学号和密码", "Attention");
            }
            else if (carrier.SelectedIndex == 0 & cpu.IsChecked == false)
            {
                MessageBox.Show("请选择运营商", "Attention");
            }
            else
            {
                if (!AutoRun.IsChecked.HasValue)
                {
                    AutoRun.IsChecked = false;
                }
                settingData.IsAutoRun = (bool)AutoRun.IsChecked;
                if (!AutoLogin.IsChecked.HasValue)
                {
                    AutoLogin.IsChecked = false;
                }
                settingData.IsAutoLogin = (bool)AutoLogin.IsChecked;
                if (!AutoMin.IsChecked.HasValue)
                {
                    AutoMin.IsChecked = false;
                }
                settingData.IsAutoMin = (bool)AutoMin.IsChecked;
                if (!SetLogin.IsChecked.HasValue)
                {
                    SetLogin.IsChecked = false;
                }
                settingData.IsSetLogin = (bool)SetLogin.IsChecked;
                switch (ppp.IsChecked, cpu.IsChecked, auto.IsChecked)
                {
                    case (true, false, false):
                        mode = 0;
                        break;
                    case (false, true, false):
                        mode = 1;
                        break;
                    case (false, false, true):
                        mode = 2;
                        break;
                }
                settingData.Mode = mode;
                settingData.Username = code.Text;
                settingData.Password = secret.Password;
                settingData.Carrier = Carrier;
                settingData.Key = Key;
                settingData.Save();
                var result = MessageBox.Show("保存成功", "Info");
                if (result == MessageBoxResult.OK)
                {
                    ToHome();
                }
            }
        }
    }
}
