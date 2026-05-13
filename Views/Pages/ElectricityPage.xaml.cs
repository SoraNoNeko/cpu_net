using cpu_net.Model;
using cpu_net.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace cpu_net.Views.Pages
{
    public partial class ElectricityPage : Page
    {
        private readonly SettingModel _settingData = new SettingModel();
        private readonly ElectricityService _electricityService = new ElectricityService();

        public MainWindow? ParentWindow { get; set; }

        public ElectricityPage()
        {
            InitializeComponent();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_settingData.PathExist())
            {
                MessageBox.Show("请先配置电费查询设置", "提示");
                return;
            }

            var setting = _settingData.Read();
            if (!setting.ElectricityEnabled || string.IsNullOrWhiteSpace(setting.ElectricityStudentNo))
            {
                MessageBox.Show("请先启用电费查询并设置学号", "提示");
                return;
            }

            await ExecuteRefreshAsync(setting);
        }

        private async System.Threading.Tasks.Task ExecuteRefreshAsync(SettingModel setting)
        {
            SetRefreshingState(true);
            ClearErrorTip();
            AppendLog($"[{DateTime.Now:HH:mm:ss}] 开始刷新学号 {setting.ElectricityStudentNo} 的电费...");

            try
            {
                // 先检测服务器是否可达
                var (reachable, serverMsg) = await ElectricityService.CheckServerReachableAsync();
                UpdateNetworkStatus(reachable, serverMsg);

                if (!reachable)
                {
                    ShowErrorTip($"无法访问电费服务器: {serverMsg}\n请检查网络连接或确认服务器地址是否正确。");
                    BalanceText.Text = "--";
                    BalanceText.Foreground = Brushes.Gray;
                    DegreesText.Text = "--";
                    DegreesText.Foreground = Brushes.Gray;
                    RoomInfoText.Text = "";
                    StatusText.Text = "服务器不可达";
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 服务器不可达: {serverMsg}");
                    return;
                }

                var result = await _electricityService.QueryAsync(setting.ElectricityStudentNo);

                if (result.IsRoomNotBound)
                {
                    BalanceText.Text = "--";
                    BalanceText.Foreground = Brushes.Orange;
                    DegreesText.Text = "--";
                    DegreesText.Foreground = Brushes.Orange;
                    RoomInfoText.Text = "";
                    StatusText.Text = "未绑定房间";
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 查询结果: 未绑定房间");
                    await NotifyService.SendRoomNotBoundAlertAsync(setting, setting.ElectricityStudentNo);
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 已发送未绑定房间提醒");
                }
                else if (result.Success && (result.Balance.HasValue || result.Degrees.HasValue))
                {
                    BalanceText.Text = result.Balance.HasValue ? $"{result.Balance.Value:F2} 元" : "--";
                    DegreesText.Text = result.Degrees.HasValue ? $"{result.Degrees.Value:F2} 度" : "--";

                    // 根据衡量模式判断阈值
                    bool isBelowThreshold = setting.ElectricityThresholdMode switch
                    {
                        1 => result.Degrees.HasValue && result.Degrees.Value < setting.ElectricityThreshold,
                        _ => result.Balance.HasValue && result.Balance.Value < setting.ElectricityThreshold,
                    };

                    if (setting.ElectricityThresholdMode == 1)
                    {
                        // 度数模式：仅度数参与阈值判断
                        BalanceText.Foreground = Brushes.ForestGreen;
                        DegreesText.Foreground = isBelowThreshold ? Brushes.Red : Brushes.ForestGreen;
                    }
                    else
                    {
                        // 费用模式：仅费用参与阈值判断
                        BalanceText.Foreground = isBelowThreshold ? Brushes.Red : Brushes.ForestGreen;
                        DegreesText.Foreground = Brushes.ForestGreen;
                    }

                    StatusText.Text = $"上次刷新: {result.QueryTime:HH:mm:ss}";
                    RoomInfoText.Text = string.IsNullOrEmpty(result.RoomInfo) ? "" : $"房间: {result.RoomInfo}";

                    var logParts = new System.Collections.Generic.List<string>();
                    if (result.Balance.HasValue) logParts.Add($"余额 {result.Balance.Value:F2} 元");
                    if (result.Degrees.HasValue) logParts.Add($"电量 {result.Degrees.Value:F2} 度");
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 刷新成功: {string.Join(" / ", logParts)}");

                    if (isBelowThreshold)
                    {
                        var alertValue = setting.ElectricityThresholdMode == 1
                            ? (result.Degrees ?? 0)
                            : (result.Balance ?? 0);
                        await NotifyService.SendElectricityAlertAsync(setting, alertValue, result.RoomInfo);
                        var alertType = setting.ElectricityThresholdMode == 1 ? "电量" : "余额";
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] {alertType}低于阈值，已发送提醒");
                    }
                }
                else
                {
                    BalanceText.Text = "--";
                    BalanceText.Foreground = Brushes.Gray;
                    DegreesText.Text = "--";
                    DegreesText.Foreground = Brushes.Gray;
                    RoomInfoText.Text = "";
                    StatusText.Text = "查询失败";
                    string errDetail = result.ErrorMessage ?? "未知错误";
                    ShowErrorTip($"刷新失败: {errDetail}\n请确认学号正确，或服务器是否正常运行。");
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 刷新失败: {errDetail}");
                    if (!string.IsNullOrEmpty(result.RawResponse))
                    {
                        AppendLog($"原始响应: {result.RawResponse.Substring(0, Math.Min(200, result.RawResponse.Length))}...");
                    }
                }
            }
            catch (Exception ex)
            {
                BalanceText.Text = "--";
                BalanceText.Foreground = Brushes.Gray;
                DegreesText.Text = "--";
                DegreesText.Foreground = Brushes.Gray;
                RoomInfoText.Text = "";
                StatusText.Text = "查询异常";
                ShowErrorTip($"发生异常: {ex.Message}\n请检查网络连接或稍后重试。");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] 异常: {ex.Message}");
            }
            finally
            {
                SetRefreshingState(false);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow?.NavigateToSettings("electricity");
        }

        /// <summary>
        /// 设置刷新按钮的加载状态
        /// </summary>
        private void SetRefreshingState(bool isRefreshing)
        {
            RefreshButton.IsEnabled = !isRefreshing;
            RefreshText.Text = isRefreshing ? "刷新中..." : "刷新";

            if (isRefreshing)
            {
                // 旋转动画
                var rotate = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.8))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                };
                var transform = new RotateTransform();
                RefreshIcon.RenderTransform = transform;
                RefreshIcon.RenderTransformOrigin = new Point(0.5, 0.5);
                transform.BeginAnimation(RotateTransform.AngleProperty, rotate);
                StatusText.Text = "刷新中...";
            }
            else
            {
                RefreshIcon.RenderTransform = null;
            }
        }

        /// <summary>
        /// 更新网络状态指示器
        /// </summary>
        private void UpdateNetworkStatus(bool reachable, string message)
        {
            NetworkStatusPanel.Visibility = Visibility.Visible;
            if (reachable)
            {
                NetworkStatusDot.Fill = Brushes.Green;
                NetworkStatusText.Text = "服务器在线";
                NetworkStatusText.Foreground = Brushes.Green;
            }
            else
            {
                NetworkStatusDot.Fill = Brushes.Red;
                NetworkStatusText.Text = "服务器离线";
                NetworkStatusText.Foreground = Brushes.Red;
            }
        }

        private void ShowErrorTip(string message)
        {
            ErrorTipText.Text = message;
            ErrorTipText.Visibility = Visibility.Visible;
            OpenWebButton.Visibility = Visibility.Visible;
        }

        private void ClearErrorTip()
        {
            ErrorTipText.Text = "";
            ErrorTipText.Visibility = Visibility.Collapsed;
            OpenWebButton.Visibility = Visibility.Collapsed;
        }

        private void OpenWebButton_Click(object sender, RoutedEventArgs e)
        {
            var setting = _settingData.Read();
            string studentNo = setting.ElectricityStudentNo;
            string url = $"http://10.200.13.18:8899/h5/#/?no={System.Uri.EscapeDataString(studentNo)}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void AppendLog(string message)
        {
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
            LoggingService.WriteTextLog(message, "Log", false);
        }
    }
}
