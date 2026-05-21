using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using cpu_net.Model;
using Velopack;
using Velopack.Sources;

namespace cpu_net.Services
{
    /// <summary>
    /// 自动更新服务（基于 Velopack）
    /// </summary>
    public static class UpdateService
    {
        private static readonly string GitHubRepoUrl = "SoraNoNeko/cpu_net";
        private static UpdateManager? _updateManager;
        private static bool _isChecking;

        /// <summary>
        /// 当前应用版本号
        /// </summary>
        public static string CurrentVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString(3) ?? "Unknown";
            }
        }

        /// <summary>
        /// 初始化 Velopack 更新管理器
        /// 应在应用启动时尽早调用
        /// </summary>
        public static void Initialize()
        {
            try
            {
                ApplyProxySettings();
                _updateManager = new UpdateManager(new GithubSource($"https://github.com/{GitHubRepoUrl}", null, false));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] 初始化失败: {ex.Message}");
                LoggingService.WriteErrorLog(ex);
            }
        }

        private static void ApplyProxySettings()
        {
            try
            {
                var setting = new SettingModel();
                if (!setting.PathExist()) return;
                setting = setting.Read();

                if (!setting.UpdateProxyEnabled || string.IsNullOrWhiteSpace(setting.UpdateProxyHost))
                {
                    // 清除代理环境变量
                    Environment.SetEnvironmentVariable("HTTP_PROXY", null);
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
                    Environment.SetEnvironmentVariable("ALL_PROXY", null);
                    HttpClient.DefaultProxy = new WebProxy();
                    return;
                }

                string proxyType = setting.UpdateProxyType?.ToUpperInvariant() ?? "HTTP";
                string host = setting.UpdateProxyHost.Trim();
                int port = setting.UpdateProxyPort;
                if (port <= 0 || port > 65535) port = proxyType == "HTTP" ? 8080 : 1080;

                var proxyUri = new Uri($"{proxyType.ToLowerInvariant()}://{host}:{port}");
                var proxy = new System.Net.WebProxy(proxyUri);

                if (!string.IsNullOrWhiteSpace(setting.UpdateProxyUsername))
                {
                    proxy.Credentials = new System.Net.NetworkCredential(
                        setting.UpdateProxyUsername,
                        setting.UpdateProxyPassword ?? string.Empty);
                }

                proxy.UseDefaultCredentials = false;
                proxy.BypassProxyOnLocal = true;

                HttpClient.DefaultProxy = proxy;

                // 同时设置环境变量供其他 HTTP 客户端使用
                string proxyUrl = $"{proxyType.ToLowerInvariant()}://{host}:{port}";
                if (!string.IsNullOrWhiteSpace(setting.UpdateProxyUsername))
                {
                    proxyUrl = $"{proxyType.ToLowerInvariant()}://{setting.UpdateProxyUsername}:{setting.UpdateProxyPassword}@{host}:{port}";
                }
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);

                Debug.WriteLine($"[UpdateService] 已应用代理: {proxyType} {host}:{port}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] 应用代理设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 后台静默检查更新（不弹窗）
        /// </summary>
        public static async Task CheckForUpdatesSilentAsync(CancellationToken cancellationToken = default)
        {
            if (_updateManager == null || _isChecking) return;

            _isChecking = true;
            try
            {
                var newVersion = await _updateManager.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    Debug.WriteLine($"[UpdateService] 发现新版本: {newVersion.TargetFullRelease.Version}");
                    // 静默检查不弹窗，仅记录日志；如需自动下载可在后台执行
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] 静默检查更新失败: {ex.Message}");
            }
            finally
            {
                _isChecking = false;
            }
        }

        /// <summary>
        /// 检查更新并提示用户（用于手动检查）
        /// </summary>
        public static async Task CheckAndPromptUpdateAsync(Window? owner = null)
        {
            if (_updateManager == null)
            {
                MessageBox.Show(
                    owner,
                    "更新服务未初始化，请稍后重试。",
                    "检查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_isChecking)
            {
                MessageBox.Show(
                    owner,
                    "正在检查更新中，请稍候...",
                    "检查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _isChecking = true;
            try
            {
                var newVersion = await _updateManager.CheckForUpdatesAsync();
                if (newVersion == null)
                {
                    MessageBox.Show(
                        owner,
                        $"当前已是最新版本 ({CurrentVersion})。",
                        "检查更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    owner,
                    $"发现新版本: {newVersion.TargetFullRelease.Version}\n\n当前版本: {CurrentVersion}\n\n是否立即下载并安装？\n\n安装完成后应用将自动重启。",
                    "发现新版本",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await DownloadAndInstallAsync(owner, newVersion);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"检查更新失败:\n{ex.Message}",
                    "检查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                LoggingService.WriteErrorLog(ex);
            }
            finally
            {
                _isChecking = false;
            }
        }

        /// <summary>
        /// 下载并安装更新，完成后自动重启应用
        /// </summary>
        private static async Task DownloadAndInstallAsync(Window? owner, UpdateInfo updateInfo)
        {
            if (_updateManager == null) return;

            var progressWindow = new Views.Windows.UpdateProgressWindow();
            try
            {
                if (owner != null)
                {
                    progressWindow.Owner = owner;
                    progressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                progressWindow.Show();

                Action<int> progress = p => progressWindow.SetProgress(p);

                await _updateManager.DownloadUpdatesAsync(updateInfo, progress);

                progressWindow.Close();

                // 更新前备份用户配置（Velopack 会替换 current/ 目录）
                BackupUserDataBeforeUpdate();

                // 应用更新并重启
                _updateManager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    owner,
                    $"下载更新失败:\n{ex.Message}",
                    "更新失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                LoggingService.WriteErrorLog(ex);
            }
        }

        #region 用户数据保护与 Velopack 缓存清理

        private static readonly string[] PreservedItems = new[]
        {
            "config.yaml",
            "ErrorLog",
            "Log",
            "RecordLog",
            "Images"
        };

        /// <summary>
        /// 获取更新备份目录路径（%LocalAppData%\CPU_NET\update_backup）
        /// </summary>
        private static string GetUpdateBackupDir()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CPU_NET",
                "update_backup");
        }

        /// <summary>
        /// 更新前备份用户配置到 %LocalAppData%，防止 Velopack 替换 current/ 时丢失
        /// </summary>
        public static void BackupUserDataBeforeUpdate()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = GetUpdateBackupDir();

                if (Directory.Exists(backupDir))
                {
                    Directory.Delete(backupDir, true);
                }
                Directory.CreateDirectory(backupDir);

                foreach (string item in PreservedItems)
                {
                    string sourcePath = Path.Combine(appDir, item);
                    string destPath = Path.Combine(backupDir, item);

                    if (File.Exists(sourcePath))
                    {
                        string? parentDir = Path.GetDirectoryName(destPath);
                        if (parentDir != null)
                            Directory.CreateDirectory(parentDir);
                        File.Copy(sourcePath, destPath, true);
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        CopyDirectory(sourcePath, destPath);
                    }
                }

                Debug.WriteLine($"[UpdateService] 已备份用户数据到: {backupDir}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] 备份用户数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新后恢复用户配置，然后删除备份目录
        /// 应在 Velopack 初始化完成后、应用正式运行前调用
        /// </summary>
        public static void RestoreUserDataAfterUpdate()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = GetUpdateBackupDir();

                if (!Directory.Exists(backupDir))
                    return;

                foreach (string item in PreservedItems)
                {
                    string sourcePath = Path.Combine(backupDir, item);
                    string destPath = Path.Combine(appDir, item);

                    try
                    {
                        if (File.Exists(sourcePath))
                        {
                            string? parentDir = Path.GetDirectoryName(destPath);
                            if (parentDir != null)
                                Directory.CreateDirectory(parentDir);
                            File.Copy(sourcePath, destPath, true);
                        }
                        else if (Directory.Exists(sourcePath))
                        {
                            if (Directory.Exists(destPath))
                                Directory.Delete(destPath, true);
                            CopyDirectory(sourcePath, destPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[UpdateService] 恢复 {item} 失败: {ex.Message}");
                    }
                }

                // 恢复完成后清理备份目录
                try
                {
                    Directory.Delete(backupDir, true);
                }
                catch { }

                Debug.WriteLine($"[UpdateService] 已从备份恢复用户数据");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] 恢复用户数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理 Velopack 在 %TEMP% 下残留的临时缓存目录
        /// </summary>
        public static void CleanVelopackTemp()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                if (!Directory.Exists(tempPath))
                    return;

                foreach (string dir in Directory.GetDirectories(tempPath, "Velopack*"))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        Debug.WriteLine($"[UpdateService] 已清理 Velopack 缓存: {dir}");
                    }
                    catch
                    {
                        // 可能仍在使用中，忽略
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] 清理 Velopack 缓存失败: {ex.Message}");
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        #endregion
    }
}
