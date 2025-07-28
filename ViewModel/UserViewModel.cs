using CommunityToolkit.Mvvm.Input;
using cpu_net.Services;
using cpu_net.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpu_net.ViewModel
{
    public class UserViewModel : ViewModelBase
    {
        private readonly ISettingService _settingService;
        AutoStart autoStart = new AutoStart();
        public UserViewModel(ISettingService settingService)
        {
            _settingService = settingService;
            var settings = _settingService.Settings;
            Code = settings.Username;
            Password = settings.Password;
            IsAutoRun = settings.IsAutoRun;
            IsAutoLogin = settings.IsAutoLogin;
            IsAutoMin = settings.IsAutoMin;
            IsSetLogin = settings.IsSetLogin;
            Mode = settings.Mode;
        }

        private string code;
        public string Code
        {
            get { return code; }
            set { code = value; OnPropertyChanged(); }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { password = value; OnPropertyChanged(); }
        }

        private bool isAutoRun;
        public bool IsAutoRun
        {
            get { return isAutoRun; }
            set
            {
                isAutoRun = value; _settingService.Settings.IsAutoRun = isAutoRun;
                autoStart.SetMeAutoStart(isAutoRun);
                OnPropertyChanged();
            }
        }

        private bool isAutoLogin;
        public bool IsAutoLogin
        {
            get { return isAutoLogin; }
            set { isAutoLogin = value; _settingService.Settings.IsAutoLogin = isAutoLogin; OnPropertyChanged(); }
        }

        private bool isAutoMin;
        public bool IsAutoMin
        {
            get { return isAutoMin; }
            set { isAutoMin = value; _settingService.Settings.IsAutoMin = isAutoMin; OnPropertyChanged(); }
        }

        private bool isSetLogin;
        public bool IsSetLogin
        {
            get { return isSetLogin; }
            set { isSetLogin = value; _settingService.Settings.IsSetLogin = isSetLogin; OnPropertyChanged(); }
        }

        private int mode;
        public int Mode
        {
            get { return mode; }
            set { mode = value; _settingService.Settings.Mode = mode; OnPropertyChanged(); }
        }

        private RelayCommand pppButton_Click;
        public RelayCommand PppButton_Click
        {
            get
            {
                if (pppButton_Click == null)
                    pppButton_Click = new RelayCommand(() => PppMode());
                return pppButton_Click;

            }
            set { pppButton_Click = value; }
        }

        private RelayCommand cpuButton_Click;
        public RelayCommand CpuButton_Click
        {
            get
            {
                if (cpuButton_Click == null)
                    cpuButton_Click = new RelayCommand(() => CpuMode());
                return cpuButton_Click;

            }
            set { cpuButton_Click = value; }
        }

        private RelayCommand autoButton_Click;
        public RelayCommand AutoButton_Click
        {
            get
            {
                if (autoButton_Click == null)
                    autoButton_Click = new RelayCommand(() => AutoMode());
                return autoButton_Click;

            }
            set { autoButton_Click = value; }
        }

        private void PppMode()
        {
            Mode = 0;
        }

        private void CpuMode()
        {
            Mode = 1;
        }

        private void AutoMode()
        {
            Mode = 2;
        }
    }
}
