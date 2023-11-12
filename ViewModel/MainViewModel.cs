using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cpu_net.Views.Pages;
using System.Windows.Controls;
using Prism.Mvvm;
using cpu_net.Model;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Security.Cryptography.Pkcs;
using System.Runtime.CompilerServices;

namespace cpu_net.ViewModel
{
    public class MainViewModel: ViewModelBase
    {
        public CarrierViewModel m1 { get; set; }
        public UserViewModel m2 { get; set; }

        public MainViewModel() 
        { 
            m1 = new CarrierViewModel();
            m2 = new UserViewModel();

        }
        private string? txtLog;

        public String? TxtLog
        {
            get { return txtLog; }
            set { txtLog = value; OnPropertyChanged(); }
        }

        public void Info(string message)
        {
             TxtLog = TxtLog + message + Environment.NewLine;
        }

        private RelayCommand loginButton_Click;
        public RelayCommand LoginButton_Click
        {
            get
            {
                if (loginButton_Click == null)
                    loginButton_Click = new RelayCommand(() => LoginOnline());
                return loginButton_Click;

            }
            set { loginButton_Click = value; }
        }

        private RelayCommand bindButton_Click;
        public RelayCommand BindButton_Click
        {
            get
            {
                if (bindButton_Click == null)
                    bindButton_Click = new RelayCommand(() => BindOnline());
                return bindButton_Click;

            }
            set { bindButton_Click = value; }
        }

        private void LoginOnline()
        {
            Info("Test Login");
        }

        private void BindOnline()
        {
            Info("Test Bind");
        }
    }

    public class LoginInfoViewModel: BindableBase
    {
        public LoginInfoViewModel(SettingModel settingModel)
        {
            _SettingModel = settingModel;
            SettingModel.PropertyChanged += (object sender, PropertyChangedEventArgs e) => settingModel.Save();
        }
        private SettingModel _SettingModel;

        public SettingModel SettingModel
        {
            get { return _SettingModel; }
            set { SetProperty(ref _SettingModel, value); }
        }
    }

    public class UserViewModel : ViewModelBase
    {
        SettingModel settingData = new SettingModel();

        public UserViewModel()
        {
            WeakReferenceMessenger.Default.Register<string>(this, Receive);
            IsAutoRun = false;
            IsAutoLogin = true;
            IsAutoMin = true;
            IsSetLogin = false;
            if (settingData.PathExist())
            {
                settingData.Read();
                Code = settingData.Username;
                Password = settingData.Password;
                IsAutoRun = settingData.IsAutoRun;
                IsAutoLogin = settingData.IsAutoLogin;
                IsAutoMin = settingData.IsAutoMin;
                IsSetLogin = settingData.IsSetLogin;
            }
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

        private Boolean isAutoRun;
        public Boolean IsAutoRun
        {
            get { return isAutoRun; }
            set { isAutoRun = value; settingData.IsAutoRun = isAutoRun; OnPropertyChanged(); }
        }

        private Boolean isAutoLogin;
        public Boolean IsAutoLogin
        {
            get { return isAutoLogin; }
            set { isAutoLogin = value; settingData.IsAutoLogin = isAutoLogin; OnPropertyChanged(); }
        }
        private Boolean isAutoMin;
        public Boolean IsAutoMin
        {
            get { return isAutoMin; }
            set { isAutoMin = value; settingData.IsAutoMin = isAutoMin; OnPropertyChanged(); }
        }
        private Boolean isSetLogin;
        public Boolean IsSetLogin
        {
            get { return isSetLogin; }
            set { isSetLogin = value; settingData.IsSetLogin = isSetLogin; OnPropertyChanged(); }
        }

        private RelayCommand saveButton_Click;
        public RelayCommand SaveButton_Click
        {
            get
            {
                if (saveButton_Click == null)
                    saveButton_Click = new RelayCommand(() => SaveAccount());
                return saveButton_Click;
            }
            set { saveButton_Click = value; }
        }
        CarriersModel carrier = new CarriersModel();
        string Text;
        public void Receive(object recipient,string message)
        {
            Text = message;
        }
        
        private void SaveAccount()
        {
            settingData.Username = Code;
            settingData.Password = Password;
            int Key=0;
            string carrier="";
            switch(Text)
            {
                case "请选择运营商": 
                    Key = 0;
                    carrier = "";
                    break;
                case "移动":
                    Key = 1;
                    carrier = "cmcc";
                    break;
                case "联通":
                    Key = 2;
                    carrier = "unicom";
                    break;
                case "电信":
                    Key = 3;
                    carrier = "telecom";
                    break;
            }
            settingData.Carrier = carrier;
            settingData.Key = Key;
            settingData.Save();
        }
    }

    public class CarrierViewModel : ViewModelBase 
    {
        SettingModel settingData = new SettingModel();
        public CarrierViewModel()
        {
            ComboxList = new ObservableCollection<CarriersModel>() {
          new CarriersModel() { Key = 0,Text = "请选择运营商" },
          new CarriersModel() { Key = 1,Text = "移动" },
          new CarriersModel() { Key = 2,Text = "联通" },
          new CarriersModel() { Key = 3,Text = "电信" },
        };
            if (settingData.PathExist())
            {
                settingData.Read();
                ComboxItem = ComboxList[settingData.Key];
            }
            else
            {
                ComboxItem = ComboxList[0];
            }

        }

        private CarriersModel comboxItem;
        /// <summary>
        /// 下拉框选中信息
        /// </summary>
        public CarriersModel ComboxItem
        {
            get { return comboxItem; }
            set { comboxItem = value; 
                WeakReferenceMessenger.Default.Send<string>(comboxItem.Text);
                OnPropertyChanged(); }
        }

        private ObservableCollection<CarriersModel> comboxList;
        /// <summary>
        /// 下拉框列表
        /// </summary>
        public ObservableCollection<CarriersModel> ComboxList
        {
            get { return comboxList; }
            set { comboxList = value; OnPropertyChanged(); }
        }
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
