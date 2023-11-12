using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpu_net.Model
{
    public class UserModel : ObservableObject
    {
        private string _code;
        public string Code
        {
            get { return _code; }
            set { _code = value; OnPropertyChanged(nameof(Code)); }
        }

        private string _secret;
        public string Secret
        {
            get { return _secret; }
            set { _secret = value; OnPropertyChanged(nameof(Secret)); }
        }

        private Boolean _isAutoRun;
        public Boolean IsAutoRun
        {
            get { return _isAutoRun; }
            set { _isAutoRun = value; OnPropertyChanged(); }
        }

        private Boolean _isAutoLogin;
        public Boolean IsAutoLogin
        {
            get { return _isAutoLogin; }
            set { _isAutoLogin = value; OnPropertyChanged(); }
        }
        private Boolean _isAutoMin;
        public Boolean IsAutoMin
        {
            get { return _isAutoMin; }
            set { _isAutoMin = value; OnPropertyChanged(); }
        }
        private Boolean _isSetLogin;
        public Boolean IsSetLogin
        {
            get { return _isSetLogin; }
            set { _isSetLogin = value; OnPropertyChanged(); }
        }
    }
}
