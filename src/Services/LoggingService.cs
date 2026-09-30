using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace cpu_net.Services
{
    /// <summary>
    /// 日志服务：提供线程安全的日志写入与读取功能
    /// </summary>
    public static class LoggingService
    {
        private static readonly SemaphoreSlim LogSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 日志根目录，固定为应用程序所在目录，避免相对路径导致写入系统目录的风险
        /// </summary>
        private static readonly string LogBaseDir = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 写入异常日志到 ErrorLog 目录（按天分文件）
        /// </summary>
        public static void WriteErrorLog(Exception ex)
        {
            string errorLogDir = Path.Combine(LogBaseDir, "ErrorLog");

            var now = DateTime.Now;
            string fileName = $"{now.Year}{now.Month:D2}{now.Day:D2}.log";
            string logPath = Path.Combine(errorLogDir, fileName);

            var log = Environment.NewLine + "----------------------" + DateTime.Now + " --------------------------" + Environment.NewLine
                      + ex.Message
                      + Environment.NewLine
                      + ex.InnerException
                      + Environment.NewLine
                      + ex.StackTrace
                      + Environment.NewLine + "----------------------footer--------------------------" + Environment.NewLine;

            LogSemaphore.Wait();
            try
            {
                Directory.CreateDirectory(errorLogDir);
                File.AppendAllText(logPath, log);
            }
            catch (Exception failure)
            {
                WriteFallback(log, failure);
            }
            finally
            {
                LogSemaphore.Release();
            }
        }

        /// <summary>
        /// 写入运行时日志（同步入口，内部转异步，不阻塞调用方）
        /// </summary>
        public static void WriteTextLog(string log, string logName, bool testMode)
        {
            _ = WriteTextLogAsync(log, logName, testMode);
        }

        /// <summary>
        /// 异步写入运行时日志
        /// </summary>
        public static async Task WriteTextLogAsync(string log, string logName, bool testMode)
        {
            if (!testMode && logName == "RecordLog")
            {
                return;
            }

            string logDir = Path.Combine(LogBaseDir, logName);

            var now = DateTime.Now;
            string fileName = logName == "RecordLog"
                ? $"{now.Year}{now.Month:D2}{now.Day:D2}.log"
                : $"{now.Year}{now.Month:D2}.log";

            string logPath = Path.Combine(logDir, fileName);
            var formattedLog = $"{DateTime.Now:M-d HH:mm:ss}  {log}{Environment.NewLine}";

            await LogSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(logDir);
                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        await File.AppendAllTextAsync(logPath, formattedLog).ConfigureAwait(false);
                        break;
                    }
                    catch (IOException ex) when (attempt < 3 &&
                        ((ex.HResult & 0xffff) == 32 || (ex.HResult & 0xffff) == 33))
                    {
                        await Task.Delay(100 * (attempt + 1)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception failure)
            {
                WriteFallback(formattedLog, failure);
            }
            finally
            {
                LogSemaphore.Release();
            }
        }

        /// <summary>
        /// 读取日志文件最后 N 行
        /// </summary>
        public static string ReadLogText(string logName, int maxLines = 200)
        {
            var now = DateTime.Now;
            string fileName = logName == "RecordLog"
                ? $"{now.Year}{now.Month:D2}{now.Day:D2}.log"
                : $"{now.Year}{now.Month:D2}.log";
            string logPath = Path.Combine(LogBaseDir, logName, fileName);

            if (!File.Exists(logPath))
            {
                return string.Empty;
            }

            LogSemaphore.Wait();
            try
            {
                var allLines = File.ReadAllLines(logPath);
                var lines = allLines.Length > maxLines
                    ? allLines.Skip(allLines.Length - maxLines)
                    : allLines;
                return string.Join(Environment.NewLine, lines);
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                LogSemaphore.Release();
            }
        }

        // 独立的按进程备用文件，日志失败时不递归调用日志服务。
        private static void WriteFallback(string entry, Exception failure)
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "cpu_net", "FallbackLogs");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, $"{DateTime.Now:yyyyMMdd}-{Environment.ProcessId}.log"),
                    $"{DateTime.Now:O} 日志写入失败：{failure.GetType().Name} (0x{failure.HResult:X8}){Environment.NewLine}{entry}");
            }
            catch
            {
                Debug.WriteLine("CPU_NET 日志及备用日志写入失败。");
            }
        }
    }
}
