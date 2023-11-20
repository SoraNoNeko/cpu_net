using cpu_net.Model;
using cpu_net.ViewModel;
using cpu_net.Views.Pages;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Timer = System.Threading.Timer;

namespace cpu_net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Debug.WriteLine("action1");
            TimerMain();
            //Debug.WriteLine("action2");
            SettingModel settingData = new SettingModel();
            MainViewModel mainViewModel = new MainViewModel();
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                if (settingData.IsAutoLogin)
                {
                    mainViewModel.LoginOnline();
                }
                if (settingData.IsAutoMin)
                {
                    this.Visibility = Visibility.Hidden;
                }
            }

        }


        private Timer timer;

        public void TimerMain()
        {
            //Debug.WriteLine("action3");
            timer = new Timer(LoginCheck, "", 21600000, 21600000);
            //timer = new Timer(LoginCheck, "", 3000, 21600000);
            //timer.Dispose();
        }
        private void LoginCheck(object? ob)
        {
            timer.Dispose();
            loginCheck();
            //Debug.WriteLine("tick1");
            //Test();
            TimerMain();
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
        private void loginCheck()
        {
            HomePage homePage = new HomePage();
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

                    Action invokeAction = new Action(loginCheck);
                    if (!this.Dispatcher.CheckAccess())
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.Send, invokeAction);
                    }
                    else
                    {
                        homePage.LoginButton.Command.Execute(null);
                    }

                }
            }
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
    }
}
