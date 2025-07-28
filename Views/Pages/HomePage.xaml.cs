using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel;
using Prism.Mvvm;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace cpu_net.Views.Pages
{
    public partial class HomePage : Page
    {
        private readonly ISettingService _settingService;
        private readonly HttpClient _httpClient;
        private const string DefaultNoticeText = @"欢迎使用本程序，
本程序旨在帮助药大学生自动登录校园网，免受断网困扰
本本答疑群：939789212
使用本程序前，需要绑定运营商账号，
绑定方法点击下方按钮可以显示
本程序所有数据均存放在本地，
如有使用困难请带着日志截图去本本群询问
注意事项：
如果需要自动登录功能，请勾选定时登录
其功能为固定间隔检测联网状态
设置中的自动登录为启动时自动登录
需要电脑不关机并连宿舍网
尝试使用CPU模式强制连接CPU时可能会卡住，是正常现象";

        private MainWindow _parentWindow;
        public MainWindow ParentWindow
        {
            get => _parentWindow;
            set => _parentWindow = value;
        }

        // 通过依赖注入获取设置服务和HttpClient
        public HomePage(MainViewModel mVM, ISettingService settingService)
        {
            InitializeComponent();
            DataContext = mVM;
            _settingService = settingService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // 设置超时时间
            };

            Loaded += HomePage_Loaded; // 页面加载时获取公告
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadNoticeText();
        }

        private async Task LoadNoticeText()
        {
            // 先显示默认公告
            textNotice.Text = DefaultNoticeText;
            sv1.ScrollToEnd();

            try
            {
                // 尝试从服务器获取最新公告
                string noticeUrl = "http://10.3.4.106/notice.txt";
                string noticeContent = await _httpClient.GetStringAsync(noticeUrl);

                if (!string.IsNullOrWhiteSpace(noticeContent))
                {
                    textNotice.Text = noticeContent;
                    sv1.ScrollToEnd();
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不中断用户体验
                // 在实际应用中，可以记录到日志系统
                Console.WriteLine($"加载公告失败: {ex.Message}");

                // 可选：在UI上显示错误信息
                // textNotice.Text = $"{DefaultNoticeText}\n\n[公告加载失败: {ex.Message}]";
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用设置服务检查配置是否存在
            if (_settingService.Settings.PathExist())
            {
                return;
            }

            var result = MessageBox.Show("请在设置中添加账号信息", "提示");
            if (result == MessageBoxResult.OK)
            {
                ToConf();
            }
        }

        private void ToConf()
        {
            // 使用Dispatcher确保在UI线程执行
            Dispatcher.Invoke(() =>
            {
                ParentWindow.ChangePage("conf");
            });
        }

        private void textLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            sv1.ScrollToEnd();
        }
    }
}