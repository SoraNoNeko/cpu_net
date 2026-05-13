using cpu_net.Model;
using cpu_net.ViewModel;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace cpu_net.Views.Pages
{
    public partial class ConfigurationPage : Page
    {
        private SettingModel _settingData = new SettingModel();
        private bool _isScrollingFromMenu = false;

        public MainWindow ParentWindow { get; set; }

        public ConfigurationPage()
        {
            InitializeComponent();
            LoadSettingsToUi(new SettingModel(), isReset: true);

            MenuListBox.SelectionChanged += MenuListBox_SelectionChanged;
            MenuListBox.PreviewMouseLeftButtonDown += MenuListBox_PreviewMouseLeftButtonDown;
            ContentScrollViewer.ScrollChanged += ContentScrollViewer_ScrollChanged;
        }

        private void Code_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9.-]+");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var settingData = new SettingModel();
            LoadSettingsToUi(settingData, isReset: true);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/SoraNoNeko/cpu_net");
        }

        private void ToHome()
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(ToHome));
            }
            else
            {
                this.ParentWindow.Home_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存网络设置
            if (NetworkEnabledCheckBox.IsChecked == true)
            {
                if (string.IsNullOrEmpty(code.Text) || string.IsNullOrEmpty(secret.Password))
                {
                    ScrollToSection(NetworkPanel);
                    MenuListBox.SelectedIndex = 0;
                    MessageBox.Show("请输入学号和密码", "Attention");
                    return;
                }

                if (carrier.SelectedIndex == 0 && cpu.IsChecked != true)
                {
                    ScrollToSection(NetworkPanel);
                    MenuListBox.SelectedIndex = 0;
                    MessageBox.Show("请选择运营商", "Attention");
                    return;
                }
            }

            (string carrierValue, int key) = ResolveCarrier();

            _settingData.NetworkLoginEnabled = NetworkEnabledCheckBox.IsChecked ?? true;
            _settingData.IsAutoRun = AutoRun.IsChecked ?? false;
            _settingData.IsAutoLogin = AutoLogin.IsChecked ?? false;
            _settingData.IsAutoMin = AutoMin.IsChecked ?? false;
            _settingData.IsSetLogin = SetLogin.IsChecked ?? false;
            _settingData.Mode = ResolveMode();
            _settingData.Username = code.Text;
            _settingData.Password = secret.Password;
            _settingData.Carrier = carrierValue;
            _settingData.Key = key;
            _settingData.LoginTime = int.Parse(loginTime.Text);

            // 保存电费设置
            if (!ElectricitySettings.SaveSettings(_settingData))
                return;

            // 保存邮件设置
            EmailSettings.SaveSettings(_settingData);

            // 保存背景设置
            BackgroundSettings.SaveSettings(_settingData);

            _settingData.Save();

            // 应用背景和图标
            ParentWindow.ApplyBackgroundAndIcon();

            // 重启定时器以应用新的间隔设置
            if (ParentWindow.DataContext is MainViewModel vm)
            {
                vm.TimerMain();
            }

            MessageBox.Show("保存成功", "Info");
        }

        private void MenuListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindParent<ListBoxItem>(e.OriginalSource as DependencyObject) is ListBoxItem item && item.IsSelected)
            {
                e.Handled = true;
            }
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 由滚动联动触发的选中变更不应再执行滚动，避免循环重置
            if (_isScrollingFromMenu) return;
            if (MenuListBox?.SelectedItem is not ListBoxItem item) return;

            string tag = item.Tag?.ToString() ?? "network";

            FrameworkElement? target = tag switch
            {
                "network" => NetworkPanel,
                "electricity" => ElectricitySettings,
                "email" => EmailSettings,
                "background" => BackgroundSettings,
                "about" => AboutPanel,
                _ => null
            };

            ScrollToSection(target);
        }

        private void ScrollToSection(FrameworkElement? target)
        {
            if (target == null || ContentScrollViewer == null) return;
            if (!target.IsLoaded)
            {
                // 控件尚未加载到可视树，延迟到布局完成后重试
                Dispatcher.BeginInvoke(() => ScrollToSection(target), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            _isScrollingFromMenu = true;
            try
            {
                var point = target.TransformToVisual(ContentScrollViewer).Transform(new Point(0, 0));
                ContentScrollViewer.ScrollToVerticalOffset(ContentScrollViewer.VerticalOffset + point.Y);
            }
            catch (InvalidOperationException)
            {
                // 目标控件与 ScrollViewer 暂不在同一可视树中（页面布局尚未完成）
            }
            finally
            {
                Dispatcher.BeginInvoke(() => _isScrollingFromMenu = false, DispatcherPriority.Background);
            }
        }

        private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isScrollingFromMenu) return;
            if (ContentScrollViewer == null) return;

            var sections = new (FrameworkElement? Element, int Index)[]
            {
                (NetworkPanel, 0),
                (ElectricitySettings, 1),
                (EmailSettings, 2),
                (BackgroundSettings, 3),
                (AboutPanel, 4)
            };

            // 底部边界处理：当滚动到最底部时，强制选中最后一个标签
            if (ContentScrollViewer.ScrollableHeight > 0 &&
                ContentScrollViewer.VerticalOffset >= ContentScrollViewer.ScrollableHeight - 0.5)
            {
                int lastIndex = sections.Length - 1;
                if (MenuListBox.SelectedIndex != lastIndex)
                {
                    _isScrollingFromMenu = true;
                    MenuListBox.SelectedIndex = lastIndex;
                    Dispatcher.BeginInvoke(() => _isScrollingFromMenu = false, DispatcherPriority.Background);
                }
                return;
            }

            // 顶部阈值算法：从后往前找到第一个顶部在判定线以上的面板
            double threshold = 40;
            int bestIndex = 0;

            for (int i = sections.Length - 1; i >= 0; i--)
            {
                var (element, index) = sections[i];
                if (element == null) continue;
                var point = element.TransformToVisual(ContentScrollViewer).Transform(new Point(0, 0));
                if (point.Y <= threshold)
                {
                    bestIndex = index;
                    break;
                }
            }

            if (MenuListBox.SelectedIndex != bestIndex)
            {
                _isScrollingFromMenu = true;
                MenuListBox.SelectedIndex = bestIndex;
                Dispatcher.BeginInvoke(() => _isScrollingFromMenu = false, DispatcherPriority.Background);
            }
        }

        #region 辅助方法

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void LoadSettingsToUi(SettingModel data, bool isReset)
        {
            bool hasConfig = isReset && data.PathExist();
            if (hasConfig)
            {
                data = data.Read();
            }

            // 网络设置
            NetworkEnabledCheckBox.IsChecked = !hasConfig || data.NetworkLoginEnabled;
            code.Text = hasConfig ? data.Username : string.Empty;
            secret.Password = hasConfig ? data.Password : string.Empty;
            loginTime.Text = hasConfig ? data.LoginTime.ToString() : "6";
            AutoRun.IsChecked = hasConfig && data.IsAutoRun;
            AutoLogin.IsChecked = hasConfig && data.IsAutoLogin;
            AutoMin.IsChecked = hasConfig && data.IsAutoMin;
            SetLogin.IsChecked = hasConfig && data.IsSetLogin;

            carrier.SelectedIndex = hasConfig ? data.Carrier switch
            {
                "cmcc" => 1,
                "unicom" => 2,
                "telecom" => 3,
                _ => 0
            } : 0;

            ApplyModeRadio(hasConfig ? data.Mode : 0);

            // 电费设置
            ElectricitySettings.LoadSettings(data);

            // 邮件设置
            EmailSettings.LoadSettings(data);

            // 背景设置
            BackgroundSettings.LoadSettings(data);
        }

        private void ApplyModeRadio(int mode)
        {
            ppp.IsChecked = mode == 0;
            cpu.IsChecked = mode == 1;
            auto.IsChecked = mode == 2;
        }

        private int ResolveMode()
        {
            return (ppp.IsChecked, cpu.IsChecked, auto.IsChecked) switch
            {
                (true, _, _) => 0,
                (_, true, _) => 1,
                (_, _, true) => 2,
                _ => 0
            };
        }

        private (string Carrier, int Key) ResolveCarrier()
        {
            return carrier.SelectedIndex switch
            {
                1 => ("cmcc", 1),
                2 => ("unicom", 2),
                3 => ("telecom", 3),
                _ => (string.Empty, 0)
            };
        }

        #endregion
    }
}
