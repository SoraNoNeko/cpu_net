using cpu_net.Model;
using cpu_net.ViewModel;
using cpu_net.Views.Pages;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Security.Permissions;
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
        BrushConverter brushConverter = new BrushConverter();
        Brush darkblue;
        Brush white;
        HomePage homePage = new HomePage();
        ConfigurationPage configurationPage = new ConfigurationPage();
        public MainWindow()
        {
            InitializeComponent();
            homePage.ParentWindow = this;
            configurationPage.ParentWindow = this;
            ChangePage("home");
            //Debug.WriteLine("action1");
            MainViewModel mainViewModel = new MainViewModel();
            TimerMain(mainViewModel);
            //Debug.WriteLine("action2");
            SettingModel settingData = new SettingModel();
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                if (settingData.IsAutoLogin)
                {
                    loginToast(mainViewModel);
                }
                if (settingData.IsAutoMin)
                {
                    this.Visibility = Visibility.Hidden;
                }
            }
        }


        private Timer timer;

        public void TimerMain(MainViewModel mainViewModel)
        {
            //Debug.WriteLine("action3");
            timer = new Timer(LoginCheck, "", 21600000, 21600000);
            //timer = new Timer(LoginCheck, mainViewModel, 3000, 21600000);
            //timer.Dispose();
        }
        private void LoginCheck(object? ob)
        {
            MainViewModel mainViewModel = (MainViewModel)ob;
            timer.Dispose();
            loginCheck(mainViewModel);
            //Debug.WriteLine("tick1");
            //Test();
            TimerMain(mainViewModel);
        }
        /*
        private void Test()
        {
            Action invokeAction = new Action(Test);
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, invokeAction);
            }
            else
            {
                PageFrame.Refresh();
                Debug.WriteLine("tick2");
                Debug.WriteLine(PageFrame.Source.ToString());
            }
        }
        */
        private void ChangePage(string name)
        {
            darkblue = (Brush)brushConverter.ConvertFrom("DarkBlue");
            white = (Brush)brushConverter.ConvertFrom("White");
            switch (name)
            {
                case "home":
                    Home_Button.BorderBrush = darkblue;
                    Conf_Button.BorderBrush = white;
                    var home = new HomePage();
                    home.ParentWindow = this;
                    PageFrame.Content = home;
                    break;
                case "conf":
                    Home_Button.BorderBrush = white;
                    Conf_Button.BorderBrush = darkblue;
                    var conf = new ConfigurationPage();
                    conf.ParentWindow = this;
                    PageFrame.Content = conf;
                    break;
            }
        }
        private void loginCheck(MainViewModel mainViewModel)
        {
            SettingModel settingData = new SettingModel();
            //Debug.WriteLine("action4");
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                if (settingData.IsSetLogin)
                {
                    //Debug.WriteLine("count");
                    //MainViewModel mainViewModel = new MainViewModel();
                    //mainViewModel.LoginOnline();

                    Action<MainViewModel> invokeAction = new Action<MainViewModel>(loginCheck);
                    if (!this.Dispatcher.CheckAccess())
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Send, invokeAction, mainViewModel);
                    }
                    else
                    {
                        loginToast(mainViewModel);
                        //homePage.LoginButton.Command.Execute(null);
                    }

                }
            }
        }
        public void loginToast(MainViewModel mainViewModel)
        {
            int a = mainViewModel.LoginOnline();
            //Debug.WriteLine("a="+a);
            //Debug.WriteLine(this.Visibility);
            if (a == 0 & this.Visibility == Visibility.Collapsed)
            {
                Debug.WriteLine("toasttest");
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
    }
}
