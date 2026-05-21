using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SetupBootstrapper.Pages
{
    public partial class FinishPage : Page
    {
        private readonly string _targetPath;

        public FinishPage(string targetPath)
        {
            InitializeComponent();
            _targetPath = targetPath;
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (LaunchCheckBox.IsChecked == true)
            {
                // 优先尝试 current/ 子目录下的主程序，回退到根目录
                string exePath = Path.Combine(_targetPath, "current", "cpu_net.exe");
                if (!File.Exists(exePath))
                    exePath = Path.Combine(_targetPath, "cpu_net.exe");

                if (File.Exists(exePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // 启动失败静默处理，不影响安装完成
                    }
                }
            }
            Application.Current.Shutdown();
        }
    }
}
