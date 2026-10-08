using CommunityToolkit.Mvvm.Input;
using cpu_net.Constants;
using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Timer = System.Threading.Timer;

namespace cpu_net.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SettingModel _settingData = new SettingModel();
        private Timer? _timer;
        private Timer? _electricityTimer;
        private int _checkingNetwork;
        private int _loggingIn;
        private readonly TunLoginGuard _tunGuard = new TunLoginGuard();
        private readonly ElectricityService _electricityService = new ElectricityService();
        private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        // 内存日志缓冲区（即时显示，不依赖文件IO）
        private readonly Queue<string> _txtLogQueue = new Queue<string>();
        private readonly Queue<string> _recordLogQueue = new Queue<string>();
        private const int MaxLogLines = 500;

        public MainViewModel()
        {
            Debug.WriteLine("MainViewModel constructor called.");
            _ = LoadHistoryLogsAsync();
            TimerMain();
        }

        /// <summary>
        /// 启动时异步加载历史日志到内存
        /// </summary>
        private async Task LoadHistoryLogsAsync()
        {
            var txtLog = await Task.Run(() => LoggingService.ReadLogText("Log", MaxLogLines));
            var recordLog = await Task.Run(() => LoggingService.ReadLogText("RecordLog", MaxLogLines));

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var line in txtLog.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    _txtLogQueue.Enqueue(line);
                foreach (var line in recordLog.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    _recordLogQueue.Enqueue(line);

                OnPropertyChanged(nameof(TxtLog));
                OnPropertyChanged(nameof(RecordLog));
            });
        }

        public void TimerMain()
        {
            RestartNetworkTimer();
            RestartElectricityTimer();
        }

        public void RestartNetworkTimer()
        {
            var setting = _settingData.Read();
            int intervalMs = setting.NetworkLoginEnabled
                ? Math.Clamp(setting.LoginTime, 1, 86400) * 1000 : Timeout.Infinite;
            _timer?.Dispose();
            _timer = new Timer(LoginCheck, null, intervalMs, intervalMs);
        }

        public void RestartElectricityTimer()
        {
            var setting = _settingData.Read();
            _electricityTimer?.Dispose();
            _electricityTimer = null;
            if (!setting.ElectricityEnabled) return;
            var now = DateTime.Now;
            var target = new DateTime(now.Year, now.Month, now.Day,
                Math.Clamp(setting.ElectricityCheckHour, 0, 23), Math.Clamp(setting.ElectricityCheckMinute, 0, 59), 0);
            if (target <= now) target = target.AddDays(1);
            _electricityTimer = new Timer(ElectricityCheck, null, (int)(target - now).TotalMilliseconds, Timeout.Infinite);
        }

        private bool PauseLoginForTun() => _tunGuard.ShouldPause(TunDetectionService.Detect(), Info);

        private async void LoginCheck(object? state)
        {
            if (Interlocked.Exchange(ref _checkingNetwork, 1) != 0) return;
            try
            {
                await CheckNetworkAsync();
            }
            catch (Exception ex)
            {
                Info($"网络检测异常：{NetworkService.DescribeFailure(ex)}");
            }
            finally
            {
                Volatile.Write(ref _checkingNetwork, 0);
            }
        }

        private async Task CheckNetworkAsync()
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

            if (PauseLoginForTun()) return;

            bool networkAvailable = false;
            var timer = Stopwatch.StartNew();
            string endpoint = Uri.TryCreate(testUrl, UriKind.Absolute, out var testUri)
                ? testUri.GetLeftPart(UriPartial.Path) : "测试地址格式无效";

            try
            {
                using var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    if (content.Trim() == testCode)
                    {
                        networkAvailable = true;
                    }
                    else
                    {
                        Info($"网络检测失败：{endpoint}；HTTP {(int)response.StatusCode}；正文与配置不匹配；实际长度 {content.Trim().Length}，预期长度 {testCode.Length}；耗时 {timer.ElapsedMilliseconds} ms");
                    }
                }
                else
                {
                    Info($"网络检测失败：{endpoint}；HTTP {(int)response.StatusCode}；耗时 {timer.ElapsedMilliseconds} ms");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                Info($"网络检测失败：{endpoint}；{NetworkService.DescribeFailure(ex)}；耗时 {timer.ElapsedMilliseconds} ms；检测使用系统代理设置");
            }
            catch (System.Threading.Tasks.TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Info($"网络检测失败：{endpoint}；{NetworkService.DescribeFailure(ex)}；耗时 {timer.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Info($"网络检测失败：{endpoint}；{NetworkService.DescribeFailure(ex)}；耗时 {timer.ElapsedMilliseconds} ms");
            }

            if (PauseLoginForTun()) return;

            if (!networkAvailable)
            {
                Info("外网检测未通过，开始检查认证接口并尝试登录；检测失败可能来自 DNS、代理或测试站点。");
                await Task.Run(() => LoginOnline());
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
                // 维护时段检查（23:00-08:00）
                var now = DateTime.Now;
                if (now.Hour >= 23 || now.Hour < 8)
                {
                    Record($"[{now:HH:mm:ss}] 电费查询跳过：服务器维护时段（23:00-08:00）");
                    return;
                }

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
                Info($"电费查询异常: {ex.Message}");
            }
            finally
            {
                // 重新设置下一天的定时器
                TimerMain();
            }
        }

        #region 日志属性与命令

        public string TxtLog => string.Join(Environment.NewLine, _txtLogQueue);
        public string RecordLog => string.Join(Environment.NewLine, _recordLogQueue);

        public void Info(string message)
        {
            var line = $"{DateTime.Now:M-d HH:mm:ss}  {message}";
            lock (_txtLogQueue)
            {
                _txtLogQueue.Enqueue(line);
                while (_txtLogQueue.Count > MaxLogLines) _txtLogQueue.Dequeue();
            }
            OnPropertyChanged(nameof(TxtLog));
            LoggingService.WriteTextLog(message, "Log", _settingData.TestMode);
        }

        public void Record(string message)
        {
            var line = $"{DateTime.Now:M-d HH:mm:ss}  {message}";
            lock (_recordLogQueue)
            {
                _recordLogQueue.Enqueue(line);
                while (_recordLogQueue.Count > MaxLogLines) _recordLogQueue.Dequeue();
            }
            OnPropertyChanged(nameof(RecordLog));
            LoggingService.WriteTextLog(message, "RecordLog", _settingData.TestMode);
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
            if (Interlocked.Exchange(ref _loggingIn, 1) != 0) return 0;
            try { return PauseLoginForTun() ? 0 : LoginCore(); }
            finally { Volatile.Write(ref _loggingIn, 0); }
        }

        private int LoginCore()
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

            if (PauseLoginForTun()) return 0;
            string localIp = TryGetLocalIpFromDrCom(mode, ip);
            string loginUrl = BuildLoginUrl(mode, setting, localIp);


            try
            {
                if (PauseLoginForTun()) return 0;
                string responseText = NetworkService.HttpGetRequest(loginUrl, Info);

                // 去除首尾包裹（如 dr1004(...) ）
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    Info("登录失败：认证接口返回空正文，请检查上方 HTTP 状态及耗时。");
                    return 0;
                }

                var json = NetworkService.ExtractJson(responseText, NetworkConstants.LoginCallback);
                var loginResult = JsonSerializer.Deserialize<LoginResult>(json);

                if (loginResult == null)
                {
                    Info("登录失败：认证接口返回 JSON null。");
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
                Info($"认证结果：result={loginResult.result}；ret_code={errorCode?.ret_code}；msg={errorMsg?.msg}");
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
                Info($"登录响应解析失败：{e.Message}");
                return 0;
            }
            catch (Exception e)
            {
                Info($"登录请求失败：{(e is InvalidOperationException ? e.Message : NetworkService.DescribeFailure(e))}");
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
                string raw = NetworkService.HttpGetRequest(drComUrl, Info);

                if (raw.Length < 3)
                {
                    Info($"状态接口返回空或过短正文，使用本地 IP {fallbackIp}。");
                    return fallbackIp;
                }

                var json = NetworkService.ExtractJson(raw, NetworkConstants.DrComCallback);
                var result = JsonSerializer.Deserialize<DrComIpResult>(json);
                if (IPAddress.TryParse(result?.ss5, out var address) && address.AddressFamily == AddressFamily.InterNetwork)
                {
                    Info($"状态接口确认认证 IP：{address}");
                    return address.ToString();
                }
                Info($"状态接口未返回有效 IPv4 ss5，使用本地 IP {fallbackIp}。");
                return fallbackIp;
            }
            catch (Exception e)
            {
                Info($"状态查询失败：{(e is InvalidOperationException || e is JsonException ? e.Message : NetworkService.DescribeFailure(e))}；使用本地 IP {fallbackIp} 继续认证。");
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
                1 => $"{NetworkConstants.CpuLoginBaseUrl}&user_account=%2C0%2C{Uri.EscapeDataString(setting.Username)}&user_password={Uri.EscapeDataString(setting.Password)}" +
                     $"&wlan_user_ip={localIp}&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=&wlan_ac_name=&jsVersion=3.3.3&v=1954",
                _ => $"{NetworkConstants.BroadbandLoginBaseUrl}callback={NetworkConstants.LoginCallback}&login_method=1&user_account=%2C0%2C{Uri.EscapeDataString(setting.Username)}%40{Uri.EscapeDataString(setting.Carrier)}" +
                     $"&user_password={Uri.EscapeDataString(setting.Password)}&wlan_user_ip={localIp}&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=&wlan_ac_name=&jsVersion=4.2.2&terminal_type=1&lang=zh-cn&v=9745&lang=zh"
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
