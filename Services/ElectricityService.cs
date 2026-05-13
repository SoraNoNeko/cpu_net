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
        /// 判断当前是否处于服务器维护时段（23:00-08:00）
        /// </summary>
        private static bool IsMaintenanceTime() => DateTime.Now.Hour >= 23 || DateTime.Now.Hour < 8;

        /// <summary>
        /// 默认电费查询URL模板（备用）
        /// </summary>
        public const string DefaultQueryUrlTemplate = "http://10.200.13.18:8899/h5/#/?no={0}";

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
        /// <returns>查询结果</returns>
        public async Task<ElectricityResult> QueryAsync(string studentNo)
        {
            LoggingService.WriteTextLog($"[电费] 开始查询，学号={studentNo}", "Log", false);

            // 先检查服务器是否可达
            var (reachable, message) = await CheckServerReachableAsync();
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

            // 仅使用真实API，不再重试备用端点
            var realResult = await QueryRealApiAsync(studentNo);
            if (!realResult.Success && !realResult.IsRoomNotBound)
            {
                LoggingService.WriteTextLog($"[电费] 查询失败: {realResult.ErrorMessage}", "Log", false);
            }
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
                string jsonBody = JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                string responseText = await response.Content.ReadAsStringAsync();
                result.RawResponse = responseText;

                if (!response.IsSuccessStatusCode)
                {
                    int statusCode = (int)response.StatusCode;
                    if (IsMaintenanceTime())
                    {
                        result.ErrorMessage = "服务器维护中（23:00-08:00），请稍后再试";
                        LoggingService.WriteTextLog($"[电费] 真实API返回{statusCode}，处于维护时段 ({DateTime.Now:HH:mm})", "Log", false);
                        return result;
                    }
                    result.ErrorMessage = $"API返回状态码: {statusCode}";
                    LoggingService.WriteTextLog($"[电费] 真实API返回状态码异常: {statusCode}", "Log", false);
                    return result;
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    if (IsMaintenanceTime())
                    {
                        result.ErrorMessage = "服务器维护中（23:00-08:00），请稍后再试";
                        LoggingService.WriteTextLog($"[电费] 真实API返回空响应，处于维护时段 ({DateTime.Now:HH:mm})", "Log", false);
                        return result;
                    }
                    result.IsRoomNotBound = true;
                    result.ErrorMessage = "未绑定房间（返回空响应）";
                    LoggingService.WriteTextLog("[电费] 真实API返回空响应，判断为未绑定房间", "Log", false);
                    return result;
                }

                return ParseRealApiResponse(responseText, result);
            }
            catch (HttpRequestException ex)
            {
                if (IsMaintenanceTime())
                {
                    result.ErrorMessage = "服务器维护中（23:00-08:00），请稍后再试";
                    LoggingService.WriteTextLog($"[电费] 真实API请求异常，处于维护时段: {ex.Message}", "Log", false);
                }
                else
                {
                    result.ErrorMessage = $"请求异常: {ex.Message}";
                    LoggingService.WriteTextLog($"[电费] 真实API请求异常: {ex.Message}", "Log", false);
                }
                return result;
            }
            catch (TaskCanceledException ex)
            {
                if (IsMaintenanceTime())
                {
                    result.ErrorMessage = "服务器维护中（23:00-08:00），请稍后再试";
                    LoggingService.WriteTextLog($"[电费] 真实API请求超时，处于维护时段: {ex.Message}", "Log", false);
                }
                else
                {
                    result.ErrorMessage = $"请求超时: {ex.Message}";
                    LoggingService.WriteTextLog($"[电费] 真实API请求超时: {ex.Message}", "Log", false);
                }
                return result;
            }
            catch (Exception ex)
            {
                if (IsMaintenanceTime())
                {
                    result.ErrorMessage = "服务器维护中（23:00-08:00），请稍后再试";
                    LoggingService.WriteTextLog($"[电费] 真实API未知异常，处于维护时段: {ex.Message}", "Log", false);
                }
                else
                {
                    result.ErrorMessage = $"未知异常: {ex.Message}";
                    LoggingService.WriteTextLog($"[电费] 真实API请求未知异常: {ex.Message}", "Log", false);
                }
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
                        if (IsMaintenanceTime())
                        {
                            result.ErrorMessage = "服务器维护中（23:00-08:00），请稍后再试";
                            LoggingService.WriteTextLog($"[电费] 真实API返回空数组，处于维护时段 ({DateTime.Now:HH:mm})", "Log", false);
                            return result;
                        }
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

                    // 提取房间信息（组合 buiName + name，例如 "E1 0404房间"）
                    string? roomName = null;
                    string? buiName = null;
                    if (first.TryGetProperty("name", out var nameProp))
                        roomName = nameProp.GetString();
                    if (first.TryGetProperty("buiName", out var buiNameProp))
                        buiName = buiNameProp.GetString();
                    result.RoomInfo = $"{buiName} {roomName}".Trim();

                    // 提取电费余额（meters数组中的amount字段）和可用电量（remain字段）
                    if (first.TryGetProperty("meters", out var metersProp) && metersProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var meter in metersProp.EnumerateArray())
                        {
                            if (meter.TryGetProperty("amount", out var amountProp))
                            {
                                if (amountProp.ValueKind == JsonValueKind.Number)
                                {
                                    result.Balance = amountProp.GetDecimal();
                                }
                                else if (amountProp.ValueKind == JsonValueKind.String)
                                {
                                    if (decimal.TryParse(amountProp.GetString(), out var d))
                                    {
                                        result.Balance = d;
                                    }
                                }
                            }
                            if (meter.TryGetProperty("remain", out var remainProp))
                            {
                                if (remainProp.ValueKind == JsonValueKind.Number)
                                {
                                    result.Degrees = remainProp.GetDecimal();
                                }
                                else if (remainProp.ValueKind == JsonValueKind.String)
                                {
                                    if (decimal.TryParse(remainProp.GetString(), out var d))
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
                    if (!result.Balance.HasValue && first.TryGetProperty("amount", out var rootAmount))
                    {
                        if (rootAmount.ValueKind == JsonValueKind.Number)
                        {
                            result.Balance = rootAmount.GetDecimal();
                        }
                        else if (rootAmount.ValueKind == JsonValueKind.String)
                        {
                            if (decimal.TryParse(rootAmount.GetString(), out var d))
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
                    // 非数组响应，视为异常
                    result.ErrorMessage = "API返回非预期格式";
                    LoggingService.WriteTextLog("[电费] API返回非数组格式", "Log", false);
                }
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"JSON解析失败: {ex.Message}";
                LoggingService.WriteTextLog($"[电费] API响应JSON解析失败: {ex.Message}", "Log", false);
            }

            return result;
        }


    }
}
