using System;
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
using cpu_net.Views.Pages;
using cpu_net.ViewModel;
using System.Diagnostics;
using Prism.Ioc;
using System.ComponentModel;
using cpu_net.Model;
using CommunityToolkit.Mvvm.Input;

namespace cpu_net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConfigurationPage ConfigurationPage = new ConfigurationPage();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            HomePage homePage = new HomePage();//实例化HomePage，初始选择页homePage
            PageFrame.Content = new Frame() { Content = homePage };//mainwindow中建立frame，用来承载所有的page,用homePage作为初始页面
            ConfigurationPage.info = GetInfo;
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
    }
}
