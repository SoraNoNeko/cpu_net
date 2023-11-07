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
        }
        // 页面实例的缓存
        private static readonly Dictionary<Type, Page> bufferedPages =
            new Dictionary<Type, Page>();
        // 当
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果选择项不是 ListBoxItem, 则返回
            if (PageMenu.SelectedItem is not ListBoxItem item)
                return;

            // 如果 Tag 不是一个类型, 则返回
            if (item.Tag is not Type type)
                return;

            // 如果页面缓存中找不到页面, 则创建一个新的页面并存入
            if (!bufferedPages.TryGetValue(type, out Page? page))
                page = bufferedPages[type] =
                    Activator.CreateInstance(type) as Page ?? throw new Exception("this would never happen");

            // 使用 Frame 进行导航.
            PageFrame.Navigate(page);
        }
    }
}
