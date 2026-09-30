using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;

namespace cpu_net.Services
{
    /// <summary>
    /// 网络请求服务（基于同步 HttpWebRequest 实现）
    /// </summary>
    public static class NetworkService
    {
        /// <summary>
        /// 同步 HTTP GET 请求
        /// </summary>
        public static string HttpGetRequest(string url, Action<string>? diagnostic = null)
        {
            var endpoint = new Uri(url).GetLeftPart(UriPartial.Path);
            var timer = Stopwatch.StartNew();
            diagnostic?.Invoke($"认证请求：{endpoint}；直连；超时 5000 ms");
            try
            {
                using var getResponse = CreateGetHttpWebRequest(url).GetResponse() as HttpWebResponse;
                if (getResponse != null && (int)getResponse.StatusCode >= 300)
                    throw new InvalidOperationException($"{endpoint}；直连；HTTP {(int)getResponse.StatusCode}；认证接口返回重定向；耗时 {timer.ElapsedMilliseconds} ms");
                string content = GetHttpResponse(getResponse, "GET");
                diagnostic?.Invoke($"认证响应：{endpoint}；HTTP {(int?)getResponse?.StatusCode}；耗时 {timer.ElapsedMilliseconds} ms；正文长度 {content.Length}");
                return content;
            }
            catch (WebException ex)
            {
                using var response = ex.Response as HttpWebResponse;
                string reason = response != null ? $"HTTP {(int)response.StatusCode}" : ex.Status switch
                {
                    WebExceptionStatus.NameResolutionFailure => "DNS 解析失败",
                    WebExceptionStatus.ProxyNameResolutionFailure => "代理 DNS 解析失败",
                    WebExceptionStatus.ConnectFailure => "服务器连接失败",
                    WebExceptionStatus.Timeout => "请求超时",
                    WebExceptionStatus.TrustFailure => "证书验证失败",
                    _ => ex.Status.ToString()
                };
                throw new InvalidOperationException($"{endpoint}；直连；{reason}；耗时 {timer.ElapsedMilliseconds} ms" +
                    (ex.InnerException is SocketException socket ? $"；Socket={socket.SocketErrorCode}" : ""));
            }
        }

        public static string DescribeFailure(Exception exception)
        {
            if (exception is OperationCanceledException)
                return "请求超时或被取消（检测超时设置为 5 秒）";
            if (exception is HttpRequestException http)
            {
                string reason = http.HttpRequestError switch
                {
                    HttpRequestError.NameResolutionError => "DNS 解析失败：检查测试域名及 DNS 配置",
                    HttpRequestError.ConnectionError => "连接失败：检查服务器端口、网络路由及系统代理",
                    HttpRequestError.ProxyTunnelError => "代理隧道失败：检查系统代理连接",
                    HttpRequestError.SecureConnectionError => "TLS 连接失败：检查证书及系统时间",
                    _ => "HTTP 请求失败"
                };
                return $"{reason}（{http.HttpRequestError}）；HTTP={http.StatusCode?.ToString() ?? "未收到响应"}" +
                    (http.InnerException is SocketException socket ? $"；Socket={socket.SocketErrorCode}" : "");
            }
            return exception.GetType().Name;
        }

        public static string ExtractJson(string response, string callback)
        {
            string text = response.Trim();
            if (text.StartsWith("{") || text == "null") return text;
            if (!text.StartsWith(callback, StringComparison.Ordinal))
                throw new JsonException("响应不是预期的 JSON 或 JSONP，可能是 HTML 页面或重定向响应");
            text = text.Substring(callback.Length).Trim();
            if (text.EndsWith(";")) text = text.Substring(0, text.Length - 1).TrimEnd();
            if (!text.StartsWith("(") || !text.EndsWith(")"))
                throw new JsonException("JSONP 括号不完整");
            return text.Substring(1, text.Length - 2).Trim();
        }

        /// <summary>
        /// 同步 HTTP POST 请求
        /// </summary>
        public static string HttpPostRequest(string url, string postJsonData)
        {
            try
            {
                using var postResponse = CreatePostHttpWebRequest(url, postJsonData).GetResponse() as HttpWebResponse;
                return GetHttpResponse(postResponse, "POST");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static HttpWebRequest CreateGetHttpWebRequest(string url)
        {
            var getRequest = WebRequest.Create(url) as HttpWebRequest;
            getRequest!.Method = "GET";
            getRequest.Proxy = null;
            getRequest.AllowAutoRedirect = false;
            getRequest.Timeout = 5000;
            getRequest.ReadWriteTimeout = 5000;
            getRequest.ContentType = "text/html;charset=UTF-8";
            getRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return getRequest;
        }

        private static HttpWebRequest CreatePostHttpWebRequest(string url, string postData)
        {
            var postRequest = WebRequest.Create(url) as HttpWebRequest;
            postRequest!.KeepAlive = false;
            postRequest.Timeout = 5000;
            postRequest.Method = "POST";
            postRequest.ContentType = "application/x-www-form-urlencoded";
            postRequest.ContentLength = postData.Length;
            postRequest.AllowWriteStreamBuffering = false;

            using (var writer = new StreamWriter(postRequest.GetRequestStream(), Encoding.ASCII))
            {
                writer.Write(postData);
                writer.Flush();
            }

            return postRequest;
        }

        private static string GetHttpResponse(HttpWebResponse? response, string requestType)
        {
            if (response == null)
            {
                return string.Empty;
            }

            string encoding = "UTF-8";

            if (string.Equals(requestType, "POST", StringComparison.OrdinalIgnoreCase))
            {
                encoding = response.ContentEncoding;
                if (string.IsNullOrEmpty(encoding))
                {
                    encoding = "UTF-8";
                }
            }

            using var reader = new StreamReader(response.GetResponseStream()!, Encoding.GetEncoding(encoding));
            return reader.ReadToEnd();
        }
    }
}
