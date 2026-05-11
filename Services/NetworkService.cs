using System;
using System.IO;
using System.Net;
using System.Text;

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
        public static string HttpGetRequest(string url)
        {
            try
            {
                using var getResponse = CreateGetHttpWebRequest(url).GetResponse() as HttpWebResponse;
                return GetHttpResponse(getResponse, "GET");
            }
            catch
            {
                return string.Empty;
            }
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
            getRequest.Timeout = 5000;
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
