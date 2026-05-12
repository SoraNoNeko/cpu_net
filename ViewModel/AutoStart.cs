using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace cpu_net.ViewModel
{
    public class AutoStart
    {
        private const string AppName = "CPU_NET";

        private string AppPath => Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

        /// <summary>
        /// 设置开机自动启动
        /// </summary>
        public void SetMeAutoStart(bool onOff = true)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (key == null) return;

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
