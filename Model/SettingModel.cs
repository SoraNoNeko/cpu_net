using Prism.Mvvm;
using System;
using System.IO;

namespace cpu_net.Model
{
    public class SettingModel : BindableBase
    {
        string DefaultTestUrl = "http://www.msftconnecttest.com/connecttest.txt";
        string DefaultTestCode = "Microsoft Connect Test";
        public SettingModel()
        {
        }

        //配置文件路径
        private readonly string _SettingDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");
        private string? _Password;
        private string? _UserName;
        private string? _Carrier;
        private int? _Key;
        private int? _Mode;
        private bool? _IsAutoRun;
        private bool? _IsAutoLogin;
        private bool? _IsAutoMin;
        private bool? _IsSetLogin;
        private int? _LoginTime;
        private bool? _TestMode;
        private string? _TestUrl;
        private string? _TestCode;
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
            get { return _UserName ?? ""; }
            set { SetProperty(ref _UserName, value); }
        }

        /// <summary>
        /// 默认密码
        /// </summary>
        public string Password
        {
            get { return _Password ?? ""; }
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
            get { return _Carrier ?? ""; }
            set
            {
                SetProperty(ref _Carrier, value);
            }
        }

        public int Key
        {
            get { return _Key ?? 0; }
            set
            {
                SetProperty(ref _Key, value);
            }
        }

        public int Mode
        {
            get { return _Mode ?? 0; }
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
            get { return _IsAutoRun ?? false; }
            set
            {
                SetProperty(ref _IsAutoRun, value);
            }
        }
        public bool IsAutoLogin
        {
            get { return _IsAutoLogin ?? false; }
            set
            {
                SetProperty(ref _IsAutoLogin, value);
            }
        }
        public bool IsAutoMin
        {
            get { return _IsAutoMin ?? false; }
            set
            {
                SetProperty(ref _IsAutoMin, value);
            }
        }
        public bool IsSetLogin
        {
            get { return _IsSetLogin ?? false; }
            set
            {
                SetProperty(ref _IsSetLogin, value);
            }
        }
        public int LoginTime
        {
            get {
                return _LoginTime ?? 5; 
            }
            set
            {
                SetProperty(ref _LoginTime, value);
            }
        }
        public bool TestMode
        {
            get
            {
                return _TestMode ?? false;
            }
            set
            {
                SetProperty(ref _TestMode, value);
            }
        }
        public string TestUrl
        {
            get
            {
                return _TestUrl ?? DefaultTestUrl;
            }
            set
            {
                SetProperty(ref _TestUrl, value);
            }
        }
        public string TestCode
        {
            get
            {
                return _TestCode ?? DefaultTestCode;
            }
            set
            {
                SetProperty(ref _TestCode, value);
            }
        }

        /*public SettingModel Read()
        {
            // Check if the file exists before attempting to read it
            if (!File.Exists(_SettingDataPath))
            {
                // If the file doesn't exist, return a new default SettingModel instance
                return new SettingModel();
            }
            string yamlStr = File.ReadAllText(_SettingDataPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance) // 根据你的 YAML 文件命名约定选择，例如 PascalCaseNamingConvention 或 NullNamingConvention
                .Build();

            // 尝试反序列化，如果文件为空或格式错误，则返回新的 SettingModel 实例
            try
            {
                return deserializer.Deserialize<SettingModel>(yamlStr);
            }
            catch (YamlDotNet.Core.YamlException)
            {
                // 处理文件可能为空或无效 YAML 的情况
                return new SettingModel();
            }
        }

        public void Save()
        {
            SettingModel userSettingData = new SettingModel
            {
                // ServerAddress = ServerAddress, // 如果你希望保存 ServerAddress，需要在这里取消注释并确保有对应的属性
                Username = Username,
                Carrier = Carrier,
                Key = Key,
                Mode = Mode,
                Password = Password,
                IsAutoRun = IsAutoRun,
                IsAutoLogin = IsAutoLogin,
                IsAutoMin = IsAutoMin,
                IsSetLogin = IsSetLogin,
                LoginTime = LoginTime,
                TestMode = TestMode,
                TestCode = TestCode,
                TestUrl = TestUrl,
            };
            if (userSettingData.PathExist())
            {
                SettingModel data = userSettingData.Read();
                userSettingData.TestMode = data.TestMode;
                userSettingData.TestUrl = data.TestUrl;
                userSettingData.TestCode = data.TestCode;
            }

            var serializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance) // 保持命名约定一致
                .Build();

            string yamlStr = serializer.Serialize(userSettingData);
            File.WriteAllText(_SettingDataPath, yamlStr);
        }*/
    }
}

