using cpu_net.Model;
using cpu_net.ViewModel.Base;

namespace cpu_net.ViewModel
{
    public class UserViewModel : ViewModelBase
    {
        private readonly SettingModel _settingData = new SettingModel();
        private readonly AutoStart _autoStart = new AutoStart();

        public UserViewModel()
        {
            if (_settingData.PathExist())
            {
                _settingData = _settingData.Read();
                Code = _settingData.Username;
                Password = _settingData.Password;
                IsAutoRun = _settingData.IsAutoRun;
                IsAutoLogin = _settingData.IsAutoLogin;
                IsAutoMin = _settingData.IsAutoMin;
                IsSetLogin = _settingData.IsSetLogin;
                NetworkLoginEnabled = _settingData.NetworkLoginEnabled;
                Mode = _settingData.Mode;
                LoginTime = _settingData.LoginTime;
                ElectricityEnabled = _settingData.ElectricityEnabled;
                ElectricityStudentNo = _settingData.ElectricityStudentNo;
                ElectricityThreshold = _settingData.ElectricityThreshold;
                ElectricityThresholdMode = _settingData.ElectricityThresholdMode;
                ElectricityIntervalMinutes = _settingData.ElectricityIntervalMinutes;
                ElectricityCheckHour = _settingData.ElectricityCheckHour;
                ElectricityCheckMinute = _settingData.ElectricityCheckMinute;
                NotifyType = _settingData.NotifyType;
                EmailEnabled = _settingData.EmailEnabled;
                EmailSmtpServer = _settingData.EmailSmtpServer;
                EmailSmtpPort = _settingData.EmailSmtpPort;
                EmailUsername = _settingData.EmailUsername;
                EmailPassword = _settingData.EmailPassword;
                EmailTo = _settingData.EmailTo;
                EmailAlertSubject = _settingData.EmailAlertSubject;
                EmailAlertBody = _settingData.EmailAlertBody;
                EmailNotBoundSubject = _settingData.EmailNotBoundSubject;
                EmailNotBoundBody = _settingData.EmailNotBoundBody;
                BackgroundImagePath = _settingData.BackgroundImagePath;
                BackgroundOpacity = _settingData.BackgroundOpacity;
                CustomIconPath = _settingData.CustomIconPath;
            }
        }

        private string _code;
        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        private bool _isAutoRun;
        public bool IsAutoRun
        {
            get => _isAutoRun;
            set
            {
                _isAutoRun = value;
                _settingData.IsAutoRun = _isAutoRun;
                _autoStart.SetMeAutoStart(_isAutoRun);
                OnPropertyChanged();
            }
        }

        private bool _isAutoLogin;
        public bool IsAutoLogin
        {
            get => _isAutoLogin;
            set { _isAutoLogin = value; _settingData.IsAutoLogin = _isAutoLogin; OnPropertyChanged(); }
        }

        private bool _isAutoMin;
        public bool IsAutoMin
        {
            get => _isAutoMin;
            set { _isAutoMin = value; _settingData.IsAutoMin = _isAutoMin; OnPropertyChanged(); }
        }

        private bool _isSetLogin;
        public bool IsSetLogin
        {
            get => _isSetLogin;
            set { _isSetLogin = value; _settingData.IsSetLogin = _isSetLogin; OnPropertyChanged(); }
        }

        private bool _networkLoginEnabled;
        public bool NetworkLoginEnabled
        {
            get => _networkLoginEnabled;
            set { _networkLoginEnabled = value; _settingData.NetworkLoginEnabled = _networkLoginEnabled; OnPropertyChanged(); }
        }

        private int _mode;
        public int Mode
        {
            get => _mode;
            set { _mode = value; _settingData.Mode = _mode; OnPropertyChanged(); }
        }

        private int _loginTime;
        public int LoginTime
        {
            get => _loginTime;
            set { _loginTime = value; _settingData.LoginTime = _loginTime; OnPropertyChanged(); }
        }

        #region 电费设置

        private bool _electricityEnabled;
        public bool ElectricityEnabled
        {
            get => _electricityEnabled;
            set { _electricityEnabled = value; OnPropertyChanged(); }
        }

        private string _electricityStudentNo;
        public string ElectricityStudentNo
        {
            get => _electricityStudentNo;
            set { _electricityStudentNo = value; OnPropertyChanged(); }
        }

        private decimal _electricityThreshold;
        public decimal ElectricityThreshold
        {
            get => _electricityThreshold;
            set { _electricityThreshold = value; OnPropertyChanged(); }
        }

        private int _electricityThresholdMode;
        public int ElectricityThresholdMode
        {
            get => _electricityThresholdMode;
            set { _electricityThresholdMode = value; OnPropertyChanged(); }
        }

        private int _electricityIntervalMinutes;
        public int ElectricityIntervalMinutes
        {
            get => _electricityIntervalMinutes;
            set { _electricityIntervalMinutes = value; OnPropertyChanged(); }
        }

        private int _electricityCheckHour;
        public int ElectricityCheckHour
        {
            get => _electricityCheckHour;
            set { _electricityCheckHour = value; _settingData.ElectricityCheckHour = _electricityCheckHour; OnPropertyChanged(); }
        }

        private int _electricityCheckMinute;
        public int ElectricityCheckMinute
        {
            get => _electricityCheckMinute;
            set { _electricityCheckMinute = value; _settingData.ElectricityCheckMinute = _electricityCheckMinute; OnPropertyChanged(); }
        }

        private int _notifyType;
        public int NotifyType
        {
            get => _notifyType;
            set { _notifyType = value; OnPropertyChanged(); }
        }

        #endregion

        #region 邮件设置

        private bool _emailEnabled;
        public bool EmailEnabled
        {
            get => _emailEnabled;
            set { _emailEnabled = value; OnPropertyChanged(); }
        }

        private string _emailSmtpServer;
        public string EmailSmtpServer
        {
            get => _emailSmtpServer;
            set { _emailSmtpServer = value; OnPropertyChanged(); }
        }

        private int _emailSmtpPort;
        public int EmailSmtpPort
        {
            get => _emailSmtpPort;
            set { _emailSmtpPort = value; OnPropertyChanged(); }
        }

        private string _emailUsername;
        public string EmailUsername
        {
            get => _emailUsername;
            set { _emailUsername = value; OnPropertyChanged(); }
        }

        private string _emailPassword;
        public string EmailPassword
        {
            get => _emailPassword;
            set { _emailPassword = value; OnPropertyChanged(); }
        }

        private string _emailTo;
        public string EmailTo
        {
            get => _emailTo;
            set { _emailTo = value; OnPropertyChanged(); }
        }

        private string _emailAlertSubject;
        public string EmailAlertSubject
        {
            get => _emailAlertSubject;
            set { _emailAlertSubject = value; OnPropertyChanged(); }
        }

        private string _emailAlertBody;
        public string EmailAlertBody
        {
            get => _emailAlertBody;
            set { _emailAlertBody = value; OnPropertyChanged(); }
        }

        private string _emailNotBoundSubject;
        public string EmailNotBoundSubject
        {
            get => _emailNotBoundSubject;
            set { _emailNotBoundSubject = value; OnPropertyChanged(); }
        }

        private string _emailNotBoundBody;
        public string EmailNotBoundBody
        {
            get => _emailNotBoundBody;
            set { _emailNotBoundBody = value; OnPropertyChanged(); }
        }

        #endregion

        #region 背景与图标

        private string _backgroundImagePath;
        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set { _backgroundImagePath = value; OnPropertyChanged(); }
        }

        private double _backgroundOpacity;
        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            set { _backgroundOpacity = value; OnPropertyChanged(); }
        }

        private string _customIconPath;
        public string CustomIconPath
        {
            get => _customIconPath;
            set { _customIconPath = value; OnPropertyChanged(); }
        }

        #endregion
    }
}
