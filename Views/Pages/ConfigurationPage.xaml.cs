using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// ConfigurationPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigurationPage : Page
    {
        //定义一个委托
        public delegate void Info(string infomation);
        public Info info;

        public static ConfigurationPage instance;

        public static ConfigurationPage Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ConfigurationPage();
                }
                return instance;
            }
        }

        public ConfigurationPage()
        {
            InitializeComponent();
            if (!String.IsNullOrEmpty(code.Text))
            {
                UserViewModel vm = new();
                secret.Password = vm.Password;
            }
        }

        private void Code_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9.-]+");
            e.Handled = re.IsMatch(e.Text);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            code.Text = String.Empty;
            secret.Password = String.Empty;
            carrier.SelectedIndex = 0;
        }
        public int getKey() { return carrier.SelectedIndex; }
    }
}
