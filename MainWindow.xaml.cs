using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel;
using cpu_net.Views.Pages;
using Microsoft.Toolkit.Uwp.Notifications;
using Prism.Navigation.Regions;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using Timer = System.Threading.Timer;

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
        private readonly ISettingService _settingService;
        private readonly IRegionManager _regionManager;
        private BrushConverter _brushConverter = new BrushConverter();
        private Brush? _darkblue;
        private Brush? _white;

        public MainViewModel _vm { get; }
        public MainWindow(MainViewModel viewModel, ISettingService settingService, IRegionManager regionManager)
        {
            InitializeComponent();
            // 注入依赖项
            // _vm = viewModel;
            // _settingService = settingService;
            _vm = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
            _regionManager = regionManager;
            DataContext = _vm;  // 设置数据上下文
            if (_settingService.Settings == null)
            {
                _settingService.Initialize(); // 如果服务有初始化方法
            }
            PreventSleep();
            InitializePages();
            InitializeTimer();
        }
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            ChangePage("home");
            // 使用局部变量避免重复访问
            if (_settingService.Settings.IsAutoLogin)
            {
                loginToast();
            }

            if (_settingService.Settings.IsAutoMin)
            {
                this.Visibility = Visibility.Hidden;
            }
        }
        private void InitializePages()
        {
            // 初始导航
            _regionManager.RequestNavigate("ContentRegion", "HomePage");
            _regionManager.RequestNavigate("ContentRegion", "ConfigurationPage");
            
        }
        private Timer? loginTimer;
        private void InitializeTimer()
        {
            // 使用设置服务中的值初始化定时器
            int loginTime = _settingService.Settings.LoginTime * 1000;
            loginTimer = new Timer(LoginCheck, null, loginTime, loginTime);
        }
        public string GetIP()
        {
            string localIP = string.Empty;
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    localIP = endPoint.Address.ToString();
                }
                _vm.Record($"重连中，当前IP为{localIP}");
            }
            catch
            {
                _vm.Record("重连中，IP获取失败，请检查网络连接");
            }
            return localIP;
        }
        private async void LoginCheck(object? ob)
        {
            // timer.Dispose(); // 销毁当前定时器
            // SettingModel settingData = new SettingModel();
            var settings = _settingService.Settings;
            bool isSetLogin = settings.IsSetLogin;
            if (!isSetLogin)
            {
                return;
            }
            string test_url = settings.TestUrl;
            string test_code = settings.TestCode;
            
            // 使用HttpClient检测网络连接状态
            bool networkAvailable = false;
            try
            {
                /*   using (var ping = new System.Net.NetworkInformation.Ping())
                   {
                       var reply = ping.Send("www.baidu.com", 1000); // Ping百度，超时1秒
                       _vm.Record("正在检测网络");
                       networkAvailable = reply?.Status == System.Net.NetworkInformation.IPStatus.Success;
                   }*/
                using (HttpClient client = new HttpClient())
                {
                    // 设置一个合理的超时时间，例如5秒
                    client.Timeout = TimeSpan.FromSeconds(5);

                    _vm.Record($"正在访问 {test_url} 进行网络检测");

                    HttpResponseMessage response = await client.GetAsync(test_url);

                    if (response.IsSuccessStatusCode) // 检查HTTP状态码是否为2xx成功
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        if (content.Trim() == test_code) // 比较文件内容，去除可能的空白字符
                        {
                            networkAvailable = true;
                        }
                        else
                        {
                            _vm.Record($"connecttest.txt 内容不匹配：'{content.Trim()}'");
                        }
                    }
                    else
                    {
                        _vm.Record($"访问 {test_url} 失败，HTTP状态码：{response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // 网络请求相关的异常（例如，DNS解析失败，连接超时等）
                _vm.Record($"网络请求异常：{ex.Message}");
                if (ex.InnerException != null)
                { 
                    _vm.Record(ex.InnerException.Message); 
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                // HttpClient超时异常
                _vm.Record($"网络连接超时：{ex.Message}");
            }
            catch (Exception ex)
            {
                // 其他未知异常
                _vm.Record($"检测过程中发生未知异常：{ex.Message}");
            }

            if (networkAvailable)
            {
                _vm.Record("网络正常");
            }
            else
            {
                _vm.Record("网络异常");
                var _IP = GetIP();
                if (String.IsNullOrEmpty(_IP))
                {
                    _vm.Record("请检查网络连接后重试");
                    return;
                }
                int _mode = 0;
                switch (settings.Mode)
                {
                    case 0:
                        _mode = settings.Mode;
                        break;
                    case 1:
                        _mode = settings.Mode;
                        break;
                    case 2:
                        string[] _ip = _IP.Split('.');
                        if (_ip[0] == "10" & _ip[1] == "12")
                        {
                            _mode = 0;
                            _vm.Record("重连中，自动识别为宽带环境");
                        }
                        else
                        {
                            _mode = 1;
                            _vm.Info("重连中，自动识别为CPU环境");
                        }
                        break;
                }
                if (_mode == 0) {
                    loginCheck(); // 仅在断网时执行登录检查
                    _vm.Info("检测到网络断开连接，已尝试重连");
                }
                
            }

        }
        private void PreventSleep()
        {
            // 阻止系统休眠和关闭显示器
            SetThreadExecutionState(
                EXECUTION_STATE.ES_CONTINUOUS |
                EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                EXECUTION_STATE.ES_DISPLAY_REQUIRED
            );
        }

        private void AllowSleep()
        {
            // 恢复系统正常休眠
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }
        public void ChangePage(string name)
        {
            _darkblue = (Brush)_brushConverter.ConvertFrom("DarkBlue");
            _white = (Brush)_brushConverter.ConvertFrom("White");

            switch (name)
            {
                case "home":
                    Home_Button.BorderBrush = _darkblue;
                    Conf_Button.BorderBrush = _white;
                    _regionManager.RequestNavigate("ContentRegion", "HomePage");
                    break;

                case "conf":
                    Home_Button.BorderBrush = _white;
                    Conf_Button.BorderBrush = _darkblue;
                    _regionManager.RequestNavigate("ContentRegion", "ConfigurationPage");
                    break;
            }
        }
        private void loginCheck()
        {
            // 使用设置服务检查配置
            if (_settingService.Settings.IsSetLogin)
            {
                Dispatcher.Invoke(() => {
                    loginToast();
                });
            }
        }

        public void loginToast()
        {
            // 通过 ViewModel 执行登录
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

            //关闭窗口
            if (result == MessageBoxResult.Yes)
                e.Cancel = false;

            //不关闭窗口
            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //鼠标左键，实现窗体最小化隐藏或显示窗体
            if (e.Button == MouseButtons.Left)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Visibility = Visibility.Hidden;
                    //解决最小化到任务栏可以强行关闭程序的问题。
                    this.ShowInTaskbar = false;//使Form不在任务栏上显示
                }
                else
                {
                    this.Visibility = Visibility.Visible;
                    //解决最小化到任务栏可以强行关闭程序的问题。
                    this.ShowInTaskbar = false;//使Form不在任务栏上显示
                    this.Activate();
                }
            }
            if (e.Button == MouseButtons.Right)
            {
                //object sender = new object();
                // EventArgs e = new EventArgs();
                exit_Click(sender, e);//触发单击退出事件
            }
        }
        // 退出选项
        private void exit_Click(object sender, EventArgs e)
        {
            if (System.Windows.MessageBox.Show("是否退出？",
                                               "询问",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question,
                                                MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                //System.Windows.Application.Current.Shutdown();
                System.Environment.Exit(0);
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
            loginTimer?.Dispose();
            AllowSleep(); // 窗口关闭时恢复休眠
            base.OnClosed(e);
        }
    }
}
