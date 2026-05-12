using CommunityToolkit.Mvvm.Input;
using cpu_net.Constants;
using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel.Base;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Windows;
using Timer = System.Threading.Timer;

namespace cpu_net.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SettingModel _settingData = new SettingModel();
        private Timer? _timer;
        private Timer? _electricityTimer;
        private readonly ElectricityService _electricityService = new ElectricityService();
        private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public MainViewModel()
        {
            Debug.WriteLine("MainViewModel constructor called.");
            TimerMain();
        }

        public void TimerMain()
        {
            var setting = new SettingModel();
            int intervalMs = 1000;
            int elecDueMs = Timeout.Infinite;
            if (setting.PathExist())
            {
                setting = setting.Read();
                if (setting.NetworkLoginEnabled)
                {
                    intervalMs = setting.LoginTime * 1000;
                }
                else
                {
                    intervalMs = Timeout.Infinite;
                }
                if (setting.ElectricityEnabled)
                {
                    var now = DateTime.Now;
                    var target = new DateTime(now.Year, now.Month, now.Day, setting.ElectricityCheckHour, setting.ElectricityCheckMinute, 0);
                    if (target <= now)
                        target = target.AddDays(1);
                    elecDueMs = (int)(target - now).TotalMilliseconds;
                }
            }

            _timer?.Dispose();
            _timer = new Timer(LoginCheck, null, intervalMs, intervalMs);

            _electricityTimer?.Dispose();
            if (elecDueMs != Timeout.Infinite)
            {
                _electricityTimer = new Timer(ElectricityCheck, null, elecDueMs, Timeout.Infinite);
            }
        }

        private async void LoginCheck(object? state)
        {
            var setting = new SettingModel();
            string testUrl = NetworkConstants.GoogleDnsIp;
            string testCode = string.Empty;
            bool isSetLogin = false;

            if (setting.PathExist())
            {
                setting = setting.Read();
                if (!setting.NetworkLoginEnabled)
                {
                    return;
                }
                isSetLogin = setting.IsSetLogin;
                testUrl = setting.TestUrl;
                testCode = setting.TestCode;
            }

            if (!isSetLogin)
            {
                return;
            }

            bool networkAvailable = false;

            try
            {
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    if (content.Trim() == testCode)
                    {
                        networkAvailable = true;
                    }
                    else
                    {
                        Record($"connecttest.txt 内容不匹配：'{content.Trim()}'");
                    }
                }
                else
                {
                    Record($"访问 {testUrl} 失败，HTTP状态码：{response.StatusCode}");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                Record($"网络请求异常：{ex.Message}");
                if (ex.InnerException != null)
                {
                    Record(ex.InnerException.Message);
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Record($"网络连接超时：{ex.Message}");
            }
            catch (Exception ex)
            {
                Record($"检测过程中发生未知异常：{ex.Message}");
            }

            if (!networkAvailable)
            {
                LoginOnline();
                Record("检测到网络断开连接，已尝试重连");
            }
        }

        private async void ElectricityCheck(object? state)
        {
            var setting = new SettingModel();
            if (!setting.PathExist()) return;
            setting = setting.Read();

            if (!setting.ElectricityEnabled || string.IsNullOrWhiteSpace(setting.ElectricityStudentNo))
            {
                return;
            }

            try
            {
                Record($"[{DateTime.Now:HH:mm:ss}] 开始电费查询...");
                var result = await _electricityService.QueryAsync(setting.ElectricityStudentNo);

                if (result.IsRoomNotBound)
                {
                    Record($"[{DateTime.Now:HH:mm:ss}] 电费查询: 未绑定房间");
                    await NotifyService.SendRoomNotBoundAlertAsync(setting, setting.ElectricityStudentNo);
                    Record($"[{DateTime.Now:HH:mm:ss}] 已发送未绑定房间提醒");
                }
                else if (result.Success && (result.Balance.HasValue || result.Degrees.HasValue))
                {
                    var balanceText = result.Balance.HasValue ? $"余额 {result.Balance.Value:F2} 元" : "";
                    var degreesText = result.Degrees.HasValue ? $"电量 {result.Degrees.Value:F2} 度" : "";
                    var infoText = string.Join(" / ", new[] { balanceText, degreesText }.Where(s => !string.IsNullOrEmpty(s)));
                    Record($"[{DateTime.Now:HH:mm:ss}] 电费查询: {infoText}");

                    bool isBelowThreshold = setting.ElectricityThresholdMode switch
                    {
                        1 => result.Degrees.HasValue && result.Degrees.Value < setting.ElectricityThreshold,
                        _ => result.Balance.HasValue && result.Balance.Value < setting.ElectricityThreshold,
                    };

                    if (isBelowThreshold)
                    {
                        var alertValue = setting.ElectricityThresholdMode == 1
                            ? (result.Degrees ?? 0)
                            : (result.Balance ?? 0);
                        await NotifyService.SendElectricityAlertAsync(setting, alertValue, result.RoomInfo);
                        var alertType = setting.ElectricityThresholdMode == 1 ? "电量" : "余额";
                        Record($"[{DateTime.Now:HH:mm:ss}] {alertType}低于阈值，已发送提醒");
                    }
                }
                else
                {
                    Record($"[{DateTime.Now:HH:mm:ss}] 电费查询失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Record($"[{DateTime.Now:HH:mm:ss}] 电费查询异常: {ex.Message}");
            }
            finally
            {
                // 重新设置下一天的定时器
                TimerMain();
            }
        }

        #region 日志属性与命令

        public string TxtLog
        {
            get => LoggingService.ReadLogText("Log");
            set
            {
                LoggingService.WriteTextLog(value, "Log", _settingData.TestMode);
                OnPropertyChanged();
            }
        }

        public string RecordLog
        {
            get => LoggingService.ReadLogText("RecordLog");
            set
            {
                LoggingService.WriteTextLog(value, "RecordLog", _settingData.TestMode);
                OnPropertyChanged();
            }
        }

        public void Info(string message)
        {
            TxtLog = message;
        }

        public void Record(string message)
        {
            RecordLog = message;
        }

        private RelayCommand? _noticeButtonClick;
        public RelayCommand NoticeButton_Click => _noticeButtonClick ??= new RelayCommand(() => NoticeOnline());

        private RelayCommand? _loginButtonClick;
        public RelayCommand LoginButton_Click => _loginButtonClick ??= new RelayCommand(() => LoginOnline());

        private RelayCommand? _bindButtonClick;
        public RelayCommand BindButton_Click => _bindButtonClick ??= new RelayCommand(() => BindOnline());

        #endregion

        public string GetIP()
        {
            string localIP = string.Empty;
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect(NetworkConstants.GoogleDnsIp, NetworkConstants.GoogleDnsPort);

                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    localIP = endPoint.Address.ToString();
                }

                Info($"当前IP为{localIP}");
                Record($"当前IP为{localIP}");
            }
            catch
            {
                Info("IP获取失败");
                Record("IP获取失败");
            }
            return localIP;
        }

        public int LoginOnline()
        {
            if (!_settingData.PathExist())
            {
                Info("No Config Found");
                return 0;
            }

            var ip = GetIP();
            if (string.IsNullOrEmpty(ip))
            {
                Info("请检查网络连接后重试");
                return 0;
            }

            var setting = _settingData.Read();
            int mode = ResolveLoginMode(setting.Mode, ip);

            if (setting.Mode == 2)
            {
                string envText = mode == 0 ? "宽带环境" : "CPU环境";
                Info($"自动识别为{envText}");
            }

            string localIp = TryGetLocalIpFromDrCom(mode, ip);
            string loginUrl = BuildLoginUrl(mode, setting, localIp);

            Record(loginUrl);

            try
            {
                string responseText = NetworkService.HttpGetRequest(loginUrl);
                responseText = responseText.Replace(NetworkConstants.LoginCallback, "").Replace(" ", "");
                Record(responseText);

                // 去除首尾包裹（如 dr1004(...) ）
                if (responseText.Length < 4)
                {
                    Info("网络错误");
                    return 0;
                }

                var json = responseText.Substring(1, responseText.Length - 3);
                var loginResult = JsonSerializer.Deserialize<LoginResult>(json);

                if (loginResult == null)
                {
                    Info("网络错误");
                    return 0;
                }

                if (loginResult.result == 1)
                {
                    Info("登录成功");
                    return 1;
                }

                // result == 0，解析具体错误
                var errorCode = JsonSerializer.Deserialize<LoginErrorCode>(json);
                if (errorCode?.ret_code == 2)
                {
                    Info("本设备已在线，请勿重复登录");
                    return 0;
                }

                var errorMsg = JsonSerializer.Deserialize<LoginErrorMessage>(json);
                Info("登录失败");
                Info($"Error Message: {errorMsg?.msg}");
                return 0;
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                Info("登录失败");
                Info(e.Message);
                Record(e.Message);
                return 0;
            }
            catch (JsonException e)
            {
                Record(e.Message);
                Info("JSON解析失败");
                return 0;
            }
            catch (Exception e)
            {
                Record(e.TargetSite + e.Message + e.StackTrace);
                Info(e.TargetSite + e.Message + e.StackTrace);
                Info("网络连接失败，请检查网络设置，如果使用路由器，请确认是否使用自动获取ip");
                return 0;
            }
        }

        /// <summary>
        /// 根据设置模式与本地 IP 决定实际使用的登录模式
        /// </summary>
        private static int ResolveLoginMode(int settingMode, string ip)
        {
            if (settingMode != 2)
            {
                return settingMode;
            }

            string[] parts = ip.Split('.');
            bool isBroadband = parts[0] == NetworkConstants.IpPrefixes.Broadband1 &&
                               (parts[1] == NetworkConstants.IpPrefixes.BroadbandSegment1 ||
                                parts[1] == NetworkConstants.IpPrefixes.BroadbandSegment2 ||
                                parts[1] == NetworkConstants.IpPrefixes.BroadbandSegment3);
            bool isBroadband192 = parts[0] == NetworkConstants.IpPrefixes.Broadband2;

            return isBroadband || isBroadband192 ? 0 : 1;
        }

        /// <summary>
        /// 尝试通过 DrCOM 接口获取真实本地 IP，失败时回退到传入的 IP
        /// </summary>
        private string TryGetLocalIpFromDrCom(int mode, string fallbackIp)
        {
            string drComUrl = mode == 1
                ? NetworkConstants.CpuDrComUrl
                : NetworkConstants.BroadbandDrComUrl;

            try
            {
                string raw = NetworkService.HttpGetRequest(drComUrl)
                    .Replace(NetworkConstants.DrComCallback, "")
                    .Replace(" ", "");

                if (raw.Length < 3)
                {
                    return fallbackIp;
                }

                var json = raw.Substring(1, raw.Length - 2);
                var result = JsonSerializer.Deserialize<DrComIpResult>(json);
                return result?.ss5 ?? fallbackIp;
            }
            catch (Exception e)
            {
                Record($"Mode Case {mode}");
                Record(e.Message);
                return fallbackIp;
            }
        }

        /// <summary>
        /// 构建登录请求 URL
        /// </summary>
        private static string BuildLoginUrl(int mode, SettingModel setting, string localIp)
        {
            return mode switch
            {
                1 => $"{NetworkConstants.CpuLoginBaseUrl}&user_account=%2C0%2C{setting.Username}&user_password={setting.Password}" +
                     $"&wlan_user_ip={localIp}&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=&wlan_ac_name=&jsVersion=3.3.3&v=1954",
                _ => $"{NetworkConstants.BroadbandLoginBaseUrl}callback={NetworkConstants.LoginCallback}&login_method=1&user_account=%2C0%2C{setting.Username}%40{setting.Carrier}" +
                     $"&user_password={setting.Password}&wlan_user_ip={localIp}&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=&wlan_ac_name=&jsVersion=4.2.2&terminal_type=1&lang=zh-cn&v=9745&lang=zh"
            };
        }

        private static void NoticeOnline()
        {
            System.Diagnostics.Process.Start("explorer.exe", NetworkConstants.TutorialUrl);
        }

        private static void BindOnline()
        {
            System.Diagnostics.Process.Start("explorer.exe", NetworkConstants.SelfServiceUrl);
        }
    }
}
