using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
                _updateManager = new UpdateManager(new GithubSource($"https://github.com/{GitHubRepoUrl}", null, false));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] 初始化失败: {ex.Message}");
                LoggingService.WriteErrorLog(ex);
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
    }
}
