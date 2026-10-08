using Prism.Mvvm;
using System;
using System.IO;
using cpu_net.Services;

namespace cpu_net.Model
{
    public class SettingModel : BindableBase
    {
        private const string DefaultTestUrl = "http://www.msftconnecttest.com/connecttest.txt";
        private const string DefaultTestCode = "Microsoft Connect Test";

        private readonly string _settingDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");

        private string _password = string.Empty;
        private string _userName = string.Empty;
        private string _carrier = string.Empty;
        private int _key;
        private int _mode;
        private bool? _isAutoRun;
        private bool? _isAutoLogin;
        private bool? _isAutoMin;
        private bool? _isSetLogin;
        private int? _loginTime;
        private bool? _networkLoginEnabled;
        private bool? _testMode;
        private string? _testUrl;
        private string? _testCode;

        // 电费查询设置
        private bool? _electricityEnabled;
        private string? _electricityStudentNo;
        private decimal? _electricityThreshold;
        private int? _electricityThresholdMode;
        private int? _electricityIntervalMinutes;
        private int? _electricityCheckHour;
        private int? _electricityCheckMinute;

        // 提醒方式: 0=系统通知, 1=邮件通知, 2=两者
        private int? _notifyType;

        // 邮件提醒设置
        private bool? _emailEnabled;
        private string? _emailSmtpServer;
        private int? _emailSmtpPort;
        private string? _emailUsername;
        private string? _emailPassword;
        private string? _emailTo;
        private string? _emailAlertSubject;
        private string? _emailAlertBody;
        private string? _emailNotBoundSubject;
        private string? _emailNotBoundBody;

        // 背景与图标设置
        private string? _backgroundImagePath;
        private double? _backgroundOpacity;
        private string? _customIconPath;
        private double? _textBoxOpacity;

        // 更新代理设置
        private bool? _updateProxyEnabled;
        private string? _updateProxyType; // HTTP, SOCKS5, SOCKS4
        private string? _updateProxyHost;
        private int? _updateProxyPort;
        private string? _updateProxyUsername;
        private string? _updateProxyPassword;

        public bool PathExist() => File.Exists(_settingDataPath);

        public string Username
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Carrier
        {
            get => _carrier;
            set => SetProperty(ref _carrier, value);
        }

        public int Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public int Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public bool IsAutoRun
        {
            get => _isAutoRun ?? false;
            set => SetProperty(ref _isAutoRun, value);
        }

        public bool IsAutoLogin
        {
            get => _isAutoLogin ?? false;
            set => SetProperty(ref _isAutoLogin, value);
        }

        public bool IsAutoMin
        {
            get => _isAutoMin ?? false;
            set => SetProperty(ref _isAutoMin, value);
        }

        public bool IsSetLogin
        {
            get => _isSetLogin ?? false;
            set => SetProperty(ref _isSetLogin, value);
        }

        public bool NetworkLoginEnabled
        {
            get => _networkLoginEnabled ?? true;
            set => SetProperty(ref _networkLoginEnabled, value);
        }

        public int LoginTime
        {
            get => _loginTime ?? 5;
            set => SetProperty(ref _loginTime, value);
        }

        public bool TestMode
        {
            get => _testMode ?? false;
            set => SetProperty(ref _testMode, value);
        }

        public string TestUrl
        {
            get => _testUrl ?? DefaultTestUrl;
            set => SetProperty(ref _testUrl, value);
        }

        public string TestCode
        {
            get => _testCode ?? DefaultTestCode;
            set => SetProperty(ref _testCode, value);
        }

        #region 电费查询

        public bool ElectricityEnabled
        {
            get => _electricityEnabled ?? false;
            set => SetProperty(ref _electricityEnabled, value);
        }

        public string ElectricityStudentNo
        {
            get => _electricityStudentNo ?? string.Empty;
            set => SetProperty(ref _electricityStudentNo, value);
        }

        public decimal ElectricityThreshold
        {
            get => _electricityThreshold ?? 10m;
            set => SetProperty(ref _electricityThreshold, value);
        }

        public int ElectricityThresholdMode
        {
            get => _electricityThresholdMode ?? 0;
            set => SetProperty(ref _electricityThresholdMode, value);
        }

        public int ElectricityIntervalMinutes
        {
            get => _electricityIntervalMinutes ?? 60;
            set => SetProperty(ref _electricityIntervalMinutes, value);
        }

        public int ElectricityCheckHour
        {
            get => _electricityCheckHour ?? 8;
            set => SetProperty(ref _electricityCheckHour, value);
        }

        public int ElectricityCheckMinute
        {
            get => _electricityCheckMinute ?? 0;
            set => SetProperty(ref _electricityCheckMinute, value);
        }

        #endregion

        #region 邮件设置

        public bool EmailEnabled
        {
            get => _emailEnabled ?? false;
            set => SetProperty(ref _emailEnabled, value);
        }

        public string EmailSmtpServer
        {
            get => _emailSmtpServer ?? string.Empty;
            set => SetProperty(ref _emailSmtpServer, value);
        }

        public int EmailSmtpPort
        {
            get => _emailSmtpPort ?? 587;
            set => SetProperty(ref _emailSmtpPort, value);
        }

        public string EmailUsername
        {
            get => _emailUsername ?? string.Empty;
            set => SetProperty(ref _emailUsername, value);
        }

        public string EmailPassword
        {
            get => _emailPassword ?? string.Empty;
            set => SetProperty(ref _emailPassword, value);
        }

        public string EmailTo
        {
            get => _emailTo ?? string.Empty;
            set => SetProperty(ref _emailTo, value);
        }

        public string EmailAlertSubject
        {
            get => _emailAlertSubject ?? "【CPU_NET】电费余额不足提醒";
            set => SetProperty(ref _emailAlertSubject, value);
        }

        public string EmailAlertBody
        {
            get => _emailAlertBody ?? "<html><body><h2>电费余额不足提醒</h2><p>您的电费余额已低于设定阈值，请及时充值。</p><table border='1' cellpadding='8' style='border-collapse:collapse;'><tr><td>当前余额</td><td><b>{balance} 元</b></td></tr><tr><td>提醒阈值</td><td>{threshold} 元</td></tr><tr><td>房间信息</td><td>{roomInfo}</td></tr><tr><td>提醒时间</td><td>{time}</td></tr></table><p style='color:#888;font-size:12px;'>本邮件由 CPU_NET 自动发送，请勿直接回复。</p></body></html>";
            set => SetProperty(ref _emailAlertBody, value);
        }

        public string EmailNotBoundSubject
        {
            get => _emailNotBoundSubject ?? "【CPU_NET】电费查询异常提醒";
            set => SetProperty(ref _emailNotBoundSubject, value);
        }

        public string EmailNotBoundBody
        {
            get => _emailNotBoundBody ?? "<html><body><h2>电费查询异常提醒</h2><p>系统在查询电费时发现您的账号未绑定房间，无法获取电费余额。</p><table border='1' cellpadding='8' style='border-collapse:collapse;'><tr><td>学号</td><td>{studentNo}</td></tr><tr><td>异常类型</td><td><b>未绑定房间</b></td></tr><tr><td>提醒时间</td><td>{time}</td></tr></table><p>请前往能源管控平台绑定房间后重试。</p><p style='color:#888;font-size:12px;'>本邮件由 CPU_NET 自动发送，请勿直接回复。</p></body></html>";
            set => SetProperty(ref _emailNotBoundBody, value);
        }

        #endregion

        #region 提醒设置

        public int NotifyType
        {
            get => _notifyType ?? 0;
            set => SetProperty(ref _notifyType, value);
        }

        #endregion

        #region 背景与图标

        public string BackgroundImagePath
        {
            get => _backgroundImagePath ?? string.Empty;
            set => SetProperty(ref _backgroundImagePath, value);
        }

        public double BackgroundOpacity
        {
            get => _backgroundOpacity ?? 0.5;
            set => SetProperty(ref _backgroundOpacity, value);
        }

        public string CustomIconPath
        {
            get => _customIconPath ?? string.Empty;
            set => SetProperty(ref _customIconPath, value);
        }

        public double TextBoxOpacity
        {
            get => _textBoxOpacity ?? 0.8;
            set => SetProperty(ref _textBoxOpacity, value);
        }

        public bool UpdateProxyEnabled
        {
            get => _updateProxyEnabled ?? false;
            set => SetProperty(ref _updateProxyEnabled, value);
        }

        public string UpdateProxyType
        {
            get => _updateProxyType ?? "HTTP";
            set => SetProperty(ref _updateProxyType, value);
        }

        public string UpdateProxyHost
        {
            get => _updateProxyHost ?? string.Empty;
            set => SetProperty(ref _updateProxyHost, value);
        }

        public int UpdateProxyPort
        {
            get => _updateProxyPort ?? 0;
            set => SetProperty(ref _updateProxyPort, value);
        }

        public string UpdateProxyUsername
        {
            get => _updateProxyUsername ?? string.Empty;
            set => SetProperty(ref _updateProxyUsername, value);
        }

        public string UpdateProxyPassword
        {
            get => _updateProxyPassword ?? string.Empty;
            set => SetProperty(ref _updateProxyPassword, value);
        }

        #endregion

        public SettingModel Read() => new SettingsStore(_settingDataPath).Read();

        public void Save() => new SettingsStore(_settingDataPath).Save(this);
    }
}
