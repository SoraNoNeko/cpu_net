using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace cpu_net.ViewModel
{
    public class AutoStart
    {
        /// <summary>
        /// 统一的注册表键名，任务管理器中显示为 cpu_net
        /// </summary>
        private const string AppName = "cpu_net";

        /// <summary>
        /// 历史上可能使用过的其他键名，用于清理旧启动项，防止新旧版本共存导致多开
        /// </summary>
        private static readonly string[] LegacyNames = new[] { "cpu_net.exe", "CPU_NET" };

        private string AppPath => Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

        /// <summary>
        /// 设置开机自动启动，同时清理历史遗留的启动项键名
        /// </summary>
        public void SetMeAutoStart(bool onOff = true)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (key == null) return;

                // 清理所有历史键名，避免新旧版本共存导致多个启动项
                foreach (var legacyName in LegacyNames)
                {
                    if (key.GetValue(legacyName) != null)
                    {
                        key.DeleteValue(legacyName);
                    }
                }

                if (onOff)
                {
                    key.SetValue(AppName, AppPath);
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置开机自启失败: {ex.Message}");
            }
        }
    }
}
