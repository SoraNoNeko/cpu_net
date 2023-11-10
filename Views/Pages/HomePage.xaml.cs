using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using cpu_net.ViewModel;
using System.Diagnostics;
using NLog.Config;
using NLog.Targets.Wrappers;
using NLog;

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        readonly String noticeText = @"TestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTest
TestTestTestTest
TestTestTest";
        public HomePage()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            textNotice.Text = noticeText;
        }

        public void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Info("success1");
        }

        public void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Info("success2");
        }
        public void Info(string message)
        {
            textLog.Text = textLog.Text + message + Environment.NewLine;
        }
    }
}
