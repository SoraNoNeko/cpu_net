using System;
using System.Windows;
using cpu_net.Services;

namespace cpu_net
{
    /// <summary>
    /// 应用程序入口点
    /// Velopack 必须在程序启动的最开始初始化
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // Velopack 必须在任何 UI 初始化之前运行
            Velopack.VelopackApp.Build().Run();

            // 更新后恢复用户配置（Velopack 替换 current/ 会导致配置丢失）
            UpdateService.RestoreUserDataAfterUpdate();

            // 清理 Velopack 在 %TEMP% 下残留的临时缓存
            UpdateService.CleanVelopackTemp();

            // 启动 WPF 应用程序
            var app = new App();
            app.InitializeComponent();  // 加载 App.xaml 资源并设置 StartupUri
            app.Run();
        }
    }
}
