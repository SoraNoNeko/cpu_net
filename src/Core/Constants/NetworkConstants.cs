namespace cpu_net.Constants
{
    /// <summary>
    /// 网络相关常量定义
    /// </summary>
    public static class NetworkConstants
    {
        // 通用
        public const string GoogleDnsIp = "8.8.8.8";
        public const int GoogleDnsPort = 65530;

        // DrCOM 状态查询
        public const string DrComCallback = "dr1002";
        public const string LoginCallback = "dr1004";

        // 宽带模式 (Mode 0)
        public const string BroadbandDrComUrl = "http://172.17.253.3/drcom/chkstatus?callback=dr1002";
        public const string BroadbandLoginBaseUrl = "http://172.17.253.3:801/eportal/portal/login?";
        public const string BroadbandLoginBaseUrlSsl = "https://172.17.253.3:802/eportal/portal/login?";

        // CPU 模式 (Mode 1)
        public const string CpuDrComUrl = "http://192.168.199.21/drcom/chkstatus?callback=dr1002";
        public const string CpuLoginBaseUrl = "http://192.168.199.21:801/eportal/?c=Portal&a=login&callback=dr1004&login_method=1";

        // 公告
        public const string NoticeUrl = "http://10.3.4.106/notice.txt";

        // 外部页面
        public const string TutorialUrl = "https://lic.cpu.edu.cn/ee/c6/c7550a192198/page.htm";
        public const string SelfServiceUrl = "http://192.168.199.70:8080/Self/Dashboard";
        public const string GitHubRepoUrl = "https://github.com/SoraNoNeko/cpu_net";

        // IP 环境识别
        public static class IpPrefixes
        {
            public const string Broadband1 = "10";
            public const string Broadband2 = "192";
            public const string BroadbandSegment1 = "12";
            public const string BroadbandSegment2 = "31";
            public const string BroadbandSegment3 = "33";
        }
    }
}
