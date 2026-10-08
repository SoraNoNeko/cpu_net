using cpu_net.Model;
using cpu_net.Services;
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
        private bool _isScrollingFromMenu = false;

        public MainWindow ParentWindow { get; set; }

        public ConfigurationPage()
        {
            InitializeComponent();
            LoadSettingsToUi(new SettingModel(), isReset: true);
            VersionTextBlock.Text = $"版本: {UpdateService.CurrentVersion}";

            MenuListBox.SelectionChanged += MenuListBox_SelectionChanged;
            MenuListBox.PreviewMouseLeftButtonDown += MenuListBox_PreviewMouseLeftButtonDown;
            ContentScrollViewer.ScrollChanged += ContentScrollViewer_ScrollChanged;
        }

        private void Code_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9.-]+");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/SoraNoNeko/cpu_net");
        }

        private void ProxyEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateProxySettingsEnabled();
        }

        private void UpdateProxySettingsEnabled()
        {
            if (ProxySettingsBorder == null) return;
            bool enabled = ProxyEnabledCheckBox.IsChecked == true;
            ProxySettingsBorder.IsEnabled = enabled;
            ProxySettingsBorder.Opacity = enabled ? 1.0 : 0.5;
        }

        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateService.CheckAndPromptUpdateAsync(Window.GetWindow(this));
        }

        private void SaveModule(string name, Func<SettingModel, bool> update, Action? apply = null)
        {
            try
            {
                if (!new SettingsStore().Update(update)) return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{name}保存失败：{ex.Message}", "保存设置", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                apply?.Invoke();
                MessageBox.Show($"{name}已保存", "保存设置");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{name}已保存，应用设置失败：{ex.Message}。请重启软件后重试。", "保存设置");
            }
        }

        private void SaveNetwork_Click(object sender, RoutedEventArgs e)
        {
            SaveModule("网络设置", settings =>
            {
                bool enabled = NetworkEnabledCheckBox.IsChecked == true;
                if (enabled && (string.IsNullOrWhiteSpace(code.Text) || string.IsNullOrEmpty(secret.Password)))
                {
                    MessageBox.Show("请输入学号和密码", "网络设置");
                    return false;
                }
                if (enabled && carrier.SelectedIndex <= 0 && cpu.IsChecked != true)
                {
                    MessageBox.Show("请选择运营商", "网络设置");
                    return false;
                }
                if (!int.TryParse(loginTime.Text, out int interval) || interval < 1 || interval > 86400)
                {
                    MessageBox.Show("定时时长请输入 1 到 86400 之间的整数秒", "网络设置");
                    return false;
                }
                var selectedCarrier = ResolveCarrier();
                settings.NetworkLoginEnabled = enabled;
                settings.IsAutoRun = AutoRun.IsChecked == true;
                settings.IsAutoLogin = AutoLogin.IsChecked == true;
                settings.IsAutoMin = AutoMin.IsChecked == true;
                settings.IsSetLogin = SetLogin.IsChecked == true;
                settings.Mode = ResolveMode();
                settings.Username = code.Text.Trim();
                settings.Password = secret.Password;
                settings.Carrier = selectedCarrier.Carrier;
                settings.Key = selectedCarrier.Key;
                settings.LoginTime = interval;
                return true;
            }, () =>
            {
                new AutoStart().SetMeAutoStart(AutoRun.IsChecked == true);
                (ParentWindow?.DataContext as MainViewModel)?.RestartNetworkTimer();
            });
        }

        private void SaveElectricity_Click(object sender, RoutedEventArgs e) =>
            SaveModule("电费设置", settings => ElectricitySettings.SaveSettings(settings),
                () => (ParentWindow?.DataContext as MainViewModel)?.RestartElectricityTimer());

        private void SaveEmail_Click(object sender, RoutedEventArgs e) =>
            SaveModule("邮件设置", settings => EmailSettings.SaveSettings(settings));

        private void SaveBackground_Click(object sender, RoutedEventArgs e) =>
            SaveModule("背景与图标", settings => { BackgroundSettings.SaveSettings(settings); return true; },
                () => ParentWindow?.ApplyBackgroundAndIcon());

        private void SaveProxy_Click(object sender, RoutedEventArgs e)
        {
            SaveModule("代理设置", settings =>
            {
                bool enabled = ProxyEnabledCheckBox.IsChecked == true;
                bool validPort = int.TryParse(ProxyPortTextBox.Text, out int port) && port >= 1 && port <= 65535;
                if (enabled && (string.IsNullOrWhiteSpace(ProxyHostTextBox.Text) || !validPort))
                {
                    MessageBox.Show("请输入代理服务器及 1 到 65535 之间的端口", "代理设置");
                    return false;
                }
                settings.UpdateProxyEnabled = enabled;
                settings.UpdateProxyType = ProxyTypeComboBox.SelectedItem is ComboBoxItem item ? item.Tag?.ToString() ?? "HTTP" : "HTTP";
                settings.UpdateProxyHost = ProxyHostTextBox.Text.Trim();
                settings.UpdateProxyPort = validPort ? port : 0;
                settings.UpdateProxyUsername = ProxyUsernameTextBox.Text.Trim();
                settings.UpdateProxyPassword = ProxyPasswordBox.Password;
                return true;
            });
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
                "proxy" => ProxyPanel,
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
                (ProxyPanel, 4),
                (AboutPanel, 5)
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

            // 代理设置
            ProxyEnabledCheckBox.IsChecked = hasConfig && data.UpdateProxyEnabled;
            ProxyTypeComboBox.SelectedIndex = hasConfig ? data.UpdateProxyType?.ToUpperInvariant() switch
            {
                "SOCKS5" => 1,
                "SOCKS4" => 2,
                _ => 0
            } : 0;
            ProxyHostTextBox.Text = hasConfig ? data.UpdateProxyHost : string.Empty;
            ProxyPortTextBox.Text = hasConfig && data.UpdateProxyPort > 0 ? data.UpdateProxyPort.ToString() : string.Empty;
            ProxyUsernameTextBox.Text = hasConfig ? data.UpdateProxyUsername : string.Empty;
            ProxyPasswordBox.Password = hasConfig ? data.UpdateProxyPassword : string.Empty;

            UpdateProxySettingsEnabled();
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
