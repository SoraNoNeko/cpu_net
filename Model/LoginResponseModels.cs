namespace cpu_net.Model
{
    // JSON 反序列化模型，用于校园网登录接口响应解析

    /// <summary>DrCOM 状态查询 IP 结果</summary>
    public class DrComIpResult
    {
        public string ss5 { get; set; } = string.Empty;
    }

    /// <summary>登录结果（result=1 成功，result=0 失败）</summary>
    public class LoginResult
    {
        public int result { get; set; }
    }

    /// <summary>登录错误代码（ret_code）</summary>
    public class LoginErrorCode
    {
        public int ret_code { get; set; }
    }

    /// <summary>登录错误信息</summary>
    public class LoginErrorMessage
    {
        public string msg { get; set; } = string.Empty;
    }
}
