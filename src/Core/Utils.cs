using cpu_net.Services;
using System;

namespace cpu_net
{
    internal static class Utils
    {
        public static void LogWrite(Exception ex)
        {
            LoggingService.WriteErrorLog(ex);
        }
    }
}
