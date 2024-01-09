using System;
using System.IO;
using System.Threading;

namespace cpu_net
{
    internal class Utils
    {
        private static readonly ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();
        public static void LogWrite(Exception ex)
        {
            if (!Directory.Exists("ErrorLog"))
            {
                Directory.CreateDirectory("ErrorLog");
            }
            string Month,Day;
            var now = DateTime.Now;
            if (now.Month < 10)
            {
                Month = "0" + now.Month.ToString();
            }
            else
            {
                Month = now.Month.ToString();
            }
            if (now.Day < 10)
            {
                Day = "0" + now.Day.ToString();
            }
            else
            {
                Day = now.Day.ToString();
            }
            var logpath = @"ErrorLog\" + now.Year + "" + Month + "" + Day + ".log";
            var log = "\r\n----------------------" + DateTime.Now + " --------------------------\r\n"
                      + ex.Message
                      + "\r\n"
                      + ex.InnerException
                      + "\r\n"
                      + ex.StackTrace
                      + "\r\n----------------------footer--------------------------\r\n";
            try
            {
                //设置读写锁为写入模式独占资源，其他写入请求需要等待本次写入结束之后才能继续写入
                LogWriteLock.EnterWriteLock();
                File.AppendAllText(logpath, log);
            }
            finally
            {
                //退出写入模式，释放资源占用
                LogWriteLock.ExitWriteLock();
            }
        }
    }
}
