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
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static System.Net.WebRequestMethods;

namespace cpu_net.ViewModel
{
    public class MainViewModel: ViewModelBase
    {

        public MainViewModel() 
        { 
        }

        private static readonly ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();
        public static void TextLog(string log)
        {
            var now = DateTime.Now;
            var logpath = @"" + now.Year + "" + now.Month + "" + now.Day + ".log";
            var _log = $"{DateTime.Now.ToString("HH:mm")}  "+log + "\r\n";
            try
            {
                //设置读写锁为写入模式独占资源，其他写入请求需要等待本次写入结束之后才能继续写入
                LogWriteLock.EnterWriteLock();
                System.IO.File.AppendAllText(logpath, _log);
            }
            finally
            {
                //退出写入模式，释放资源占用
                LogWriteLock.ExitWriteLock();
            }
        }

        public static string ReadLog()
        {
            var now = DateTime.Now;
            string fLog = "";
            var logpath = @"" + now.Year + "" + now.Month + "" + now.Day + ".log";
            if (System.IO.File.Exists(logpath))
            {

                try
                {
                    LogWriteLock.EnterReadLock();
                    fLog = System.IO.File.ReadAllText(logpath);
                }
                finally
                {
                    LogWriteLock.ExitReadLock();
                }
            }
            else
            {
                return "";
            }
            return fLog;
        }

        public String TxtLog
        {
            get { return ReadLog(); }
            set { TextLog(value); OnPropertyChanged(); }
        }

        public void Info(string message)
        {
            TxtLog = message;
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
        
        public string GetIP()
        {
            string localIP = string.Empty;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            Info($"当前IP为{localIP}");
            return localIP;
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
                var _IP = GetIP();
                settingData = settingData.Read();
                int _mode = 0;
                switch (settingData.Mode)
                {
                    case 0:
                        _mode = settingData.Mode;
                        break;
                    case 1:
                        _mode = settingData.Mode;
                        break;
                    case 2:
                        string[] _ip = _IP.Split('.');
                        if (_ip[0] == "10" & _ip[1] == "7")
                        {
                            _mode = 1;
                            Info("自动识别为CPU环境");
                        }
                        else if(_ip[0] == "10" & _ip[1] == "4")
                        {
                            _mode = 1;
                            Info("自动识别为CPU环境");
                        }
                        else
                        {
                            _mode = 0;
                            Info("自动识别为宽带环境");
                        }
                        break;
                }
                string Login_url;
                switch (_mode) {
                    case 0:
                        Login_url = $"http://172.17.253.3/drcom/login?callback=dr1003&DDDDD={settingData.Username}%40{settingData.Carrier}" +
                    $"&upass={settingData.Password}&0MKKey=123456&R1=0&R2=&R3=0&R6=0&para=00&v6ip=&terminal_type=1&lang=zh-cn&jsVersion=4.1.3&v=7011&lang=zh";
                        break;
                    case 1:
                        IPHostEntry ipEntry;
                        string local_ip;
                        try
                        {
                            ipEntry = Dns.GetHostEntry("192.168.199.54");
                            local_ip = ipEntry.ToString();
                        }
                        catch
                        {
                            try
                            {
                                ipEntry = Dns.GetHostEntry("172.17.253.5");
                                local_ip = ipEntry.ToString();
                            }
                            catch
                            {
                                local_ip = _IP;
                            }
                        }
                        Info(local_ip);
                        Login_url = $"http://192.168.199.21:801/eportal/?c=Portal&a=login&callback=dr1004&login_method=1&user_account=%2C1%2C{settingData.Username}&user_password={settingData.Password}" +
                            $"&wlan_user_ip={local_ip}&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=&wlan_ac_name=&jsVersion=3.3.3&v=1954";
                        break;
                    default:
                        Login_url = $"http://172.17.253.3/drcom/login?callback=dr1003&DDDDD={settingData.Username}%40{settingData.Carrier}" +
                   $"&upass={settingData.Password}&0MKKey=123456&R1=0&R2=&R3=0&R6=0&para=00&v6ip=&terminal_type=1&lang=zh-cn&jsVersion=4.1.3&v=7011&lang=zh";
                        break;
                }
                try
                {
                    //var _res = HttpRequestHelper.HttpGetRequest(Login_url).Replace(" ","");
                    string _res;
                    switch (_mode)
                    {
                        case 0:
                            _res = HttpRequestHelper.HttpGetRequest(Login_url).Replace("dr1003", "").Replace(" ", "");
                            break;
                        case 1:
                            _res = HttpRequestHelper.HttpGetRequest(Login_url).Replace("dr1004", "").Replace(" ", "");
                            break;
                        default:
                            _res = HttpRequestHelper.HttpGetRequest(Login_url).Replace("dr1003", "").Replace(" ", "");
                            break;
                    }

                    //System.Diagnostics.Debug.WriteLine(_res);
                    var res = _res.Substring(1,_res.Length-2);
                    //System.Diagnostics.Debug.WriteLine(res);
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
                    Info("JSON解析失败");
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
                Mode = settingData.Mode;
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

        private int mode;
        public int Mode
        {
            get { return mode; }
            set { mode = value; settingData.Mode = mode; OnPropertyChanged(); }
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
        /*
        private Boolean pppChecked;
        public Boolean PppChecked
        {
            get { return Mode == 0?true:false; }
            set { pppChecked = value; cpuChecked = false; autoChecked = false; Mode = 0; settingData.Mode = Mode; OnPropertyChanged(); }
        }

        private Boolean cpuChecked;
        public Boolean CpuChecked
        {
            get { return Mode == 1 ? true : false; }
            set { cpuChecked = value; autoChecked = false; pppChecked = false; Mode = 1; settingData.Mode = Mode; OnPropertyChanged(); }
        }

        private Boolean autoChecked;
        public Boolean AutoChecked
        {
            get { return Mode == 2 ? true : false; }
            set { autoChecked = value; pppChecked = false; cpuChecked = false; Mode = 2; settingData.Mode = Mode; OnPropertyChanged(); }
        }
        */
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
