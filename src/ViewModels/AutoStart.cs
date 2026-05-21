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
        /// 历史上可能使用过的注册表键名
        /// </summary>
        private static readonly string[] LegacyRegistryNames = new[] { "cpu_net.exe", "CPU_NET" };

        /// <summary>
        /// 历史上可能使用过的启动文件夹快捷方式名称（不含 .lnk 后缀）
        /// </summary>
        private static readonly string[] LegacyShortcutNames = new[] { "cpu_net", "cpu_net.exe", "CPU_NET" };

        private string AppPath => Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

        /// <summary>
        /// 设置开机自动启动，同时清理注册表和启动文件夹中的历史遗留启动项
        /// </summary>
        public void SetMeAutoStart(bool onOff = true)
        {
            try
            {
                // 1. 清理注册表历史键名
                using var key = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (key != null)
                {
                    foreach (var legacyName in LegacyRegistryNames)
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

                // 2. 清理启动文件夹中的旧快捷方式（旧版本可能在此创建 .lnk）
                CleanLegacyStartupShortcuts();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置开机自启失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动时检测并修复指向错误路径的开机启动项。
        /// 适用于覆盖安装、路径变更后首次启动的场景。
        /// </summary>
        public void FixStaleAutoStartEntry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (key == null) return;

                var currentPath = AppPath;
                var existingValue = key.GetValue(AppName) as string;

                // 如果存在启动项但路径与当前应用不一致，说明是旧版本残留
                if (!string.IsNullOrEmpty(existingValue) &&
                    !string.Equals(existingValue, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    // 读取用户配置，确认是否应重新设置
                    bool shouldAutoStart = false;
                    try
                    {
                        var setting = new Model.SettingModel();
                        if (setting.PathExist())
                        {
                            setting = setting.Read();
                            shouldAutoStart = setting.IsAutoRun;
                        }
                    }
                    catch { /* 配置读取失败时保守处理，不自动设置 */ }

                    // 删除指向旧路径的启动项
                    key.DeleteValue(AppName);
                    Debug.WriteLine($"已删除指向旧路径的启动项: {existingValue}");

                    // 若用户开启了自启，用新路径重新注册
                    if (shouldAutoStart && !string.IsNullOrEmpty(currentPath))
                    {
                        key.SetValue(AppName, currentPath);
                        Debug.WriteLine($"已重新注册启动项: {currentPath}");
                    }
                }

                // 同时清理其他历史遗留键名（无论路径是否一致）
                foreach (var legacyName in LegacyRegistryNames)
                {
                    if (key.GetValue(legacyName) != null)
                    {
                        key.DeleteValue(legacyName);
                        Debug.WriteLine($"已删除历史遗留启动项: {legacyName}");
                    }
                }

                // 清理启动文件夹旧快捷方式
                CleanLegacyStartupShortcuts();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"修复开机启动项失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理当前用户启动文件夹中的历史快捷方式
        /// </summary>
        private static void CleanLegacyStartupShortcuts()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!Directory.Exists(startupFolder))
                    return;

                foreach (var name in LegacyShortcutNames)
                {
                    string shortcutPath = Path.Combine(startupFolder, name + ".lnk");
                    if (File.Exists(shortcutPath))
                    {
                        File.Delete(shortcutPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理启动文件夹旧快捷方式失败: {ex.Message}");
            }
        }
    }
}
