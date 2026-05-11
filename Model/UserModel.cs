using CommunityToolkit.Mvvm.ComponentModel;

namespace cpu_net.Model
{
    public class UserModel : ObservableObject
    {
        private string _code;
        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }

        private string _secret;
        public string Secret
        {
            get => _secret;
            set { _secret = value; OnPropertyChanged(); }
        }

        private bool _isAutoRun;
        public bool IsAutoRun
        {
            get => _isAutoRun;
            set { _isAutoRun = value; OnPropertyChanged(); }
        }

        private bool _isAutoLogin;
        public bool IsAutoLogin
        {
            get => _isAutoLogin;
            set { _isAutoLogin = value; OnPropertyChanged(); }
        }

        private bool _isAutoMin;
        public bool IsAutoMin
        {
            get => _isAutoMin;
            set { _isAutoMin = value; OnPropertyChanged(); }
        }

        private bool _isSetLogin;
        public bool IsSetLogin
        {
            get => _isSetLogin;
            set { _isSetLogin = value; OnPropertyChanged(); }
        }

        private int _loginTime;
        public int LoginTime
        {
            get => _loginTime;
            set { _loginTime = value; OnPropertyChanged(); }
        }
    }
}
