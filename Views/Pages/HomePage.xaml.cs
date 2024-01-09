using cpu_net.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class HomePage : Page
    {
        SettingModel settingData = new SettingModel();
        readonly String noticeText = @"欢迎使用本程序，
本程序旨在帮助药大学生自动登录校园网，免受断网困扰
23级本本答疑群：819668931
使用本程序前，需要绑定运营商账号，
绑定方法点击下方按钮可以显示
本程序所有数据均存放在本地，
如有使用困难请带着右边的日志截图去本本群询问
注意事项：
如果需要自动登录功能，请勾选定时登录
其功能为每6小时刷新登录状态
设置中的自动登录为启动时自动登录
需要电脑不关机并连宿舍网
尝试使用CPU模式强制连接CPU时可能会卡住，是正常现象";

        MainWindow parentWindow;
        public MainWindow ParentWindow
        {
            get { return parentWindow; }
            set { parentWindow = value; }
        }

        public HomePage()
        {
            InitializeComponent();
            textNotice.Text = noticeText;
            sv1.ScrollToEnd();
        }

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (settingData.PathExist())
            {
                return;
            }
            else
            {
                var result = MessageBox.Show("请在设置中添加账号信息", "提示");

                if (result == MessageBoxResult.OK)
                {
                    ToConf();
                }

            }
        }
        private void ToConf()
        {
            //Debug.WriteLine("count");
            //MainViewModel mainViewModel = new MainViewModel();
            //mainViewModel.LoginOnline();

            Action invokeAction = new Action(ToConf);
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, invokeAction);
            }
            else
            {
                this.parentWindow.Conf_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                //this.NavigationService.Source = new Uri("/Views/Pages/ConfigurationPage.xaml", UriKind.Relative);
            }
        }

        private void textLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            sv1.ScrollToEnd();
        }
    }
}
