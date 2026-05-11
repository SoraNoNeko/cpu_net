using cpu_net.Model;
using cpu_net.ViewModel;
using cpu_net.Views.Pages;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace cpu_net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
        private readonly MainViewModel _vm = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            PreventSleep();
            _cachedHomePage.ParentWindow = this;
            _cachedConfigurationPage.ParentWindow = this;
            ChangePage("home");
            DataContext = _vm;

            var settingData = new SettingModel();
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                if (settingData.IsAutoLogin)
                {
                    loginToast();
                }
                if (settingData.IsAutoMin)
                {
                    this.Visibility = Visibility.Hidden;
                }
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
            switch (name)
            {
                case "home":
                    Home_Button.BorderBrush = DarkBlueBrush;
                    Conf_Button.BorderBrush = WhiteBrush;
                    PageFrame.Content = _cachedHomePage;
                    break;
                case "conf":
                    Home_Button.BorderBrush = WhiteBrush;
                    Conf_Button.BorderBrush = DarkBlueBrush;
                    PageFrame.Content = _cachedConfigurationPage;
                    break;
            }
        }

        public void loginToast()
        {
            int result = _vm.LoginOnline();
            if (result == 0 && this.Visibility == Visibility.Collapsed)
            {
                new ToastContentBuilder()
                    .AddText("登录失败")
                    .AddText("请检查网络设置")
                    .Show();
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
