using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using cpu_net.Model;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;

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
             TxtLog = TxtLog + $"{DateTime.Now.ToString("HH:mm:dd")}  " + message + Environment.NewLine;
        }
        private RelayCommand noticeButton_Click;
        public RelayCommand NoticeButton_Click
        {
            get
            {
                if (noticeButton_Click == null)
                    noticeButton_Click = new RelayCommand(() => NoticeOnline());
                return noticeButton_Click;

            }
            set { loginButton_Click = value; }
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

        public class _LRes
        {
            public int result { get; set; }
        }

        public class LRes
        {
            public string msga { get; set; }
        }
        public void LoginOnline()
        {
            SettingModel settingData=new SettingModel();
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
                string Login_url = $"http://172.17.253.3/drcom/login?callback=dr1003&DDDDD={settingData.Username}%40{settingData.Carrier}" +
                    $"&upass={settingData.Password}&0MKKey=123456&R1=0&R2=&R3=0&R6=0&para=00&v6ip=&terminal_type=1&lang=zh-cn&jsVersion=4.1.3&v=7011&lang=zh";
                try
                {               
                    //var _res = HttpRequestHelper.HttpGetRequest(Login_url).Replace(" ","");
                    var _res = HttpRequestHelper.HttpGetRequest(Login_url).Replace("dr1003", "").Replace(" ", "");
                    //System.Diagnostics.Debug.WriteLine(_res);
                    var res = _res.Substring(1,_res.Length-2);
                    System.Diagnostics.Debug.WriteLine(res);
                    if(res == null)
                    {
                        Info("网络错误");
                        return;
                    }
                    var _obj = JsonSerializer.Deserialize<_LRes>(res)!;
                    if (_obj != null) 
                    {
                        switch (_obj.result)
                        {
                            case 1:
                                Info("登录成功");
                                break;
                            case 0:
                                Info("登录失败");
                                var obj = JsonSerializer.Deserialize<LRes>(res)!;
                                switch (obj.msga)
                                {
                                    default:
                                        Info($"Error Message: {obj.msga}");
                                        break;
                                    case "ldap auth error":
                                        Info("密码错误");
                                        break;
                                    case "unbind isp uid":
                                        Info("未绑定宽带账号");
                                        break;
                                }
                                break;
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Info("登录失败");
                    Info(e.Message);
                }
                catch(JsonException e)
                {
                    Info("网络连接失败");
                }
            }
            else
            {
                Info("No Config Found");
                MessageBox.Show("请在设置中添加账号信息", "提示");
            }
            
        }
        private void NoticeOnline()
        {
            System.Diagnostics.Process.Start("explorer.exe", "https://lic.cpu.edu.cn/ee/c6/c7550a192198/page.htm");
        }
        private void BindOnline()
        {
            System.Diagnostics.Process.Start("explorer.exe", "http://192.168.199.70:8080/Self/Dashboard");
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
        AutoStart autoStart = new AutoStart();
        public UserViewModel()
        {
            WeakReferenceMessenger.Default.Register<string>(this, Receive);
            if (settingData.PathExist())
            {
                settingData = settingData.Read();
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
            set { isAutoRun = value; settingData.IsAutoRun = isAutoRun;
                autoStart.SetAutoStart(isAutoRun);
                OnPropertyChanged(); }
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
            int Key = 0;
            string carrier = "";
            switch (Text)
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
            if (String.IsNullOrEmpty(Code) | String.IsNullOrEmpty(Password))
            {
                MessageBox.Show("请输入学号和密码","Attention");
            }else if (Key == 0)
            {
                MessageBox.Show("请选择运营商", "Attention");
            }
            else
            {
                settingData.Username = Code;
                settingData.Password = Password;
                settingData.Carrier = carrier;
                settingData.Key = Key;
                settingData.Save();
                MessageBox.Show("保存成功", "Info");
            }
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
                settingData = settingData.Read();
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

    public static class HttpRequestHelper
    {
        /// <summary>
        /// Http Get Request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string HttpGetRequest(string url)
        {
            try
            {
                string strGetResponse = string.Empty;
                var getRequest = CreateHttpRequest(url, "GET");
                var getResponse = getRequest.GetResponse() as HttpWebResponse;
                strGetResponse = GetHttpResponse(getResponse, "GET");
                return strGetResponse;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Http Post Request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postJsonData"></param>
        /// <returns></returns>
        public static string HttpPostRequest(string url, string postJsonData)
        {
            string strPostReponse = string.Empty;
            try
            {
                var postRequest = CreateHttpRequest(url, "POST", postJsonData);
                var postResponse = postRequest.GetResponse() as HttpWebResponse;
                strPostReponse = GetHttpResponse(postResponse, "POST");
            }
            catch (Exception ex)
            {
                strPostReponse = ex.Message;
            }
            return strPostReponse;
        }


        private static HttpWebRequest CreateHttpRequest(string url, string requestType, params object[] strJson)
        {
            HttpWebRequest request = null;
            const string get = "GET";
            const string post = "POST";
            if (string.Equals(requestType, get, StringComparison.OrdinalIgnoreCase))
            {
                request = CreateGetHttpWebRequest(url);
            }
            if (string.Equals(requestType, post, StringComparison.OrdinalIgnoreCase))
            {
                request = CreatePostHttpWebRequest(url, strJson[0].ToString());
            }
            return request;
        }

        private static HttpWebRequest CreateGetHttpWebRequest(string url)
        {
            var getRequest = HttpWebRequest.Create(url) as HttpWebRequest;
            getRequest.Method = "GET";
            getRequest.Timeout = 5000;
            getRequest.ContentType = "text/html;charset=UTF-8";
            getRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return getRequest;
        }

        private static HttpWebRequest CreatePostHttpWebRequest(string url, string postData)
        {
            var postRequest = HttpWebRequest.Create(url) as HttpWebRequest;
            postRequest.KeepAlive = false;
            postRequest.Timeout = 5000;
            postRequest.Method = "POST";
            postRequest.ContentType = "application/x-www-form-urlencoded";
            postRequest.ContentLength = postData.Length;
            postRequest.AllowWriteStreamBuffering = false;
            StreamWriter writer = new StreamWriter(postRequest.GetRequestStream(), Encoding.ASCII);
            writer.Write(postData);
            writer.Flush();
            return postRequest;
        }

        private static string GetHttpResponse(HttpWebResponse response, string requestType)
        {
            var responseResult = "";
            const string post = "POST";
            string encoding = "UTF-8";
            if (string.Equals(requestType, post, StringComparison.OrdinalIgnoreCase))
            {
                encoding = response.ContentEncoding;
                if (encoding == null || encoding.Length < 1)
                {
                    encoding = "UTF-8";
                }
            }
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
            {
                responseResult = reader.ReadToEnd();
            }
            return responseResult;
        }

        private static string GetHttpResponseAsync(HttpWebResponse response, string requestType)
        {
            var responseResult = "";
            const string post = "POST";
            string encoding = "UTF-8";
            if (string.Equals(requestType, post, StringComparison.OrdinalIgnoreCase))
            {
                encoding = response.ContentEncoding;
                if (encoding == null || encoding.Length < 1)
                {
                    encoding = "UTF-8";
                }
            }
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
            {
                responseResult = reader.ReadToEnd();
            }
            return responseResult;
        }
    }
}
