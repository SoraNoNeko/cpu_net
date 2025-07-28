using cpu_net.Model;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cpu_net.Services
{
    public class SettingService : ISettingService
    {
        private readonly string _settingDataPath;
        private SettingModel _settings;

        public SettingService()
        {
            _settingDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");
            _settings = LoadSettings();
        }

        public SettingModel Settings
        {
            get
            {
                // 确保设置不为空
                if (_settings == null)
                {
                    _settings = LoadSettings();
                }
                return _settings;
            }
            set => _settings = value;
        }

        public void Initialize()
        {
            _settings = LoadSettings();
        }

        public void SaveSettings()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            string yamlStr = serializer.Serialize(_settings);
            File.WriteAllText(_settingDataPath, yamlStr);
        }

        public void ReloadSettings()
        {
            _settings = LoadSettings();
        }

        private SettingModel LoadSettings()
        {
            if (!File.Exists(_settingDataPath))
                return new SettingModel();

            string yamlStr = File.ReadAllText(_settingDataPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            try
            {
                return deserializer.Deserialize<SettingModel>(yamlStr) ?? new SettingModel();
            }
            catch
            {
                return new SettingModel();
            }
        }
    }
}