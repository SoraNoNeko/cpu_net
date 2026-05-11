using Prism.Mvvm;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cpu_net.Model
{
    public class SettingModel : BindableBase
    {
        private const string DefaultTestUrl = "http://www.msftconnecttest.com/connecttest.txt";
        private const string DefaultTestCode = "Microsoft Connect Test";

        private readonly string _settingDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");

        private string _password;
        private string _userName;
        private string _carrier;
        private int _key;
        private int _mode;
        private bool? _isAutoRun;
        private bool? _isAutoLogin;
        private bool? _isAutoMin;
        private bool? _isSetLogin;
        private int? _loginTime;
        private bool? _testMode;
        private string? _testUrl;
        private string? _testCode;

        public bool PathExist() => File.Exists(_settingDataPath);

        public string Username
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Carrier
        {
            get => _carrier;
            set => SetProperty(ref _carrier, value);
        }

        public int Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public int Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public bool IsAutoRun
        {
            get => _isAutoRun ?? false;
            set => SetProperty(ref _isAutoRun, value);
        }

        public bool IsAutoLogin
        {
            get => _isAutoLogin ?? false;
            set => SetProperty(ref _isAutoLogin, value);
        }

        public bool IsAutoMin
        {
            get => _isAutoMin ?? false;
            set => SetProperty(ref _isAutoMin, value);
        }

        public bool IsSetLogin
        {
            get => _isSetLogin ?? false;
            set => SetProperty(ref _isSetLogin, value);
        }

        public int LoginTime
        {
            get => _loginTime ?? 5;
            set => SetProperty(ref _loginTime, value);
        }

        public bool TestMode
        {
            get => _testMode ?? false;
            set => SetProperty(ref _testMode, value);
        }

        public string TestUrl
        {
            get => _testUrl ?? DefaultTestUrl;
            set => SetProperty(ref _testUrl, value);
        }

        public string TestCode
        {
            get => _testCode ?? DefaultTestCode;
            set => SetProperty(ref _testCode, value);
        }

        public SettingModel Read()
        {
            if (!File.Exists(_settingDataPath))
            {
                return new SettingModel();
            }

            string yamlStr = File.ReadAllText(_settingDataPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            try
            {
                return deserializer.Deserialize<SettingModel>(yamlStr);
            }
            catch (YamlDotNet.Core.YamlException)
            {
                return new SettingModel();
            }
        }

        public void Save()
        {
            var userSettingData = new SettingModel
            {
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

            // 若配置已存在，保留原有的测试模式相关配置（覆盖当前内存中的值）
            if (userSettingData.PathExist())
            {
                SettingModel data = userSettingData.Read();
                userSettingData.TestMode = data.TestMode;
                userSettingData.TestUrl = data.TestUrl;
                userSettingData.TestCode = data.TestCode;
            }

            var serializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            string yamlStr = serializer.Serialize(userSettingData);
            File.WriteAllText(_settingDataPath, yamlStr);
        }
    }
}
