// ConfigurationViewModel.cs
using Prism.Commands;
using Prism.Mvvm;
using cpu_net.Services;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Windows;
using DryIoc;
using Prism.Navigation.Regions;
using System.Collections.Generic;
using System.Windows.Controls;
using System;

namespace cpu_net.ViewModel
{
    public class ConfigurationViewModel : BindableBase
    {
        private readonly ISettingService _settingService;
        private readonly IRegionManager _regionManager;

        public ConfigurationViewModel(
            ISettingService settingService,
            IRegionManager regionManager)
        {
            _settingService = settingService;
            _regionManager = regionManager;

            // 初始化命令
            SaveCommand = new DelegateCommand(SaveSettings);
            CancelCommand = new DelegateCommand(Cancel);
            OpenGitHubCommand = new DelegateCommand(OpenGitHub);

            // 从服务加载设置
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = _settingService.Settings;
            Code = settings.Username;
            Password = settings.Password;
            SelectedCarrier = settings.Carrier;
            IsAutoRun = settings.IsAutoRun;
            IsAutoLogin = settings.IsAutoLogin;
            IsAutoMin = settings.IsAutoMin;
            IsSetLogin = settings.IsSetLogin;
            Mode = settings.Mode;
            LoginTime = settings.LoginTime;
        }

        // 属性定义
        private string _code;
        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _isAutoRun;
        public bool IsAutoRun
        {
            get => _isAutoRun;
            set => SetProperty(ref _isAutoRun, value);
        }

        private bool _isAutoLogin;
        public bool IsAutoLogin
        {
            get => _isAutoLogin;
            set => SetProperty(ref _isAutoLogin, value);
        }

        private bool _isAutoMin;
        public bool IsAutoMin
        {
            get => _isAutoMin;
            set => SetProperty(ref _isAutoMin, value);
        }

        private bool _isSetLogin;
        public bool IsSetLogin
        {
            get => _isSetLogin;
            set => SetProperty(ref _isSetLogin, value);
        }

        private int _mode;
        public int Mode
        {
            get => _mode; 
            set => SetProperty(ref _mode, value);
        }

        private int _loginTime;
        public int LoginTime
        {
            get => _loginTime;
            set => SetProperty(ref _loginTime, value);
        }

        // 运营商列表
        public List<CarrierItem> CarrierList { get; } = new List<CarrierItem>
        {
            new CarrierItem { Key = "cmcc", Text = "移动" },
            new CarrierItem { Key = "unicom", Text = "联通" },
            new CarrierItem { Key = "telecom", Text = "电信" }
        };

        private string _selectedCarrier;
        public string SelectedCarrier
        {
            get => _selectedCarrier;
            set => SetProperty(ref _selectedCarrier, value);
        }

        // 命令定义
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenGitHubCommand { get; }
        public ICommand PppCommand { get; }
        public ICommand CpuCommand { get; }
        public ICommand AutoCommand { get; }

        private void SaveSettings()
        {
            // 保存设置到服务
            _settingService.Settings.Username = Code;
            _settingService.Settings.Password = Password;
            _settingService.Settings.Carrier = SelectedCarrier;
            _settingService.Settings.IsAutoRun = IsAutoRun;
            _settingService.Settings.IsAutoLogin = IsAutoLogin;
            _settingService.Settings.IsAutoMin = IsAutoMin;
            _settingService.Settings.IsSetLogin = IsSetLogin;
            _settingService.Settings.Mode = Mode;
            _settingService.Settings.LoginTime = LoginTime;

            _settingService.SaveSettings();

            // 导航回首页
            _regionManager.RequestNavigate("ContentRegion", "HomePage");
        }

        private void Cancel()
        {
            // 导航回首页
            _regionManager.RequestNavigate("ContentRegion", "HomePage");
        }

        private void OpenGitHub()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/SoraNoNeko/cpu_net/",
                UseShellExecute = true
            });
        }
    }

    public class CarrierItem
    {
        public string Key { get; set; }
        public string Text { get; set; }
    }
    //绑定PasswordBox
    public static class LoginPasswordBoxHelper
    {
        public static string GetPassword(DependencyObject obj)
        {
            return (string)obj.GetValue(PasswordProperty);
        }

        public static void SetPassword(DependencyObject obj, string value)
        {
            obj.SetValue(PasswordProperty, value);
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), typeof(LoginPasswordBoxHelper), new PropertyMetadata(""));
        public static bool GetIsPasswordBindingEnable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsPasswordBindingEnableProperty);
        }

        public static void SetIsPasswordBindingEnable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsPasswordBindingEnableProperty, value);
        }

        public static readonly DependencyProperty IsPasswordBindingEnableProperty =
            DependencyProperty.RegisterAttached("IsPasswordBindingEnable", typeof(bool), typeof(LoginPasswordBoxHelper),
                                                new FrameworkPropertyMetadata(OnIsPasswordBindingEnabledChanged));

        private static void OnIsPasswordBindingEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = obj as PasswordBox;
            if (passwordBox != null)
            {
                passwordBox.PasswordChanged -= PasswordBoxPasswordChanged;
                if ((bool)e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
                }
            }
        }

        static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = (PasswordBox)sender;
            if (!String.Equals(GetPassword(passwordBox), passwordBox.Password))
            {
                SetPassword(passwordBox, passwordBox.Password);
            }
        }

    }
}