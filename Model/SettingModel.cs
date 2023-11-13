using Prism.Mvvm;
using System;
using System.IO;
using System.Text.Json;

namespace cpu_net.Model
{
    public class SettingModel : BindableBase
    {
        public SettingModel()
        {
        }

        //配置文件路径
        private readonly string _SettingDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
        private string _Password;
        private string _UserName;
        private string _Carrier;
        private int _Key;
        private int _Mode = 0;
        private bool _IsAutoRun;
        private bool _IsAutoLogin;
        private bool _IsAutoMin;
        private bool _IsSetLogin;
        ///private string _ServerAddress;

        //SettingDataPath exist
        public bool PathExist()
        {
            if (File.Exists(_SettingDataPath))
            {
                return true;
            }
            else return false;
        }

        /// <summary>
        /// 服务器API地址
        /// </summary>
        ///public string ServerAddress
        ///{
        ///    get { return _ServerAddress; }
        ///    set { SetProperty(ref _ServerAddress, value); }
        ///}

        /// <summary>
        /// 默认登录用户名
        /// </summary>
        public string Username
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        /// <summary>
        /// 默认密码
        /// </summary>
        public string Password
        {
            get { return _Password; }
            set
            {
                SetProperty(ref _Password, value);
            }
        }

        /// <summary>
        /// 运营商
        /// </summary>
        public string Carrier
        {
            get { return _Carrier; }
            set
            {
                SetProperty(ref _Carrier, value);
            }
        }

        public int Key
        {
            get { return _Key; }
            set
            {
                SetProperty(ref _Key, value);
            }
        }

        public int Mode
        {
            get { return _Mode; }
            set
            {
                SetProperty(ref _Mode, value);
            }
        }
        /// <summary>
        /// 是否自动登录
        /// </summary>
        public bool IsAutoRun
        {
            get { return _IsAutoRun; }
            set
            {
                SetProperty(ref _IsAutoRun, value);
            }
        }
        public bool IsAutoLogin
        {
            get { return _IsAutoLogin; }
            set
            {
                SetProperty(ref _IsAutoLogin, value);
            }
        }
        public bool IsAutoMin
        {
            get { return _IsAutoMin; }
            set
            {
                SetProperty(ref _IsAutoMin, value);
            }
        }
        public bool IsSetLogin
        {
            get { return _IsSetLogin; }
            set
            {
                SetProperty(ref _IsSetLogin, value);
            }
        }

        public SettingModel Read()
        {
            string jsonStr = File.ReadAllText(_SettingDataPath);
            SettingModel? userSettingData = JsonSerializer.Deserialize<SettingModel>(jsonStr);
            return userSettingData;
        }

        public void Save()
        {
            SettingModel userSettingData = new SettingModel();
            ///userSettingData.ServerAddress = ServerAddress;
            userSettingData.Username = Username;
            userSettingData.Carrier = Carrier;
            userSettingData.Key = Key;
            userSettingData.Mode = Mode;
            userSettingData.Password = Password;
            userSettingData.IsAutoRun = IsAutoRun;
            userSettingData.IsAutoLogin = IsAutoLogin;
            userSettingData.IsAutoMin = IsAutoMin;
            userSettingData.IsSetLogin = IsSetLogin;
            string jsonStr = JsonSerializer.Serialize<SettingModel>(userSettingData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_SettingDataPath, jsonStr);
        }
    }
}

