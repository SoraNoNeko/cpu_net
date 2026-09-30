using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using cpu_net.Services;
using cpu_net.Model;

static void Check(bool passed, string name)
{
    if (!passed) throw new Exception(name);
    Console.WriteLine($"PASS {name}");
}

foreach (string response in new[] {
    "dr1004({\"result\":1,\"msg\":\"Portal协议认证成功！\"});",
    " \r\ndr1004({\"result\":1});\r\n ",
    "dr1004({\"result\":1})", "{\"result\":1}" })
{
    Check(JsonSerializer.Deserialize<LoginResult>(NetworkService.ExtractJson(response, "dr1004"))?.result == 1,
        "认证成功响应解析");
}
try { NetworkService.ExtractJson("<html>portal</html>", "dr1004"); throw new Exception("未拒绝 HTML"); }
catch (JsonException) { Console.WriteLine("PASS HTML 响应诊断"); }
Check(NetworkService.DescribeFailure(new HttpRequestException(HttpRequestError.NameResolutionError)).Contains("NameResolutionError"), "DNS 分类");
Check(NetworkService.DescribeFailure(new TaskCanceledException()).Contains("超时"), "超时分类");

var proxy = WebRequest.DefaultWebProxy;
WebRequest.DefaultWebProxy = new RejectProxy();
try
{
    foreach (var item in new[] { (200, "dr1004({\"result\":1});"), (503, "unavailable"), (302, "redirect"), (200, "") })
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
            byte[] body = Encoding.UTF8.GetBytes(item.Item2);
            byte[] header = Encoding.ASCII.GetBytes($"HTTP/1.1 {item.Item1} Test\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header);
            await stream.WriteAsync(body);
        });
        var diagnostics = new List<string>();
        try
        {
            string result = NetworkService.HttpGetRequest($"http://127.0.0.1:{port}/login?user_password=secret", diagnostics.Add);
            Check(item.Item1 == 200 && result == item.Item2, "HTTP 直连及正文");
        }
        catch (InvalidOperationException ex)
        {
            Check(item.Item1 != 200 && ex.Message.Contains($"HTTP {item.Item1}") && !ex.Message.Contains("secret"), "HTTP 错误诊断与凭据保护");
        }
        finally { listener.Stop(); }
        await server.WaitAsync(TimeSpan.FromSeconds(5));
        Check(diagnostics.All(x => !x.Contains("secret")), "诊断地址凭据保护");
    }
}
finally { WebRequest.DefaultWebProxy = proxy; }

string logName = "Regression-" + Guid.NewGuid().ToString("N");
await Task.WhenAll(Enumerable.Range(0, 12).Select(async worker =>
{
    for (int i = 0; i < 25; i++)
    {
        await LoggingService.WriteTextLogAsync($"entry-{worker}-{i}", logName, false);
        await Task.Run(() => LoggingService.ReadLogText(logName, 1000));
    }
}));
Check(LoggingService.ReadLogText(logName, 1000).Split(Environment.NewLine).Length == 300, "并发日志读写完整性");
string logPath = Path.Combine(AppContext.BaseDirectory, logName, $"{DateTime.Now:yyyyMM}.log");
using (var locked = new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    var write = LoggingService.WriteTextLogAsync("retry-entry", logName, false);
    await Task.Delay(150);
    locked.Dispose();
    await write;
}
Check(LoggingService.ReadLogText(logName, 1000).Contains("retry-entry"), "共享冲突重试恢复");
string marker = "fallback-" + Guid.NewGuid().ToString("N");
using (var locked = new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    await LoggingService.WriteTextLogAsync(marker, logName, false);
string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "cpu_net", "FallbackLogs", $"{DateTime.Now:yyyyMMdd}-{Environment.ProcessId}.log");
Check(File.ReadAllText(fallback).Contains(marker), "持续占用的备用日志与任务异常处理");
Console.WriteLine("全部回归检查通过");

sealed class RejectProxy : IWebProxy
{
    public ICredentials? Credentials { get; set; }
    public Uri GetProxy(Uri destination) => throw new Exception("认证请求访问了系统代理");
    public bool IsBypassed(Uri host) => throw new Exception("认证请求访问了系统代理");
}
