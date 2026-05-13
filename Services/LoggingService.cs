using System;
using System.IO;
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
            if (!Directory.Exists(errorLogDir))
            {
                Directory.CreateDirectory(errorLogDir);
            }

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

            try
            {
                LogSemaphore.Wait();
                File.AppendAllText(logPath, log);
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
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var now = DateTime.Now;
            string fileName = logName == "RecordLog"
                ? $"{now.Year}{now.Month:D2}{now.Day:D2}.log"
                : $"{now.Year}{now.Month:D2}.log";

            string logPath = Path.Combine(logDir, fileName);
            var formattedLog = $"{DateTime.Now:M-d HH:mm:ss}  {log}{Environment.NewLine}";

            await LogSemaphore.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(logPath, formattedLog);
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
        }
    }
}
