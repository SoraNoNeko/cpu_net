using cpu_net.Model;
using cpu_net.ViewModel.Base;
using System;

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
                Mode = _settingData.Mode;
                LoginTime = _settingData.LoginTime;
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
    }
}
