using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SetupBootstrapper.Pages
{
    public partial class ProgressPage : Page
    {
        private readonly string _targetPath;
        private Process? _setupProcess;
        private bool _isInstalling = false;
        private bool _allowClose = false;

        public ProgressPage(string targetPath)
        {
            InitializeComponent();
            _targetPath = targetPath;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is Window w)
            {
                w.Closing += OnWindowClosing;
            }
            _ = RunInstallationAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is Window w)
            {
                w.Closing -= OnWindowClosing;
            }
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (_isInstalling && !_allowClose)
            {
                e.Cancel = true;
                MessageBox.Show(
                    "安装正在进行中，请勿关闭窗口。",
                    "安装中",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        // =================================================================
        // 主入口
        // =================================================================
        private async Task RunInstallationAsync()
        {
            IProgress<InstallProgress> progress = new Progress<InstallProgress>(p => UpdateUI(p));
            _isInstalling = true;
            CancelButton.IsEnabled = false;
            string? tempDir = null;

            try
            {
                tempDir = await Task.Run(() => ExecuteInstallation(progress));

                await Task.Delay(600);
                _isInstalling = false;
                _allowClose = true;
                Dispatcher.Invoke(() =>
                {
                    InstallProgressBar.Value = 100;
                    CurrentStepTextBlock.Text = "安装完成！";
                    DetailTextBlock.Text = "CPU_NET 已成功安装";
                    CancelButton.Visibility = Visibility.Collapsed;
                    NextButton.Visibility = Visibility.Visible;
                    NextButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Print(progress, $"[FATAL] 安装流程异常终止: {ex.Message}");
                _isInstalling = false;
                Dispatcher.Invoke(() =>
                {
                    CancelButton.IsEnabled = true;
                    CancelButton.Content = "关闭";
                    MessageBox.Show(
                        $"安装过程中发生错误:\n\n{ex.Message}\n\n请检查目标文件夹权限或磁盘空间后重试。",
                        "安装失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                {
                    SafeDeleteDirectory(tempDir);
                }
            }
        }

        // =================================================================
        // Chunk 编排器
        // =================================================================
        private string ExecuteInstallation(IProgress<InstallProgress> progress)
        {
            string tempDir = string.Empty;
            string? userDataBackupDirCurrent = null;
            string? userDataBackupDirRoot = null;
            var setupStderr = new StringBuilder();

            // Chunk 00: 参数校验 + 文件锁检测
            RunChunk(progress, 0, "准备安装环境...", "参数校验与文件锁检测", p =>
            {
                if (string.IsNullOrWhiteSpace(_targetPath))
                    throw new ArgumentException("安装路径不能为空");

                if (Directory.Exists(_targetPath))
                {
                    var lockedFiles = GetLockedFiles(_targetPath);
                    if (lockedFiles.Length > 0)
                    {
                        var lockers = GetLockingProcesses(lockedFiles);
                        string lockerInfo = lockers.Count > 0 ? $"\n占用进程: {string.Join(", ", lockers)}" : "";
                        throw new InvalidOperationException(
                            "目标文件夹中的文件正被其他程序占用，无法覆盖安装。\n\n" +
                            $"被占用的文件:\n{string.Join("\n", lockedFiles)}" + lockerInfo +
                            "\n\n请关闭相关程序后重试。");
                    }
                }
            });

            // Chunk 01: 创建临时目录
            RunChunk(progress, 1, "正在准备安装环境...", "创建临时工作目录", p =>
            {
                tempDir = Path.Combine(Path.GetTempPath(), $"CPU_NET_Install_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
            });

            string tempSetup = Path.Combine(tempDir, "setup.exe");
            long setupFileSize = 0;

            // Chunk 02: 释放安装器
            RunChunk(progress, 2, "正在准备安装组件...", "提取内嵌安装器", p =>
            {
                ExtractEmbeddedSetup(tempSetup, p);
                setupFileSize = new FileInfo(tempSetup).Length;
            });

            // Chunk 03: 备份用户数据 + 清理旧版本
            long initialDirSize = 0;
            RunChunk(progress, 3, "正在检查目标目录...", "备份用户数据并清理旧版本", p =>
            {
                initialDirSize = GetDirectorySize(_targetPath);
                if (Directory.Exists(_targetPath))
                {
                    var entries = Directory.GetFileSystemEntries(_targetPath);
                    bool hasOldVersion = entries.Any(e =>
                        Path.GetFileName(e).Equals("cpu_net.exe", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileName(e).Equals("Update.exe", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileName(e).Equals("RELEASES", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileName(e).Equals("current", StringComparison.OrdinalIgnoreCase));

                    if (hasOldVersion)
                    {
                        // 备份 current/ 中的用户数据（新版）
                        string currentDir = Path.Combine(_targetPath, "current");
                        if (Directory.Exists(currentDir))
                        {
                            userDataBackupDirCurrent = Path.Combine(tempDir, "user_data_backup_current");
                            BackupUserData(currentDir, userDataBackupDirCurrent, p);
                        }
                        // 备份根目录中的用户数据（兼容旧版）
                        userDataBackupDirRoot = Path.Combine(tempDir, "user_data_backup_root");
                        BackupUserData(_targetPath, userDataBackupDirRoot, p);
                        CleanOldVersionFiles(_targetPath, p);
                    }
                }
            });

            // Chunk 04: 启动安装引擎
            RunChunk(progress, 4, "正在启动安装引擎...", "启动 Velopack Setup", p =>
            {
                if (!Directory.Exists(_targetPath))
                    Directory.CreateDirectory(_targetPath);

                var psi = new ProcessStartInfo
                {
                    FileName = tempSetup,
                    Arguments = $"--silent --installto \"{_targetPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _setupProcess = Process.Start(psi);
                if (_setupProcess == null)
                    throw new InvalidOperationException("无法启动安装程序");

                // 异步读取避免缓冲区满导致阻塞，但不再实时打印到 UI
                _setupProcess.OutputDataReceived += (s, e) => { /* discard stdout */ };
                _setupProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        setupStderr.AppendLine(e.Data);
                };
                _setupProcess.BeginOutputReadLine();
                _setupProcess.BeginErrorReadLine();
            });

            // Chunk 05: 监控文件复制（耗时最长）
            long expectedTotalSize = Math.Max(setupFileSize * 3, 100L * 1024 * 1024);
            RunChunk(progress, 5, "正在安装应用程序...", "复制文件到目标目录", p =>
            {
                MonitorInstallationProgress(_setupProcess!, initialDirSize, ref expectedTotalSize, p);
            });

            // Chunk 06: 等待引擎退出、验证、恢复用户数据
            RunChunk(progress, 6, "正在完成安装...", "验证安装结果", p =>
            {
                if (_setupProcess == null) return;

                if (_setupProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"安装引擎返回非零退出码 ({_setupProcess.ExitCode})。\n{setupStderr}");
                }

                // 检查 setup.exe 是否安装到用户指定路径
                string defaultInstallDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CPU_NET");
                string targetExeCurrent = Path.Combine(_targetPath, "current", "cpu_net.exe");
                string defaultExeCurrent = Path.Combine(defaultInstallDir, "current", "cpu_net.exe");

                bool targetHasExe = File.Exists(targetExeCurrent) || File.Exists(Path.Combine(_targetPath, "cpu_net.exe"));
                bool defaultHasExe = File.Exists(defaultExeCurrent) || File.Exists(Path.Combine(defaultInstallDir, "cpu_net.exe"));

                if (!targetHasExe && defaultHasExe &&
                    !string.Equals(_targetPath, defaultInstallDir, StringComparison.OrdinalIgnoreCase))
                {
                    Print(p, "[VERIFY] --installto 未生效，执行兜底复制...");
                    CopyDirectory(defaultInstallDir, _targetPath, p);
                }
                else if (!targetHasExe && !defaultHasExe)
                {
                    throw new InvalidOperationException(
                        "安装引擎未写入任何文件。用户路径和默认路径均未找到 cpu_net.exe。");
                }

                // 恢复用户数据到 current/ 目录（新版结构）
                string currentDir = Path.Combine(_targetPath, "current");
                if (Directory.Exists(currentDir))
                {
                    if (!string.IsNullOrEmpty(userDataBackupDirCurrent) && Directory.Exists(userDataBackupDirCurrent))
                        RestoreUserData(userDataBackupDirCurrent, currentDir, p);
                    if (!string.IsNullOrEmpty(userDataBackupDirRoot) && Directory.Exists(userDataBackupDirRoot))
                        RestoreUserData(userDataBackupDirRoot, currentDir, p);
                }
                else
                {
                    // 若安装后仍是根目录结构（非 Velopack），恢复到根目录
                    if (!string.IsNullOrEmpty(userDataBackupDirRoot) && Directory.Exists(userDataBackupDirRoot))
                        RestoreUserData(userDataBackupDirRoot, _targetPath, p);
                }

                long finalSize = GetDirectorySize(_targetPath);
                Print(p, $"[VERIFY] 安装完成，目录大小: {FormatBytes(finalSize)}");
            });

            // Chunk 07: 清理旧启动项
            RunChunk(progress, 7, "正在完成安装...", "清理旧版本开机启动项", p =>
            {
                CleanLegacyAutoStart(_targetPath);
            });

            // Chunk 08: 同步自启配置
            RunChunk(progress, 8, "正在完成安装...", "同步用户开机自启配置", p =>
            {
                SyncAutoStartSetting(p);
            });

            // Chunk 09: 创建卸载入口（后台异步）
            RunChunk(progress, 9, "正在完成安装...", "创建卸载入口", p =>
            {
                _ = Task.Run(() => CreateUninstallEntry(p));
            });

            // Chunk 10: 最终确认
            RunChunk(progress, 10, "正在完成安装...", "确认安装完整性", p =>
            {
                string exePathCurrent = Path.Combine(_targetPath, "current", "cpu_net.exe");
                string exePathRoot = Path.Combine(_targetPath, "cpu_net.exe");
                if (!File.Exists(exePathCurrent) && !File.Exists(exePathRoot))
                {
                    throw new FileNotFoundException(
                        $"安装完成后未找到主程序 cpu_net.exe。目标路径: {_targetPath}");
                }
            });

            progress?.Report(new InstallProgress(100, "安装完成！", "CPU_NET 已成功安装"));
            return tempDir;
        }

        // =================================================================
        // Chunk 执行器：每个 Chunk 都有明确的生命周期日志
        // =================================================================
        private void RunChunk(IProgress<InstallProgress> progress, int id, string stepText, string detailText, Action<IProgress<InstallProgress>> action)
        {
            progress?.Report(new InstallProgress(0, stepText, detailText));
            try
            {
                action(progress);
            }
            catch (Exception ex)
            {
                Print(progress, $"[错误] {detailText}: {ex.Message}");
                throw;
            }
        }

        private static void Print(IProgress<InstallProgress>? progress, string message)
        {
            // 使用 -1 表示仅追加日志，不覆盖进度条和文字
            progress?.Report(new InstallProgress(-1, string.Empty, string.Empty, message));
        }

        // =================================================================
        // 各 Chunk 具体实现
        // =================================================================

        private void ExtractEmbeddedSetup(string targetPath, IProgress<InstallProgress>? progress)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("setup.exe");
            if (stream == null)
                throw new InvalidOperationException("找不到内嵌的安装程序资源 'setup.exe'");

            using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 262144);
            stream.CopyTo(fileStream);
            Print(progress, $"[EXTRACT] 已释放安装引擎: {Path.GetFileName(targetPath)} ({FormatBytes(stream.Length)})");
        }

        private void MonitorInstallationProgress(Process process, long initialDirSize, ref long expectedTotalSize, IProgress<InstallProgress>? progress)
        {
            long lastReportedSize = initialDirSize;
            const int pollIntervalMs = 500;
            const int maxWaitMinutes = 2; // 2 分钟超时保护
            int pollCount = 0;
            int stalledCount = 0;

            while (!process.HasExited)
            {
                Thread.Sleep(pollIntervalMs);
                pollCount++;

                // 超时保护：setup.exe 超过 5 分钟未退出，强制终止并报错
                if (pollCount * pollIntervalMs > maxWaitMinutes * 60 * 1000)
                {
                    Print(progress, $"[TIMEOUT] Setup.exe 超过 {maxWaitMinutes} 分钟未退出，强制终止...");
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch { /* ignore */ }
                    throw new TimeoutException(
                        $"安装引擎超时（>{maxWaitMinutes}分钟）。可能原因：\n" +
                        "1. 旧版本 CPU_NET 正在运行，文件被占用\n" +
                        "2. 磁盘空间不足或磁盘故障\n" +
                        "3. 杀毒软件拦截了文件写入\n" +
                        "请关闭旧版本后重试。");
                }

                long currentSize = GetDirectorySize(_targetPath);
                long delta = currentSize - initialDirSize;

                if (delta > expectedTotalSize * 0.9 && expectedTotalSize > 0)
                {
                    expectedTotalSize = (long)(delta * 1.5);
                }

                double ratio = expectedTotalSize > 0 ? (double)delta / expectedTotalSize : 0;
                int percent = 20 + (int)(Math.Min(ratio, 1.0) * 64);
                percent = Math.Clamp(percent, 20, 83);

                if (currentSize > lastReportedSize)
                {
                    stalledCount = 0;
                    progress?.Report(new InstallProgress(percent,
                        $"正在安装应用程序...",
                        $"已写入: {FormatBytes(delta)} / 预估: {FormatBytes(expectedTotalSize)}"));
                    lastReportedSize = currentSize;
                }
                else
                {
                    stalledCount++;
                }
            }

            long finalSize = GetDirectorySize(_targetPath);
            long finalDelta = finalSize - initialDirSize;
            progress?.Report(new InstallProgress(84, "正在完成安装...", $"文件复制完成，总计写入: {FormatBytes(finalDelta)}"));
        }

        /// <summary>
        /// 解析安装目录下实际的主程序路径。
        /// Velopack 标准结构下主程序在 current/ 子目录，兼容根目录的 fallback。
        /// </summary>
        private static string ResolveExePath(string baseDir)
        {
            string currentPath = Path.Combine(baseDir, "current", "cpu_net.exe");
            string rootPath = Path.Combine(baseDir, "cpu_net.exe");
            if (File.Exists(currentPath)) return currentPath;
            if (File.Exists(rootPath)) return rootPath;
            return currentPath; // 默认返回 current/ 下的路径（Velopack 标准）
        }

        private void SyncAutoStartSetting(IProgress<InstallProgress>? progress)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key == null)
                {
                    Print(progress, "[AUTOSTART] 无法打开注册表 Run 键");
                    return;
                }

                var newAppPath = ResolveExePath(_targetPath);
                bool wasAutoStart = false;
                string[] legacyNames = new[] { "cpu_net", "cpu_net.exe", "CPU_NET" };

                foreach (var name in legacyNames)
                {
                    var val = key.GetValue(name) as string;
                    if (!string.IsNullOrEmpty(val))
                    {
                        Print(progress, $"[AUTOSTART] 发现遗留键 '{name}' = {val}");
                        wasAutoStart = true;
                        if (!string.Equals(val, newAppPath, StringComparison.OrdinalIgnoreCase))
                        {
                            key.DeleteValue(name);
                            Print(progress, $"[AUTOSTART] 删除旧键 '{name}'");
                        }
                    }
                }

                if (wasAutoStart && File.Exists(newAppPath))
                {
                    key.SetValue("cpu_net", newAppPath);
                    Print(progress, $"[AUTOSTART] 已注册新路径: {newAppPath}");
                }
                else
                {
                    Print(progress, "[AUTOSTART] 未发现旧版自启配置，无需迁移");
                }
            }
            catch (Exception ex)
            {
                Print(progress, $"[AUTOSTART] 异常: {ex.Message}");
            }
        }

        private static void CleanLegacyAutoStart(string newInstallPath)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key == null) return;

                var newAppPath = ResolveExePath(newInstallPath);

                var existingValue = key.GetValue("cpu_net") as string;
                if (!string.IsNullOrEmpty(existingValue) &&
                    !string.Equals(existingValue, newAppPath, StringComparison.OrdinalIgnoreCase))
                {
                    key.DeleteValue("cpu_net");
                }

                foreach (var legacyName in new[] { "cpu_net.exe", "CPU_NET" })
                {
                    if (key.GetValue(legacyName) != null)
                        key.DeleteValue(legacyName);
                }

                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (Directory.Exists(startupFolder))
                {
                    foreach (var name in new[] { "cpu_net", "cpu_net.exe", "CPU_NET" })
                    {
                        string shortcutPath = Path.Combine(startupFolder, name + ".lnk");
                        if (File.Exists(shortcutPath))
                            File.Delete(shortcutPath);
                    }
                }
            }
            catch { /* 日志在 Chunk 层统一处理 */ }
        }

        /// <summary>
        /// 覆盖安装前清理旧版本的 Velopack 程序文件，保留用户配置。
        /// </summary>
        private static void CleanOldVersionFiles(string directory, IProgress<InstallProgress>? progress)
        {
            try
            {
                int deleted = 0, kept = 0, failed = 0;
                var entries = Directory.GetFileSystemEntries(directory);
                foreach (var entry in entries)
                {
                    string name = Path.GetFileName(entry);
                    // 保留用户配置、日志目录和图片目录
                    string[] preservedNames = new[] { "config.yaml", "ErrorLog", "Log", "RecordLog", "Images" };
                    if (preservedNames.Any(p => name.Equals(p, StringComparison.OrdinalIgnoreCase)) ||
                        name.EndsWith(".log", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
                    {
                        kept++;
                        continue;
                    }

                    try
                    {
                        if (File.Exists(entry))
                        {
                            File.Delete(entry);
                            deleted++;
                        }
                        else if (Directory.Exists(entry))
                        {
                            Directory.Delete(entry, true);
                            deleted++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Print(progress, $"[CLEANUP] 无法删除 {name}: {ex.Message}");
                    }
                }
                if (deleted > 0 || failed > 0)
                    Print(progress, $"[CLEANUP] 清理旧版本: 删除 {deleted} 项, 保留 {kept} 项{(failed > 0 ? $", 失败 {failed} 项" : "")}");
            }
            catch (Exception ex)
            {
                Print(progress, $"[CLEANUP] 清理旧版本异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检测目标目录下是否有被占用的关键文件（.exe / .dll / .config / .yaml）。
        /// </summary>
        private static string[] GetLockedFiles(string directory)
        {
            if (!Directory.Exists(directory)) return Array.Empty<string>();
            var locked = new System.Collections.Generic.List<string>();
            var patterns = new[] { "*.exe", "*.dll", "*.config", "*.yaml", "*.yml" };

            foreach (var pattern in patterns)
            {
                foreach (var file in Directory.EnumerateFiles(directory, pattern))
                {
                    if (IsFileLocked(file))
                        locked.Add(Path.GetFileName(file));
                }
            }
            return locked.ToArray();
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException) { return true; }
            catch (UnauthorizedAccessException) { return true; }
            catch { return false; }
        }

        /// <summary>
        /// 获取占用指定文件的进程名列表。
        /// </summary>
        private static System.Collections.Generic.List<string> GetLockingProcesses(string[] fileNames)
        {
            var result = new System.Collections.Generic.List<string>();
            try
            {
                var allProcesses = Process.GetProcesses();
                foreach (var proc in allProcesses)
                {
                    try
                    {
                        var modules = proc.Modules;
                        foreach (ProcessModule mod in modules)
                        {
                            if (fileNames.Any(f =>
                                string.Equals(mod.ModuleName, f, StringComparison.OrdinalIgnoreCase)))
                            {
                                if (!result.Contains(proc.ProcessName))
                                    result.Add(proc.ProcessName);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 目录递归复制（兜底方案：setup.exe 安装到默认位置后复制到用户路径）
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destDir, IProgress<InstallProgress>? progress)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (var file in Directory.EnumerateFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, overwrite: true);
                }
                catch (Exception ex)
                {
                    Print(progress, $"[COPY] 复制失败 {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir, progress);
            }
        }

        /// <summary>
        /// 备份 current/ 目录中的用户数据到临时目录
        /// </summary>
        private static void BackupUserData(string currentDir, string backupDir, IProgress<InstallProgress>? progress)
        {
            string[] preservedItems = new[] { "config.yaml", "ErrorLog", "Log", "RecordLog", "Images" };
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            int backedUp = 0;
            foreach (var item in preservedItems)
            {
                string sourcePath = Path.Combine(currentDir, item);
                string destPath = Path.Combine(backupDir, item);
                try
                {
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, destPath, overwrite: true);
                        backedUp++;
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        CopyDirectory(sourcePath, destPath, progress);
                        backedUp++;
                    }
                }
                catch (Exception ex)
                {
                    Print(progress, $"[BACKUP] 备份 {item} 失败: {ex.Message}");
                }
            }
            if (backedUp > 0)
                Print(progress, $"[BACKUP] 已备份 {backedUp} 项用户数据");
        }

        /// <summary>
        /// 从临时目录恢复用户数据到 current/ 目录
        /// </summary>
        private static void RestoreUserData(string backupDir, string currentDir, IProgress<InstallProgress>? progress)
        {
            if (!Directory.Exists(backupDir))
                return;

            string[] preservedItems = new[] { "config.yaml", "ErrorLog", "Log", "RecordLog", "Images" };
            int restored = 0;
            foreach (var item in preservedItems)
            {
                string sourcePath = Path.Combine(backupDir, item);
                string destPath = Path.Combine(currentDir, item);
                try
                {
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, destPath, overwrite: true);
                        restored++;
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        CopyDirectory(sourcePath, destPath, progress);
                        restored++;
                    }
                }
                catch (Exception ex)
                {
                    Print(progress, $"[RESTORE] 恢复 {item} 失败: {ex.Message}");
                }
            }
            if (restored > 0)
                Print(progress, $"[RESTORE] 已恢复 {restored} 项用户数据");
        }


        private void CreateUninstallEntry(IProgress<InstallProgress>? progress)
        {
            try
            {
                string targetPath = _targetPath;
                string updateExe = Path.Combine(targetPath, "Update.exe");
                if (!File.Exists(updateExe))
                {
                    Print(progress, "[UNINSTALL] Update.exe 不存在，跳过");
                    return;
                }

                string startMenuPrograms = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "CPU_NET");

                // 1. 从嵌入资源释放 uninstaller.exe 到安装目录
                string uninstallerPath = Path.Combine(targetPath, "uninstaller.exe");
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("uninstaller.exe"))
                {
                    if (stream == null)
                    {
                        Print(progress, "[UNINSTALL] 嵌入资源 uninstaller.exe 不存在，跳过");
                        return;
                    }
                    using var fileStream = new FileStream(uninstallerPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    stream.CopyTo(fileStream);
                    Print(progress, $"[UNINSTALL] 已释放 uninstaller.exe: {uninstallerPath}");
                }

                // 2. 安装目录中的触发脚本（用户双击的入口）
                // 职责：调用 uninstaller.exe 并传入安装目录，然后立即退出
                string batPath = Path.Combine(targetPath, "卸载 CPU_NET.bat");
                var trigger = new System.Text.StringBuilder();
                trigger.AppendLine("@echo off");
                trigger.AppendLine("chcp 65001 >nul");
                trigger.AppendLine("echo ========================================");
                trigger.AppendLine("echo         CPU_NET 卸载程序");
                trigger.AppendLine("echo ========================================");
                trigger.AppendLine("echo.");
                trigger.AppendLine("set /p confirm=确认要卸载 CPU_NET 吗？[Y/N]: ");
                trigger.AppendLine("if /I not \"%confirm%\"==\"Y\" exit");
                trigger.AppendLine("echo.");
                trigger.AppendLine("echo 正在启动卸载...");
                trigger.AppendLine("echo.");
                trigger.AppendLine("set installPath=%~dp0");
                trigger.AppendLine("set installPath=%installPath:~0,-1%");
                trigger.AppendLine("cd /d %TEMP%");  // 切换 bat 自身工作目录，避免占用安装目录
                trigger.AppendLine("start /wait \"\" \"%~dp0\\uninstaller.exe\" \"%installPath%\"");
                trigger.AppendLine("echo.");
                trigger.AppendLine("echo 清理卸载程序残留...");
                trigger.AppendLine("if exist \"%~dp0\\uninstaller.exe\" del \"%~dp0\\uninstaller.exe\" >nul 2>&1");
                trigger.AppendLine("del \"%~f0\" >nul 2>&1");
                trigger.AppendLine("exit /b 0");

                File.WriteAllText(batPath, trigger.ToString());
                Print(progress, $"[UNINSTALL] 卸载入口已创建: {batPath}");

                // 3. 开始菜单快捷方式指向触发脚本（而不是直接指向 Update.exe）
                string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                string programsFolder = Path.Combine(startMenu, "Programs", "CPU_NET");
                Directory.CreateDirectory(programsFolder);
                string shortcutPath = Path.Combine(programsFolder, "卸载 CPU_NET.lnk");

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" +
                        $"$ws = New-Object -ComObject WScript.Shell; " +
                        $"$s = $ws.CreateShortcut('{shortcutPath}'); " +
                        $"$s.TargetPath = '{batPath}'; " +
                        $"$s.WorkingDirectory = '{targetPath}'; " +
                        "$s.Description = '卸载 CPU_NET'; " +
                        "$s.IconLocation = '%SystemRoot%\\System32\\shell32.dll,31'; " +
                        "$s.Save()\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit(3000);
                Print(progress, $"[UNINSTALL] 开始菜单快捷方式: {shortcutPath}");
            }
            catch (Exception ex)
            {
                Print(progress, $"[UNINSTALL] 异常: {ex.Message}");
            }
        }

        private static void SafeDeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                try
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                        File.SetAttributes(file, FileAttributes.Normal);
                    Directory.Delete(path, true);
                }
                catch { }
            }
        }

        /// <summary>
        /// 计算目录总大小。只遍历根目录 + current/ 子目录（Velopack 安装的主要位置），
        /// 避免全量递归 packages/ 等缓存目录导致性能急剧下降。
        /// </summary>
        private static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    try { size += new FileInfo(file).Length; } catch { }
                }

                string currentDir = Path.Combine(path, "current");
                if (Directory.Exists(currentDir))
                {
                    foreach (var file in Directory.EnumerateFiles(currentDir, "*", SearchOption.AllDirectories))
                    {
                        try { size += new FileInfo(file).Length; } catch { }
                    }
                }
            }
            catch { /* 忽略遍历错误 */ }
            return size;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        private void UpdateUI(InstallProgress progress)
        {
            // 仅当值有效时才更新，避免 Print 的日志消息覆盖进度和文字
            if (progress.Percent >= 0)
                InstallProgressBar.Value = progress.Percent;
            if (!string.IsNullOrEmpty(progress.StepDescription))
                CurrentStepTextBlock.Text = progress.StepDescription;
            if (!string.IsNullOrEmpty(progress.Detail))
                DetailTextBlock.Text = progress.Detail;

            if (!string.IsNullOrEmpty(progress.LogLine))
            {
                LogTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}] {progress.LogLine}\n";
                LogScrollViewer.ScrollToEnd();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.NavigateTo(new FinishPage(_targetPath));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling && _setupProcess != null && !_setupProcess.HasExited)
            {
                MessageBox.Show(
                    "安装正在进行中，无法取消。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            _allowClose = true;
            Application.Current.Shutdown();
        }
    }

    public class InstallProgress
    {
        public int Percent { get; }
        public string StepDescription { get; }
        public string Detail { get; }
        public string LogLine { get; }

        public InstallProgress(int percent, string stepDescription, string detail = "", string logLine = "")
        {
            Percent = percent;
            StepDescription = stepDescription;
            Detail = detail;
            LogLine = logLine;
        }
    }
}
