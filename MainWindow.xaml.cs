using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel;
using cpu_net.Views.Pages;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cpu_net
{
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [Flags]
        enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        private static readonly Brush DarkBlueBrush = new SolidColorBrush(Colors.DarkBlue);
        private static readonly Brush WhiteBrush = new SolidColorBrush(Colors.White);

        private HomePage _cachedHomePage = new HomePage();
        private ConfigurationPage _cachedConfigurationPage = new ConfigurationPage();
        private ElectricityPage _cachedElectricityPage = new ElectricityPage();
        private readonly MainViewModel _vm = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            PreventSleep();
            _cachedHomePage.ParentWindow = this;
            _cachedConfigurationPage.ParentWindow = this;
            _cachedElectricityPage.ParentWindow = this;
            ChangePage("home");
            DataContext = _vm;

            var settingData = new SettingModel();
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                if (settingData.NetworkLoginEnabled && settingData.IsAutoLogin)
                {
                    loginToast();
                }
                if (settingData.IsAutoMin)
                {
                    this.Visibility = Visibility.Hidden;
                }
                ApplyBackgroundAndIcon();
            }
        }

        private void PreventSleep()
        {
            SetThreadExecutionState(
                EXECUTION_STATE.ES_CONTINUOUS |
                EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                EXECUTION_STATE.ES_DISPLAY_REQUIRED
            );
        }

        private void AllowSleep()
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        private void ChangePage(string name)
        {
            Home_Button.BorderBrush = WhiteBrush;
            Elec_Button.BorderBrush = WhiteBrush;
            Conf_Button.BorderBrush = WhiteBrush;

            switch (name)
            {
                case "home":
                    Home_Button.BorderBrush = DarkBlueBrush;
                    PageFrame.Content = _cachedHomePage;
                    break;
                case "electricity":
                    Elec_Button.BorderBrush = DarkBlueBrush;
                    PageFrame.Content = _cachedElectricityPage;
                    break;
                case "conf":
                    Conf_Button.BorderBrush = DarkBlueBrush;
                    PageFrame.Content = _cachedConfigurationPage;
                    break;
            }
        }

        public void NavigateToSettings(string section)
        {
            ChangePage("conf");
            _cachedConfigurationPage.MenuListBox.SelectedIndex = section switch
            {
                "electricity" => 1,
                "email" => 2,
                "background" => 3,
                _ => 0
            };
        }

        public void ApplyBackgroundAndIcon()
        {
            var setting = new SettingModel();
            if (!setting.PathExist()) return;
            setting = setting.Read();

            // 应用背景
            if (!string.IsNullOrWhiteSpace(setting.BackgroundImagePath))
            {
                var brush = ImageProcessingService.CreateBackgroundBrush(setting.BackgroundImagePath, setting.BackgroundOpacity);
                if (brush != null)
                {
                    MainGrid.Background = brush;
                }
            }
            else
            {
                MainGrid.Background = new SolidColorBrush(Colors.White);
            }

            // 应用图标
            if (!string.IsNullOrWhiteSpace(setting.CustomIconPath) && System.IO.File.Exists(setting.CustomIconPath))
            {
                try
                {
                    var iconUri = new Uri(setting.CustomIconPath, UriKind.Absolute);
                    this.Icon = new System.Windows.Media.Imaging.BitmapImage(iconUri);
                }
                catch { }
            }
        }

        public void loginToast()
        {
            int result = _vm.LoginOnline();
            if (result == 0 && this.Visibility == Visibility.Collapsed)
            {
                // Toast notification removed for .NET 8 compatibility
                // Using simple log instead
            }
            ChangePage("home");
        }

        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("是否退出？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question);
            e.Cancel = result != MessageBoxResult.Yes;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        private void Home_Button_Click(object sender, RoutedEventArgs e)
        {
            ChangePage("home");
        }

        private void Elec_Button_Click(object sender, RoutedEventArgs e)
        {
            ChangePage("electricity");
        }

        private void Conf_Button_Click(object sender, RoutedEventArgs e)
        {
            ChangePage("conf");
        }

        protected override void OnClosed(EventArgs e)
        {
            AllowSleep();
            base.OnClosed(e);
        }
    }
}
