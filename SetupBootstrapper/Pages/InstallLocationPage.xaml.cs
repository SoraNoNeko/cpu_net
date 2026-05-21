using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SetupBootstrapper.Helpers;
using SetupBootstrapper.Models;

namespace SetupBootstrapper.Pages
{
    public partial class InstallLocationPage : Page
    {
        private CancellationTokenSource? _debounceCts;
        private string _lastCheckedPath = string.Empty;

        public InstallLocationPage()
        {
            InitializeComponent();
            PathTextBox.Text = GetDefaultInstallPath();
            _ = CheckAsync(PathTextBox.Text); // fire-and-forget 首次检测
        }

        private string GetDefaultInstallPath()
        {
            // 1. 检测默认路径是否已有安装
            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CPU_NET");
            if (IsValidInstallDir(defaultPath))
                return defaultPath;

            // 2. 从注册表 Run 键解析旧安装路径
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run");
                if (key != null)
                {
                    foreach (string name in new[] { "cpu_net", "cpu_net.exe", "CPU_NET" })
                    {
                        var val = key.GetValue(name) as string;
                        if (!string.IsNullOrEmpty(val))
                        {
                            string? exePath = val.Trim('"').Split('"').FirstOrDefault();
                            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                            {
                                string dir = Path.GetDirectoryName(exePath)!;
                                // Velopack 结构：exe 在 current/ 子目录
                                if (Path.GetFileName(dir).Equals("current", StringComparison.OrdinalIgnoreCase))
                                    dir = Path.GetDirectoryName(dir)!;
                                if (IsValidInstallDir(dir))
                                    return dir;
                            }
                        }
                    }
                }
            }
            catch { }

            // 3. 从开始菜单快捷方式解析安装路径
            try
            {
                string programs = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs", "CPU_NET");
                if (Directory.Exists(programs))
                {
                    var lnk = Directory.GetFiles(programs, "*.lnk").FirstOrDefault();
                    if (!string.IsNullOrEmpty(lnk))
                    {
                        string target = ResolveShortcut(lnk);
                        if (!string.IsNullOrEmpty(target) && File.Exists(target))
                        {
                            string dir = Path.GetDirectoryName(target)!;
                            if (Path.GetFileName(dir).Equals("current", StringComparison.OrdinalIgnoreCase))
                                dir = Path.GetDirectoryName(dir)!;
                            if (IsValidInstallDir(dir))
                                return dir;
                        }
                    }
                }
            }
            catch { }

            return defaultPath;
        }

        private static bool IsValidInstallDir(string path)
        {
            if (!Directory.Exists(path)) return false;
            // Velopack 安装标志
            if (File.Exists(Path.Combine(path, "Update.exe"))) return true;
            // 旧版安装标志
            if (File.Exists(Path.Combine(path, "cpu_net.exe"))) return true;
            if (File.Exists(Path.Combine(path, "current", "cpu_net.exe"))) return true;
            return false;
        }

        private static string ResolveShortcut(string lnkPath)
        {
            try
            {
                // 使用 WScript.Shell 解析快捷方式
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return string.Empty;
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                string target = shortcut.TargetPath;
                return target ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        private void PathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 防抖：停止输入 300ms 后才检测
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            string path = PathTextBox.Text;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, token);
                    if (!token.IsCancellationRequested)
                        await CheckAsync(path);
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private async Task CheckAsync(string path)
        {
            if (path == _lastCheckedPath) return;
            _lastCheckedPath = path;

            // 立即更新 UI 为"检测中"状态，不让用户等待时感到卡顿
            await Dispatcher.InvokeAsync(() =>
            {
                if (path != PathTextBox.Text) return;
                StatusTextBlock.Text = "正在检测文件夹...";
                StatusTextBlock.Foreground = Brushes.Gray;
                InstallButton.IsEnabled = false;
            });

            // 后台线程执行检测，不卡 UI
            var status = await Task.Run(() => DirectoryChecker.CheckDirectory(path));

            // 回传 UI
            await Dispatcher.InvokeAsync(() =>
            {
                if (path != PathTextBox.Text) return; // 路径已变，丢弃旧结果

                // 重置按钮状态
                InstallButton.Visibility = Visibility.Visible;
                AdminPanel.Visibility = Visibility.Collapsed;

                switch (status)
                {
                    case DirectoryStatus.Empty:
                        StatusTextBlock.Text = "目标文件夹为空，可以安装。";
                        StatusTextBlock.Foreground = Brushes.Green;
                        InstallButton.IsEnabled = true;
                        break;

                    case DirectoryStatus.OldVersion:
                        StatusTextBlock.Text = "检测到旧版本，覆盖安装";
                        StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xA2, 0x3C));
                        InstallButton.IsEnabled = true;
                        break;

                    case DirectoryStatus.HasOtherFiles:
                        StatusTextBlock.Text = "目标文件夹不为空，请选择空文件夹";
                        StatusTextBlock.Foreground = Brushes.Red;
                        InstallButton.IsEnabled = false;
                        break;

                    case DirectoryStatus.RequiresAdmin:
                        StatusTextBlock.Text = "当前权限不足，无法写入该文件夹。请选择其他位置，或以管理员身份重启安装程序。";
                        StatusTextBlock.Foreground = Brushes.Red;
                        InstallButton.Visibility = Visibility.Collapsed;
                        AdminPanel.Visibility = Visibility.Visible;
                        break;
                }
            });
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedPath = PathTextBox.Text;

            var dialog = new FolderPicker
            {
                Title = "选择安装文件夹",
                InitialDirectory = Directory.Exists(selectedPath) ? selectedPath : string.Empty
            };

            if (dialog.ShowDialog(Window.GetWindow(this)))
            {
                PathTextBox.Text = dialog.ResultPath;
                // 浏览后立即检测（跳过防抖）
                _debounceCts?.Cancel();
                _ = CheckAsync(dialog.ResultPath);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.NavigateTo(new WelcomePage());
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.NavigateTo(new ProgressPage(PathTextBox.Text));
        }

        private void RestartAsAdminButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? throw new InvalidOperationException("无法获取当前程序路径");

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Verb = "runas", // 请求 UAC 提升
                    UseShellExecute = true,
                    Arguments = $"/installto \"{PathTextBox.Text}\""
                };

                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"提升权限失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelAdminButton_Click(object sender, RoutedEventArgs e)
        {
            // 取消：恢复到默认安装路径（LocalAppData 通常不需要管理员权限）
            PathTextBox.Text = GetDefaultInstallPath();
        }
    }
}
