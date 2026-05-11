using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace cpu_net.Services
{
    /// <summary>
    /// 日志服务：提供线程安全的日志写入与读取功能
    /// </summary>
    public static class LoggingService
    {
        private static readonly ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();

        /// <summary>
        /// 写入异常日志到 ErrorLog 目录（按天分文件）
        /// </summary>
        public static void WriteErrorLog(Exception ex)
        {
            if (!Directory.Exists("ErrorLog"))
            {
                Directory.CreateDirectory("ErrorLog");
            }

            var now = DateTime.Now;
            string fileName = $"{now.Year}{now.Month:D2}{now.Day:D2}.log";
            string logPath = Path.Combine("ErrorLog", fileName);

            var log = "\r\n----------------------" + DateTime.Now + " --------------------------\r\n"
                      + ex.Message
                      + "\r\n"
                      + ex.InnerException
                      + "\r\n"
                      + ex.StackTrace
                      + "\r\n----------------------footer--------------------------\r\n";

            try
            {
                LogWriteLock.EnterWriteLock();
                File.AppendAllText(logPath, log);
            }
            finally
            {
                LogWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 写入运行时日志
        /// </summary>
        /// <param name="log">日志内容</param>
        /// <param name="logName">日志目录名（Log / RecordLog）</param>
        /// <param name="testMode">是否处于测试模式（RecordLog 仅在测试模式下写入）</param>
        public static void WriteTextLog(string log, string logName, bool testMode)
        {
            if (!testMode && logName == "RecordLog")
            {
                return;
            }

            if (!Directory.Exists(logName))
            {
                Directory.CreateDirectory(logName);
            }

            var now = DateTime.Now;
            string fileName = logName == "RecordLog"
                ? $"{now.Year}{now.Month:D2}{now.Day:D2}.log"
                : $"{now.Year}{now.Month:D2}.log";

            string logPath = Path.Combine(logName, fileName);
            var formattedLog = $"{DateTime.Now:M-d HH:mm:ss}  {log}\r\n";

            try
            {
                LogWriteLock.EnterWriteLock();
                File.AppendAllText(logPath, formattedLog);
            }
            finally
            {
                LogWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 读取日志文件最后 10 行
        /// </summary>
        /// <param name="logName">日志目录名</param>
        /// <returns>日志文本</returns>
        public static string ReadLogText(string logName)
        {
            var now = DateTime.Now;
            // RecordLog 按天分文件，Log 按月分文件，与 WriteTextLog 保持一致
            string fileName = logName == "RecordLog"
                ? $"{now.Year}{now.Month:D2}{now.Day:D2}.log"
                : $"{now.Year}{now.Month:D2}.log";
            string logPath = Path.Combine(logName, fileName);

            if (!File.Exists(logPath))
            {
                return string.Empty;
            }

            try
            {
                LogWriteLock.EnterReadLock();
                var lines = File.ReadLines(logPath);
                var last10Lines = lines.Skip(Math.Max(0, lines.Count() - 10)).ToArray();
                return string.Join(Environment.NewLine, last10Lines);
            }
            finally
            {
                LogWriteLock.ExitReadLock();
            }
        }
    }
}
