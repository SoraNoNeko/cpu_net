using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace cpu_net.Views.Pages
{
    public partial class ConfigurationPage : Page
    {
        private readonly ISettingService _settingService;
        private BrushConverter _brushConverter = new BrushConverter();
        private Brush _darkblue;
        private Brush _white;

        private MainWindow _parentWindow;
        public MainWindow ParentWindow
        {
            get => _parentWindow;
            set => _parentWindow = value;
        }

        // 通过依赖注入获取设置服务
        public ConfigurationPage(MainViewModel mVM, ISettingService settingService)
        {
            InitializeComponent();
            DataContext = mVM;
            _settingService = settingService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 使用设置服务加载配置
            var settings = _settingService.Settings;

            if (_settingService.Settings.PathExist())
            {
                code.Text = settings.Username;
                secret.Password = settings.Password;
                loginTime.Text = settings.LoginTime.ToString();
                AutoRun.IsChecked = settings.IsAutoRun;
                AutoLogin.IsChecked = settings.IsAutoLogin;
                AutoMin.IsChecked = settings.IsAutoMin;
                SetLogin.IsChecked = settings.IsSetLogin;

                // 设置运营商选择
                switch (settings.Carrier)
                {
                    case "cmcc": carrier.SelectedIndex = 1; break;
                    case "unicom": carrier.SelectedIndex = 2; break;
                    case "telecom": carrier.SelectedIndex = 3; break;
                    default: carrier.SelectedIndex = 0; break;
                }

                // 设置连接模式
                switch (settings.Mode)
                {
                    case 0: ppp.IsChecked = true; break;
                    case 1: cpu.IsChecked = true; break;
                    case 2: auto.IsChecked = true; break;
                    default: ppp.IsChecked = true; break;
                }
            }
            else
            {
                // 默认设置
                ppp.IsChecked = true;
                loginTime.Text = "6";
                carrier.SelectedIndex = 0;
                AutoRun.IsChecked = false;
                AutoLogin.IsChecked = false;
                AutoMin.IsChecked = false;
                SetLogin.IsChecked = false;
            }
        }

        private void Code_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 限制只能输入数字
            Regex re = new Regex("[^0-9]+");
            e.Handled = re.IsMatch(e.Text);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 取消时重新加载设置
            LoadSettings();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            // 打开GitHub链接
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/SoraNoNeko/cpu_net",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开链接: {ex.Message}");
            }
        }

        private void ToHome()
        {
            // 导航回首页
            Dispatcher.Invoke(() =>
            {

                ParentWindow.ChangePage("home");

            });
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrEmpty(code.Text) || string.IsNullOrEmpty(secret.Password))
            {
                MessageBox.Show("请输入学号和密码", "注意");
                return;
            }

            if (carrier.SelectedIndex == 0 && !(cpu.IsChecked == true))
            {
                MessageBox.Show("请选择运营商", "注意");
                return;
            }

            // 获取运营商信息
            string carrierValue = "";
            int keyValue = 0;
            switch (carrier.SelectedIndex)
            {
                case 0: carrierValue = ""; keyValue = 0; break;
                case 1: carrierValue = "cmcc"; keyValue = 1; break;
                case 2: carrierValue = "unicom"; keyValue = 2; break;
                case 3: carrierValue = "telecom"; keyValue = 3; break;
            }

            // 获取连接模式
            int modeValue = 0;
            if (ppp.IsChecked == true) modeValue = 0;
            else if (cpu.IsChecked == true) modeValue = 1;
            else if (auto.IsChecked == true) modeValue = 2;

            // 验证登录时间
            if (!int.TryParse(loginTime.Text, out int loginTimeValue) || loginTimeValue <= 0)
            {
                MessageBox.Show("请输入有效的登录时间间隔（大于0的整数）", "注意");
                return;
            }

            // 更新设置对象
            var settings = _settingService.Settings;
            settings.Username = code.Text;
            settings.Password = secret.Password;
            settings.Carrier = carrierValue;
            settings.Key = keyValue;
            settings.Mode = modeValue;
            settings.LoginTime = loginTimeValue;
            settings.IsAutoRun = AutoRun.IsChecked ?? false;
            settings.IsAutoLogin = AutoLogin.IsChecked ?? false;
            settings.IsAutoMin = AutoMin.IsChecked ?? false;
            settings.IsSetLogin = SetLogin.IsChecked ?? false;

            // 保存设置
            _settingService.SaveSettings();

            MessageBox.Show("保存成功", "信息");
            ToHome();
        }
    }
}