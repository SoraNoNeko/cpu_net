using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace Uninstaller
{
    class Program
    {
        // Restart Manager API
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, StringBuilder strSessionKey);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames,
            uint nApplications, IntPtr rgApplications, uint nServices, IntPtr rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded,
            ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[]? rgAffectedApps, out uint lpdwRebootReasons);

        [DllImport("rstrtmgr.dll")]
        static extern int RmShutdown(uint dwSessionHandle, uint lActionFlags, IntPtr fnStatusCallback);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        // WinAPI for self-delete
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, uint dwFlags);

        const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

        static void Main(string[] args)
        {
            // 立刻切换当前目录到 TEMP，避免自身占用安装目录
            string tempDir = Path.GetTempPath();
            Environment.CurrentDirectory = tempDir;

            if (args.Length == 0)
            {
                Console.WriteLine("CPU_NET Uninstaller v1.0.0");
                Console.WriteLine("Usage: uninstaller.exe <install-path>");
                Log("错误: 缺少参数");
                return;
            }

            string installPath = Path.GetFullPath(args[0]);
            string updateExe = Path.Combine(installPath, "Update.exe");
            string? currentExe = Process.GetCurrentProcess().MainModule?.FileName;

            Console.WriteLine("========================================");
            Console.WriteLine("      CPU_NET 卸载程序 v1.0.0");
            Console.WriteLine("========================================");

            // 1. 如果自身在安装目录中，复制到 TEMP 并重启
            string normalizedInstallPath = installPath.TrimEnd('\\', '/');
            if (!string.IsNullOrEmpty(currentExe) &&
                currentExe.StartsWith(normalizedInstallPath + "\\", StringComparison.OrdinalIgnoreCase))
            {
                string tempExe = Path.Combine(Path.GetTempPath(), $"CPU_NET_Uninstaller_{Guid.NewGuid():N}.exe");
                File.Copy(currentExe, tempExe, overwrite: true);
                Log($"[Step 1/7] 自身位于安装目录内，复制到临时目录: {tempExe}");
                Console.WriteLine($"[Step 1/7] 已复制到临时目录: {tempExe}");

                var psi = new ProcessStartInfo
                {
                    FileName = tempExe,
                    Arguments = $"\"{installPath}\"",
                    CreateNoWindow = false,
                    UseShellExecute = false
                };
                Process.Start(psi);
                return;
            }

            Log($"[Step 1/7] 安装路径: {installPath}");
            Console.WriteLine($"[Step 1/7] 安装路径: {installPath}");

            // 2. 跳转资源管理器窗口
            Log("[Step 2/7] 跳转资源管理器窗口...");
            Console.WriteLine("[Step 2/7] 跳转资源管理器窗口...");
            NavigateExplorerAway(installPath);

            // 3. Restart Manager 关闭占用进程
            Log("[Step 3/7] 关闭占用进程...");
            Console.WriteLine("[Step 3/7] 关闭占用进程...");
            CloseHandlesWithRestartManager(installPath);

            // 4. 兜底：终止所有模块路径在安装目录内的进程
            Log("[Step 4/7] 终止残留进程...");
            Console.WriteLine("[Step 4/7] 终止残留进程...");
            KillProcessesInDirectory(installPath);
            GracefulKill("Update"); // Velopack Update.exe 可能在别处运行
            Thread.Sleep(2000);

            // 5. Velopack 卸载引擎
            if (File.Exists(updateExe))
            {
                Log("[Step 5/7] 调用 Velopack 卸载引擎...");
                Console.WriteLine("[Step 5/7] 调用 Velopack 卸载引擎...");
                RunProcess(updateExe, "--uninstall");
                Thread.Sleep(3000);
            }
            else
            {
                Log("[Step 5/7] Update.exe 不存在，跳过 Velopack 卸载");
            }

            // 6. 删除安装目录（先删 uninstaller.exe，再删整个目录）
            Log("[Step 6/7] 删除安装目录...");
            Console.WriteLine("[Step 6/7] 删除安装目录...");
            string originUninstaller = Path.Combine(installPath, "uninstaller.exe");
            if (File.Exists(originUninstaller))
            {
                try { File.Delete(originUninstaller); Log("已删除安装目录中的 uninstaller.exe"); }
                catch (Exception ex) { Log($"删除 uninstaller.exe 失败: {ex.Message}"); }
            }
            DeleteDirectory(installPath);

            // 7. 清理注册表和快捷方式
            Log("[Step 7/7] 清理系统残留...");
            Console.WriteLine("[Step 7/7] 清理系统残留...");
            CleanRegistry();
            CleanShortcuts();

            Log("卸载流程结束");
            Console.WriteLine("========================================");
            Console.WriteLine("           卸载完成");
            Console.WriteLine("========================================");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey(true);
        }

        static void CloseHandlesWithRestartManager(string path)
        {
            var sessionKey = new StringBuilder(256);
            int result = RmStartSession(out uint sessionHandle, 0, sessionKey);
            if (result != 0)
            {
                Log($"Restart Manager 会话启动失败: 0x{result:X8}");
                Console.WriteLine($"  -> Restart Manager 会话启动失败: 0x{result:X8}");
                return;
            }

            try
            {
                string[] resources = new[] { path };
                result = RmRegisterResources(sessionHandle, (uint)resources.Length, resources, 0, IntPtr.Zero, 0, IntPtr.Zero);
                if (result != 0)
                {
                    Log($"Restart Manager 注册资源失败: 0x{result:X8}");
                    Console.WriteLine($"  -> Restart Manager 注册资源失败: 0x{result:X8}");
                    return;
                }

                uint needed = 0, count = 0, rebootReasons = 0;
                result = RmGetList(sessionHandle, out needed, ref count, null, out rebootReasons);
                if (result == 234) // ERROR_MORE_DATA
                {
                    var processes = new RM_PROCESS_INFO[needed];
                    count = needed;
                    result = RmGetList(sessionHandle, out _, ref count, processes, out rebootReasons);

                    if (result == 0 && count > 0)
                    {
                        Log($"Restart Manager 发现 {count} 个占用进程:");
                        for (int i = 0; i < count; i++)
                            Log($"  - PID {processes[i].Process.dwProcessId}: {processes[i].strAppName}");

                        result = RmShutdown(sessionHandle, 0, IntPtr.Zero);
                        if (result == 0)
                        {
                            Log("占用进程已关闭");
                            Console.WriteLine($"  -> 已关闭 {count} 个占用进程");
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            Log($"Restart Manager 关闭失败: 0x{result:X8}");
                            Console.WriteLine($"  -> Restart Manager 关闭失败: 0x{result:X8}");
                        }
                    }
                    else if (count == 0)
                    {
                        Log("无进程占用安装目录");
                    }
                }
                else if (result == 0)
                {
                    Log("无进程占用安装目录");
                }
                else
                {
                    Log($"RmGetList 返回未知错误: 0x{result:X8}");
                    Console.WriteLine($"  -> RmGetList 返回未知错误: 0x{result:X8}");
                }
            }
            finally
            {
                RmEndSession(sessionHandle);
            }
        }

        static void KillProcessesInDirectory(string dirPath)
        {
            string normalizedDir = dirPath.TrimEnd('\\', '/');
            var allProcs = Process.GetProcesses();
            int killed = 0;
            foreach (var proc in allProcs)
            {
                try
                {
                    string? exePath = proc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath) &&
                        exePath.StartsWith(normalizedDir + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"发现占用进程: {proc.ProcessName}.exe (PID {proc.Id}) -> {exePath}");
                        Console.WriteLine($"  -> 发现占用进程: {proc.ProcessName}.exe (PID {proc.Id})");
                        try
                        {
                            if (proc.CloseMainWindow() && proc.WaitForExit(3000))
                            {
                                Log($"{proc.ProcessName}.exe (PID {proc.Id}) 已优雅关闭");
                                killed++;
                                continue;
                            }
                        }
                        catch (Exception ex) { Log($"优雅关闭失败: {ex.Message}"); }

                        try
                        {
                            proc.Kill();
                            proc.WaitForExit(3000);
                            Log($"已强制终止 {proc.ProcessName}.exe (PID {proc.Id})");
                            killed++;
                        }
                        catch (Exception ex) { Log($"强制终止失败: {ex.Message}"); }
                    }
                }
                catch
                {
                    // 无权限访问某些系统进程，忽略
                }
            }
            Log($"共终止 {killed} 个占用进程");
            if (killed > 0)
                Console.WriteLine($"  -> 已终止 {killed} 个占用进程");
        }

        static void GracefulKill(string name)
        {
            var procs = Process.GetProcessesByName(name);
            Log($"发现 {procs.Length} 个 {name}.exe 进程");
            if (procs.Length == 0) return;

            Console.WriteLine($"  -> 发现 {procs.Length} 个 {name}.exe 进程");
            foreach (var proc in procs)
            {
                try
                {
                    if (proc.CloseMainWindow() && proc.WaitForExit(3000))
                    {
                        Log($"{name}.exe (PID {proc.Id}) 已优雅关闭");
                        continue;
                    }
                }
                catch (Exception ex) { Log($"优雅关闭 {name}.exe (PID {proc.Id}) 失败: {ex.Message}"); }

                try
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                    Log($"已强制终止 {name}.exe (PID {proc.Id})");
                }
                catch (Exception ex) { Log($"强制终止 {name}.exe (PID {proc.Id}) 失败: {ex.Message}"); }
            }
        }

        static void RunProcess(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(30000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Uninstaller] 运行 {fileName} 失败: {ex.Message}");
            }
        }

        static void NavigateExplorerAway(string path)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                {
                    Log("Shell.Application 不可用");
                    return;
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                var windows = shell.Windows()!;
                int navigated = 0;
                string normalizedPath = Path.GetFullPath(path).TrimEnd('\\', '/');

                for (int i = windows.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var window = windows.Item(i);
                        string? location = window.Document?.Folder?.Self?.Path;
                        if (!string.IsNullOrEmpty(location))
                        {
                            string normalizedLocation = Path.GetFullPath(location).TrimEnd('\\', '/');
                            if (normalizedLocation.StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
                            {
                                string parent = Directory.GetParent(normalizedPath)?.FullName
                                    ?? "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"; // 此电脑
                                window.Navigate(parent);
                                navigated++;
                                Log($"已跳转 Explorer 窗口: {location} -> {parent}");
                            }
                        }
                    }
                    catch
                    {
                        // 某些窗口可能不是文件浏览器，或已关闭
                    }
                }

                if (navigated > 0)
                {
                    Console.WriteLine($"[Uninstaller] 已将 {navigated} 个资源管理器窗口跳转至上级目录");
                    Thread.Sleep(500);
                }
                else
                {
                    Log("没有资源管理器窗口打开安装目录");
                }
            }
            catch (Exception ex)
            {
                Log($"跳转 Explorer 窗口失败: {ex.Message}");
            }
        }

        static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Log("安装目录不存在，跳过删除");
                Console.WriteLine("[Uninstaller] 安装目录不存在，跳过删除");
                return;
            }

            // 去掉只读/隐藏/系统属性后删除所有文件
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            int fileSuccess = 0, fileFail = 0;
            foreach (var file in files)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    fileSuccess++;
                }
                catch (Exception ex)
                {
                    fileFail++;
                    Log($"删除文件失败: {file} -> {ex.Message}");
                }
            }
            Log($"文件删除结果: 成功 {fileSuccess}, 失败 {fileFail}");

            // 由内向外删除子目录
            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length).ToList();
            int dirSuccess = 0, dirFail = 0;
            foreach (var dir in dirs)
            {
                try
                {
                    Directory.Delete(dir, false);
                    dirSuccess++;
                }
                catch (Exception ex)
                {
                    dirFail++;
                    Log($"删除目录失败: {dir} -> {ex.Message}");
                }
            }
            Log($"子目录删除结果: 成功 {dirSuccess}, 失败 {dirFail}");

            // 删除根目录（带重试）
            bool deleted = false;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    Directory.Delete(path, false);
                    deleted = true;
                    Log($"安装目录已删除（第 {attempt} 次尝试）");
                    Console.WriteLine("  -> 安装目录已删除");
                    break;
                }
                catch (Exception ex)
                {
                    Log($"第 {attempt} 次删除根目录失败: {ex.Message}");
                    if (attempt < 5) Thread.Sleep(2000);
                }
            }

            if (!deleted)
            {
                Log("无法删除安装目录根，最终失败");
                Console.WriteLine("  -> 无法删除安装目录: 目录仍被占用");
                try
                {
                    var remaining = Directory.GetFileSystemEntries(path, "*", SearchOption.AllDirectories);
                    Log($"安装目录内剩余 {remaining.Length} 项:");
                    foreach (var r in remaining) Log($"  - {r}");
                }
                catch { }
            }
        }

        static void Log(string message)
        {
            // 不再保存日志文件，仅保留方法签名供后续扩展
        }

        static void CleanRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                if (key == null) return;

                string[] names = new[] { "cpu_net", "cpu_net.exe", "CPU_NET" };
                foreach (var name in names)
                {
                    try
                    {
                        if (key.GetValue(name) != null)
                        {
                            key.DeleteValue(name);
                            Console.WriteLine($"[Uninstaller] 已删除注册表项: {name}");
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }

        static void CleanShortcuts()
        {
            try
            {
                // 启动文件夹
                string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                foreach (var name in new[] { "cpu_net", "cpu_net.exe", "CPU_NET" })
                {
                    string path = Path.Combine(startup, name + ".lnk");
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        Console.WriteLine($"[Uninstaller] 已删除启动快捷方式: {name}.lnk");
                    }
                }

                // 开始菜单
                string programs = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs", "CPU_NET");
                if (Directory.Exists(programs))
                {
                    Directory.Delete(programs, true);
                    Console.WriteLine("[Uninstaller] 已删除开始菜单 CPU_NET");
                }
            }
            catch { /* ignore */ }
        }

        static void SelfDelete()
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            // 启动 cmd 延迟删除自身
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c timeout /t 2 >nul && del \"{exePath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            try { Process.Start(psi); } catch { /* ignore */ }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string strServiceShortName;
            public uint ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public uint dwProcessId;
            public FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }
    }
}
