using System;
using System.IO;
using cpu_net.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cpu_net.Services
{
    public sealed class SettingsStore
    {
        private static readonly object Gate = new object();
        private readonly string _path;

        public SettingsStore(string? path = null)
        {
            _path = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");
        }

        public SettingModel Read()
        {
            lock (Gate)
            {
                try { return ReadCore(); }
                catch (YamlDotNet.Core.YamlException) { return new SettingModel(); }
            }
        }

        private SettingModel ReadCore()
        {
            if (!File.Exists(_path)) return new SettingModel();
            return new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build().Deserialize<SettingModel>(File.ReadAllText(_path)) ?? new SettingModel();
        }

        // 每次保存均以磁盘中的最新配置为基础，只应用当前模块的修改。
        public bool Update(Func<SettingModel, bool> update)
        {
            lock (Gate)
            {
                var settings = ReadCore();
                if (!update(settings)) return false;
                WriteCore(settings);
                return true;
            }
        }

        public void Save(SettingModel settings)
        {
            lock (Gate) WriteCore(settings);
        }

        private void WriteCore(SettingModel settings)
        {
            string yaml = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build().Serialize(settings);
            string temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, yaml);
                File.Move(temp, _path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
