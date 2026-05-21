using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using SetupBootstrapper.Models;

namespace SetupBootstrapper
{
    /// <summary>
    /// 安装目标目录检测器
    /// </summary>
    public static class DirectoryChecker
    {
        /// <summary>
        /// 检测目标目录状态。
        /// 优化顺序：
        /// 1. 空路径/不存在 → Empty（最快）
        /// 2. 目录存在 → 先 O(1) 检查旧版本标记（避免写权限测试的 IO 延迟）
        /// 3. 无旧版本 → 检查写权限
        /// 4. 写权限 OK → 枚举判断是否为空
        /// </summary>
        public static DirectoryStatus CheckDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return DirectoryStatus.Empty;

            // 路径不存在 → 检查父目录写权限即可
            if (!Directory.Exists(path))
            {
                var writeStatus = CheckWriteAccess(path);
                return writeStatus ?? DirectoryStatus.Empty;
            }

            // 目录存在：先 O(1) 检查旧版本标记（无 IO 写入，最快）
            if (File.Exists(Path.Combine(path, "cpu_net.exe")) ||
                File.Exists(Path.Combine(path, "Update.exe")) ||
                File.Exists(Path.Combine(path, "RELEASES")) ||
                Directory.Exists(Path.Combine(path, "current")) ||
                Directory.Exists(Path.Combine(path, "packages")))
            {
                return DirectoryStatus.OldVersion;
            }

            // 再检查写权限（需要写入临时文件，可能慢）
            var dirWriteStatus = CheckWriteAccess(path);
            if (dirWriteStatus != null)
                return dirWriteStatus.Value;

            // 慢速路径：目录是否为空（需要枚举，但 .Any() 短路）
            var entries = Directory.EnumerateFileSystemEntries(path);
            if (!entries.Any())
                return DirectoryStatus.Empty;

            return DirectoryStatus.HasOtherFiles;
        }

        /// <summary>
        /// 检测对目标路径的写入权限。返回 null 表示权限足够。
        /// </summary>
        private static DirectoryStatus? CheckWriteAccess(string path)
        {
            try
            {
                // 如果目录不存在，尝试创建它（需要父目录的写入权限）
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    // 创建成功但目录是空的，返回 Empty
                    Directory.Delete(path);
                    return DirectoryStatus.Empty;
                }

                // 目录存在，尝试写入临时文件
                string tempFile = Path.Combine(path, $".cpu_net_write_test_{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(tempFile, string.Empty);
                    File.Delete(tempFile);
                    return null; // 权限足够
                }
                catch (UnauthorizedAccessException)
                {
                    return DirectoryStatus.RequiresAdmin;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return DirectoryStatus.RequiresAdmin;
            }
            catch (Exception)
            {
                // 其他异常（如路径非法、磁盘错误等），保守处理为需要管理员
                return DirectoryStatus.RequiresAdmin;
            }
        }

        /// <summary>
        /// 当前进程是否以管理员身份运行
        /// </summary>
        public static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
