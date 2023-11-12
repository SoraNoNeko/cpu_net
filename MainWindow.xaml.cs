using cpu_net.Model;
using cpu_net.ViewModel;
using cpu_net.Views.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Timer = System.Threading.Timer;

namespace cpu_net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SettingModel settingData = new SettingModel();
        ConfigurationPage ConfigurationPage = new ConfigurationPage();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            HomePage homePage = new HomePage();//实例化HomePage，初始选择页homePage
            PageFrame.Content = new Frame() { Content = homePage };//mainwindow中建立frame，用来承载所有的page,用homePage作为初始页面
            Brush darkblue;
            Brush white;
            BrushConverter brushConverter = new BrushConverter();
            darkblue = (Brush)brushConverter.ConvertFrom("DarkBlue");
            white = (Brush)brushConverter.ConvertFrom("White");
            Home_Button.BorderBrush = darkblue;
            Conf_Button.BorderBrush = white;
            TimerMain();
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                if (settingData.IsAutoLogin) 
                {
                    MainViewModel mainViewModel = new MainViewModel();
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
            timer = new Timer(LoginCheck, "", 21600000, 21600000);
            //timer.Dispose();
        }

        private void LoginCheck(object? ob)
        {
            timer.Dispose();
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                if (settingData.IsAutoLogin)
                {
                    //Debug.WriteLine("count");
                    MainViewModel mainViewModel = new MainViewModel();
                    mainViewModel.LoginOnline();
                }
            }
            TimerMain();
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

        private void Home_Selected(object sender, RoutedEventArgs e)
        {
            Brush darkblue;
            Brush white;
            BrushConverter brushConverter = new BrushConverter();
            darkblue = (Brush)brushConverter.ConvertFrom("DarkBlue");
            white = (Brush)brushConverter.ConvertFrom("White");
            Home_Button.BorderBrush = darkblue;
            Conf_Button.BorderBrush = white;
            PageFrame.Content = new Frame() { Content = HomePage.Instance };
            ///PageFrame.Navigate(new HomePage());
        }

        private void Configuration_Selected(object sender, RoutedEventArgs e)
        {
            Brush darkblue;
            Brush white;
            BrushConverter brushConverter = new BrushConverter();
            darkblue = (Brush)brushConverter.ConvertFrom("DarkBlue");
            white = (Brush)brushConverter.ConvertFrom("White");
            Home_Button.BorderBrush = white;
            Conf_Button.BorderBrush = darkblue;
            PageFrame.Content = new Frame() { Content = ConfigurationPage.Instance };
            ///PageFrame.Navigate(new ConfigurationPage());
        }
        private void GetInfo(string infomation)
        {
            MainViewModel mainViewModel = new MainViewModel();
            mainViewModel.Info(infomation);
        }
        private void initialTray()
        {
            this.Visibility = Visibility.Hidden;
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
