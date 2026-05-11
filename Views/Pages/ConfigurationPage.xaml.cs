using cpu_net.Model;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// ConfigurationPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigurationPage : Page
    {
        private SettingModel _settingData = new SettingModel();

        public MainWindow ParentWindow { get; set; }

        public ConfigurationPage()
        {
            InitializeComponent();
            LoadSettingsToUi(new SettingModel(), isReset: true);
        }

        private void Code_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9.-]+");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var settingData = new SettingModel();
            LoadSettingsToUi(settingData, isReset: true);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/SoraNoNeko/cpu_net");
        }

        private void ToHome()
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(ToHome));
            }
            else
            {
                this.ParentWindow.Home_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(code.Text) || string.IsNullOrEmpty(secret.Password))
            {
                MessageBox.Show("请输入学号和密码", "Attention");
                return;
            }

            if (carrier.SelectedIndex == 0 && cpu.IsChecked != true)
            {
                MessageBox.Show("请选择运营商", "Attention");
                return;
            }

            (string carrierValue, int key) = ResolveCarrier();

            _settingData.IsAutoRun = AutoRun.IsChecked ?? false;
            _settingData.IsAutoLogin = AutoLogin.IsChecked ?? false;
            _settingData.IsAutoMin = AutoMin.IsChecked ?? false;
            _settingData.IsSetLogin = SetLogin.IsChecked ?? false;
            _settingData.Mode = ResolveMode();
            _settingData.Username = code.Text;
            _settingData.Password = secret.Password;
            _settingData.Carrier = carrierValue;
            _settingData.Key = key;
            _settingData.LoginTime = int.Parse(loginTime.Text);
            _settingData.Save();

            var result = MessageBox.Show("保存成功", "Info");
            if (result == MessageBoxResult.OK)
            {
                ToHome();
            }
        }

        #region 辅助方法

        /// <summary>
        /// 将配置加载到 UI 控件
        /// </summary>
        private void LoadSettingsToUi(SettingModel data, bool isReset)
        {
            bool hasConfig = isReset && data.PathExist();
            if (hasConfig)
            {
                data = data.Read();
            }

            code.Text = hasConfig ? data.Username : string.Empty;
            secret.Password = hasConfig ? data.Password : string.Empty;
            loginTime.Text = hasConfig ? data.LoginTime.ToString() : "6";
            AutoRun.IsChecked = hasConfig && data.IsAutoRun;
            AutoLogin.IsChecked = hasConfig && data.IsAutoLogin;
            AutoMin.IsChecked = hasConfig && data.IsAutoMin;
            SetLogin.IsChecked = hasConfig && data.IsSetLogin;

            carrier.SelectedIndex = hasConfig ? data.Carrier switch
            {
                "cmcc" => 1,
                "unicom" => 2,
                "telecom" => 3,
                _ => 0
            } : 0;

            ApplyModeRadio(hasConfig ? data.Mode : 0);
        }

        private void ApplyModeRadio(int mode)
        {
            ppp.IsChecked = mode == 0;
            cpu.IsChecked = mode == 1;
            auto.IsChecked = mode == 2;
        }

        private int ResolveMode()
        {
            return (ppp.IsChecked, cpu.IsChecked, auto.IsChecked) switch
            {
                (true, _, _) => 0,
                (_, true, _) => 1,
                (_, _, true) => 2,
                _ => 0
            };
        }

        private (string Carrier, int Key) ResolveCarrier()
        {
            return carrier.SelectedIndex switch
            {
                1 => ("cmcc", 1),
                2 => ("unicom", 2),
                3 => ("telecom", 3),
                _ => (string.Empty, 0)
            };
        }

        #endregion
    }
}
