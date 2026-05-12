using cpu_net.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cpu_net.Services
{
    /// <summary>
    /// 电费查询服务
    /// </summary>
    public class ElectricityService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// 真实电费查询API地址
        /// </summary>
        public const string RealApiUrl = "http://10.200.13.18:8899/api/wxapp/my3";

        /// <summary>
        /// 签名固定密钥
        /// </summary>
        public const string SignKey = "ruGQQlUhZxJhQqKY8lYGYcN6UJWwNRL3";

        /// <summary>
        /// 签名固定appId
        /// </summary>
        public const string AppId = "XzJ0YzzEtk0HbVOk";

        /// <summary>
        /// 默认电费查询URL模板（备用）
        /// </summary>
        public const string DefaultQueryUrlTemplate = "http://10.200.13.18:8899/h5/#/?no={0}";

        /// <summary>
        /// 默认API端点猜测列表（备用）
        /// </summary>
        public static readonly string[] DefaultApiEndpoints = new[]
        {
            "/api/student/balance",
            "/api/room/balance",
            "/api/user/balance",
            "/api/elec/balance",
            "/api/query/balance",
            "/api/account/balance",
        };

        /// <summary>
        /// 生成请求签名（SHA1）
        /// </summary>
        public static string GenerateSign(string studentNo)
        {
            var pairs = new (string key, string value)[]
            {
                ("appId", AppId),
                ("openId", studentNo),
                ("stuNo", "true")
            };

            // 按 key 字母排序
            Array.Sort(pairs, (a, b) => string.Compare(a.key, b.key, StringComparison.Ordinal));

            var sb = new StringBuilder();
            foreach (var (key, value) in pairs)
            {
                sb.Append(key.ToLowerInvariant());
                sb.Append('=');
                sb.Append(value);
                sb.Append('&');
            }
            sb.Append("key=");
            sb.Append(SignKey);

            string signStr = sb.ToString();
            using var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(signStr));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 检查电费服务器是否可达
        /// </summary>
        public static async Task<(bool Reachable, string Message)> CheckServerReachableAsync(string? apiBaseUrl = null)
        {
            string baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl)
                ? "http://10.200.13.18:8899"
                : apiBaseUrl.TrimEnd('/');

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync(baseUrl, cts.Token);
                if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
                {
                    return (true, "服务器可达");
                }
                return (false, $"服务器返回状态码: {(int)response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                if (ex.InnerException is System.Net.Sockets.SocketException socketEx)
                {
                    return (false, $"无法连接到服务器: {socketEx.Message} (错误码: {socketEx.SocketErrorCode})");
                }
                return (false, $"网络请求失败: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "连接超时: 服务器未响应");
            }
            catch (Exception ex)
            {
                return (false, $"检查连接时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 查询电费余额
        /// </summary>
        /// <param name="studentNo">学号</param>
        /// <param name="apiBaseUrl">API基础地址（可选，默认http://10.200.13.18:8899）</param>
        /// <param name="apiEndpoint">API端点路径（可选，自动探测）</param>
        /// <returns>查询结果</returns>
        public async Task<ElectricityResult> QueryAsync(
            string studentNo,
            string? apiBaseUrl = null,
            string? apiEndpoint = null)
        {
            string baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl)
                ? "http://10.200.13.18:8899"
                : apiBaseUrl.TrimEnd('/');

            LoggingService.WriteTextLog($"[电费] 开始查询，学号={studentNo}", "Log", false);

            // 先检查服务器是否可达
            var (reachable, message) = await CheckServerReachableAsync(baseUrl);
            if (!reachable)
            {
                LoggingService.WriteTextLog($"[电费] 服务器不可达: {message}", "Log", false);
                return new ElectricityResult
                {
                    Success = false,
                    ErrorMessage = $"无法访问电费服务器: {message}",
                    RawResponse = $"ServerCheck: {message}"
                };
            }

            // 优先使用真实API
            var realResult = await QueryRealApiAsync(studentNo);
            if (realResult.Success || realResult.IsRoomNotBound)
            {
                return realResult;
            }

            // 如果指定了API端点，尝试直接调用
            if (!string.IsNullOrWhiteSpace(apiEndpoint))
            {
                return await TryQueryApiAsync(baseUrl, apiEndpoint, studentNo);
            }

            // 尝试默认API端点列表
            foreach (var endpoint in DefaultApiEndpoints)
            {
                var result = await TryQueryApiAsync(baseUrl, endpoint, studentNo);
                if (result.Success || result.IsRoomNotBound)
                {
                    return result;
                }
            }

            // API调用全部失败，不再回退到网页解析（网页已改为DAS登录页，无法提取）
            realResult.ErrorMessage = realResult.ErrorMessage ?? "所有API端点均无法获取电费数据";
            LoggingService.WriteTextLog($"[电费] 查询失败: {realResult.ErrorMessage}", "Log", false);
            return realResult;
        }

        /// <summary>
        /// 调用真实API查询电费
        /// </summary>
        private async Task<ElectricityResult> QueryRealApiAsync(string studentNo)
        {
            var result = new ElectricityResult();
            try
            {
                string sign = GenerateSign(studentNo);
                var requestBody = new
                {
                    appId = AppId,
                    openId = studentNo,
                    stuNo = true,
                    sign = sign
                };

                var request = new HttpRequestMessage(HttpMethod.Post, RealApiUrl);
                request.Headers.TryAddWithoutValidation("Referer", "http://10.200.13.18:8899/h5/");
                request.Headers.TryAddWithoutValidation("Origin", "http://10.200.13.18:8899");
                var formData = new Dictionary<string, string>
                {
                    { "appId", AppId },
                    { "openId", studentNo },
                    { "stuNo", "true" },
                    { "sign", sign }
                };
                request.Content = new FormUrlEncodedContent(formData);

                using var response = await _httpClient.SendAsync(request);
                string responseText = await response.Content.ReadAsStringAsync();
                result.RawResponse = responseText;

                if (!response.IsSuccessStatusCode)
                {
                    int statusCode = (int)response.StatusCode;
                    if (statusCode == 415)
                    {
                        var now = DateTime.Now;
                        bool isMaintenance = now.Hour >= 23 || now.Hour < 8;
                        if (isMaintenance)
                        {
                            result.ErrorMessage = "服务器维护中（23:00-08:00），请稍后再试";
                            LoggingService.WriteTextLog($"[电费] 真实API返回415，判断为服务器维护时段 ({now:HH:mm})", "Log", false);
                            return result;
                        }
                    }
                    result.ErrorMessage = $"API返回状态码: {statusCode}";
                    LoggingService.WriteTextLog($"[电费] 真实API返回状态码异常: {statusCode}", "Log", false);
                    return result;
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    result.ErrorMessage = "API返回空响应";
                    LoggingService.WriteTextLog("[电费] 真实API返回空响应", "Log", false);
                    return result;
                }

                return ParseRealApiResponse(responseText, result);
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = $"请求异常: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] 真实API请求异常: {ex.Message}", "Log", false);
                return result;
            }
            catch (TaskCanceledException ex)
            {
                result.ErrorMessage = $"请求超时: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] 真实API请求超时: {ex.Message}", "Log", false);
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"未知异常: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] 真实API请求未知异常: {ex.Message}", "Log", false);
                return result;
            }
        }

        /// <summary>
        /// 解析真实API响应
        /// </summary>
        private ElectricityResult ParseRealApiResponse(string json, ElectricityResult result)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                // 真实API返回JSON数组
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var array = doc.RootElement.EnumerateArray();
                    if (!array.MoveNext())
                    {
                        // 空数组表示未绑定房间
                        result.IsRoomNotBound = true;
                        result.ErrorMessage = "未绑定房间（返回空数组）";
                        return result;
                    }

                    var first = array.Current;

                    // 检查错误码
                    if (first.TryGetProperty("errCode", out var errCodeProp) && errCodeProp.GetInt32() != 0)
                    {
                        string errMsg = first.TryGetProperty("errMsg", out var errMsgProp)
                            ? errMsgProp.GetString() ?? "未知错误"
                            : "未知错误";
                        result.ErrorMessage = errMsg;
                        return result;
                    }

                    // 提取房间信息
                    if (first.TryGetProperty("name", out var nameProp))
                    {
                        result.RoomInfo = nameProp.GetString();
                    }

                    // 提取电费余额（meters数组中的remain字段）和可用电量（amount字段）
                    if (first.TryGetProperty("meters", out var metersProp) && metersProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var meter in metersProp.EnumerateArray())
                        {
                            if (meter.TryGetProperty("remain", out var remainProp))
                            {
                                if (remainProp.ValueKind == JsonValueKind.Number)
                                {
                                    result.Balance = remainProp.GetDecimal();
                                }
                                else if (remainProp.ValueKind == JsonValueKind.String)
                                {
                                    if (decimal.TryParse(remainProp.GetString(), out var d))
                                    {
                                        result.Balance = d;
                                    }
                                }
                            }
                            if (meter.TryGetProperty("amount", out var amountProp))
                            {
                                if (amountProp.ValueKind == JsonValueKind.Number)
                                {
                                    result.Degrees = amountProp.GetDecimal();
                                }
                                else if (amountProp.ValueKind == JsonValueKind.String)
                                {
                                    if (decimal.TryParse(amountProp.GetString(), out var d))
                                    {
                                        result.Degrees = d;
                                    }
                                }
                            }
                            if (result.Balance.HasValue || result.Degrees.HasValue)
                            {
                                break; // 取第一个电表的数据
                            }
                        }
                    }

                    // 备选：尝试直接从根对象读取
                    if (!result.Balance.HasValue && first.TryGetProperty("remain", out var rootRemain))
                    {
                        if (rootRemain.ValueKind == JsonValueKind.Number)
                        {
                            result.Balance = rootRemain.GetDecimal();
                        }
                        else if (rootRemain.ValueKind == JsonValueKind.String)
                        {
                            if (decimal.TryParse(rootRemain.GetString(), out var d))
                            {
                                result.Balance = d;
                            }
                        }
                    }

                    if (result.Balance.HasValue)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.ErrorMessage = "API响应中未找到电费余额字段";
                        LoggingService.WriteTextLog("[电费] API响应中未找到电费余额字段", "Log", false);
                    }
                }
                else
                {
                    // 可能是错误对象
                    return ParseJsonResponse(json, result);
                }
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"JSON解析失败: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] API响应JSON解析失败: {ex.Message}", "Log", false);
            }

            return result;
        }

        /// <summary>
        /// 尝试通过API查询电费
        /// </summary>
        private async Task<ElectricityResult> TryQueryApiAsync(string baseUrl, string endpoint, string studentNo)
        {
            var result = new ElectricityResult();
            try
            {
                string url = $"{baseUrl}{endpoint}?no={Uri.EscapeDataString(studentNo)}";
                string response = await _httpClient.GetStringAsync(url);
                result.RawResponse = response;

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.ErrorMessage = "API返回空响应";
                    return result;
                }

                // 尝试解析JSON
                return ParseJsonResponse(response, result);
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = $"请求异常: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] 备用API请求异常: {ex.Message}", "Log", false);
                return result;
            }
            catch (TaskCanceledException ex)
            {
                result.ErrorMessage = $"请求超时: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] 备用API请求超时: {ex.Message}", "Log", false);
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"未知异常: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] 备用API请求未知异常: {ex.Message}", "Log", false);
                return result;
            }
        }

        /// <summary>
        /// 尝试从页面HTML中提取电费信息
        /// </summary>
        private async Task<ElectricityResult> TryQueryFromPageAsync(string baseUrl, string studentNo)
        {
            var result = new ElectricityResult();
            try
            {
                string pageUrl = string.Format(DefaultQueryUrlTemplate, Uri.EscapeDataString(studentNo));
                string html = await _httpClient.GetStringAsync(pageUrl);
                result.RawResponse = html;

                if (string.IsNullOrWhiteSpace(html))
                {
                    result.ErrorMessage = "页面返回空内容";
                    return result;
                }

                // 检测未绑定房间
                string[] notBoundKeywords = new[]
                {
                    "未绑定", "请先绑定", "绑定房间", "未关联", "no room", "not bound"
                };

                foreach (var keyword in notBoundKeywords)
                {
                    if (html.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsRoomNotBound = true;
                        result.ErrorMessage = "检测到未绑定房间";
                        return result;
                    }
                }

                // 尝试从HTML中提取余额（多种正则模式）
                result.Balance = ExtractBalanceFromHtml(html);
                if (result.Balance.HasValue)
                {
                    result.Success = true;
                }
                else
                {
                    result.ErrorMessage = "无法从页面中提取电费余额";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"页面解析异常: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 解析JSON响应
        /// </summary>
        private ElectricityResult ParseJsonResponse(string json, ElectricityResult result)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 检测错误码/未绑定
                if (root.TryGetProperty("code", out var codeProp))
                {
                    int code = codeProp.GetInt32();
                    if (code != 0 && code != 200)
                    {
                        string msg = root.TryGetProperty("message", out var msgProp)
                            ? msgProp.GetString() ?? "未知错误"
                            : "未知错误";

                        if (msg.Contains("绑定", StringComparison.OrdinalIgnoreCase) ||
                            msg.Contains("房间", StringComparison.OrdinalIgnoreCase))
                        {
                            result.IsRoomNotBound = true;
                        }

                        result.ErrorMessage = msg;
                        return result;
                    }
                }

                // 尝试多种余额字段名
                string[] balanceFields = new[] { "balance", "amount", "money", "elecBalance", "remain", "surplus", "fee" };
                decimal? balance = null;

                foreach (var field in balanceFields)
                {
                    if (root.TryGetProperty(field, out var prop))
                    {
                        if (prop.ValueKind == JsonValueKind.Number)
                        {
                            balance = prop.GetDecimal();
                            break;
                        }
                        else if (prop.ValueKind == JsonValueKind.String)
                        {
                            if (decimal.TryParse(prop.GetString(), out var d))
                            {
                                balance = d;
                                break;
                            }
                        }
                    }
                }

                // 如果根没有，尝试在 data 字段中查找
                if (!balance.HasValue && root.TryGetProperty("data", out var dataProp))
                {
                    foreach (var field in balanceFields)
                    {
                        if (dataProp.TryGetProperty(field, out var prop))
                        {
                            if (prop.ValueKind == JsonValueKind.Number)
                            {
                                balance = prop.GetDecimal();
                                break;
                            }
                            else if (prop.ValueKind == JsonValueKind.String)
                            {
                                if (decimal.TryParse(prop.GetString(), out var d))
                                {
                                    balance = d;
                                    break;
                                }
                            }
                        }
                    }

                    // 尝试获取房间信息
                    if (dataProp.TryGetProperty("roomName", out var roomProp) ||
                        dataProp.TryGetProperty("room", out roomProp) ||
                        dataProp.TryGetProperty("roomInfo", out roomProp))
                    {
                        result.RoomInfo = roomProp.GetString();
                    }
                }

                if (balance.HasValue)
                {
                    result.Balance = balance;
                    result.Success = true;
                }
                else
                {
                    result.ErrorMessage = "JSON中未找到余额字段";
                }
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"JSON解析失败: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 从HTML中提取余额
        /// </summary>
        private decimal? ExtractBalanceFromHtml(string html)
        {
            // 模式1: xxx元
            var match = Regex.Match(html, @"余额\s*[：:]\s*([0-9]+\.?[0-9]*)\s*元");
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var d1))
                return d1;

            // 模式2: 金额数字
            match = Regex.Match(html, @"电费\s*[：:]\s*([0-9]+\.?[0-9]*)");
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var d2))
                return d2;

            // 模式3: balance相关
            match = Regex.Match(html, @"balance['""\s]*[:=]\s*['""]?([0-9]+\.?[0-9]*)");
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var d3))
                return d3;

            // 模式4: 通用金额模式（在中文上下文中）
            match = Regex.Match(html, @"([0-9]+\.[0-9]{1,2})\s*元");
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var d4))
                return d4;

            return null;
        }
    }
}
