using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
foreach (var identity in new[] { "singbox_tun", "v2rayN", "Mihomo", "Clash", "tun0", "Hiddify", "WireGuard Tunnel" })
{
    var adapter = new NetworkAdapterSnapshot(identity, identity, OperationalStatus.Up,
        NetworkInterfaceType.Ethernet, new[] { IPAddress.Parse("172.19.0.1") });
    Check(TunDetectionService.Evaluate(new[] { adapter }).Active, $"活动 TUN 识别 {identity}");
    Check(!TunDetectionService.Evaluate(new[] { adapter with { Status = OperationalStatus.Down } }).Active,
        $"已关闭 TUN {identity}");
    Check(!TunDetectionService.Evaluate(new[] { adapter with { Addresses = Array.Empty<IPAddress>() } }).Active,
        $"未配置地址的网卡 {identity}");
}
Check(TunDetectionService.Evaluate(new[] { new NetworkAdapterSnapshot("用户自定义名称", "Wintun Userspace Tunnel",
    OperationalStatus.Up, NetworkInterfaceType.Ethernet, new[] { IPAddress.Parse("172.19.0.1") }) }).Active,
    "驱动描述识别重命名 TUN");
Check(!TunDetectionService.Evaluate(new[] { new NetworkAdapterSnapshot("Ethernet", "Intel Ethernet",
    OperationalStatus.Up, NetworkInterfaceType.Ethernet, new[] { IPAddress.Parse("10.5.1.2") }) }).Active,
    "普通校园网允许登录");
var guard = new TunLoginGuard();
var events = new List<string>();
Check(guard.ShouldPause(new TunDetectionResult(true, "singbox"), events.Add), "TUN 登录暂停");
Check(guard.ShouldPause(new TunDetectionResult(true, "singbox"), events.Add) && events.Count == 1, "稳定状态日志去重");
Check(!guard.ShouldPause(new TunDetectionResult(false, ""), events.Add) && events.Count == 2, "TUN 关闭恢复");
Check(guard.ShouldPause(new TunDetectionResult(false, "检测失败", false), events.Add), "检测异常暂缓认证");
Check(!guard.ShouldPause(new TunDetectionResult(false, ""), events.Add), "检测恢复后允许认证");

string configPath = Path.Combine(AppContext.BaseDirectory, "settings-regression-" + Guid.NewGuid().ToString("N") + ".yaml");
var store = new SettingsStore(configPath);
store.Save(new SettingModel { Username = "test-student", Password = "test-password", Mode = 1,
    ElectricityEnabled = true, ElectricityCheckMinute = 30, NotifyType = 2, EmailEnabled = true,
    UpdateProxyEnabled = true, UpdateProxyHost = "127.0.0.1", UpdateProxyPort = 8080,
    BackgroundImagePath = "example.png", BackgroundOpacity = 0.5, TestUrl = "http://example.test/check" });
store.Update(settings => { settings.BackgroundImagePath = ""; settings.BackgroundOpacity = 0; return true; });
var saved = store.Read();
Check(saved.BackgroundImagePath == "" && saved.BackgroundOpacity == 0 && saved.Mode == 1 && saved.ElectricityEnabled &&
    saved.EmailEnabled && saved.UpdateProxyEnabled && saved.Username == "test-student", "背景独立保存及清空、零值");
store.Update(settings => { settings.ElectricityEnabled = false; settings.ElectricityCheckMinute = 0; settings.NotifyType = 0; return true; });
saved = store.Read();
Check(!saved.ElectricityEnabled && saved.ElectricityCheckMinute == 0 && saved.NotifyType == 0 && saved.EmailEnabled,
    "电费关闭与零值独立保存");
store.Update(settings => { settings.EmailEnabled = false; settings.EmailPassword = ""; return true; });
store.Update(settings => { settings.UpdateProxyEnabled = false; settings.UpdateProxyHost = ""; settings.UpdateProxyPort = 0; return true; });
store.Update(settings => { settings.Mode = 0; settings.Password = ""; return true; });
saved = store.Read();
Check(!saved.EmailEnabled && !saved.UpdateProxyEnabled && saved.UpdateProxyHost == "" && saved.UpdateProxyPort == 0 &&
    saved.Mode == 0 && saved.Password == "" && saved.TestUrl == "http://example.test/check", "模块顺序保存及配置保留");
string before = File.ReadAllText(configPath);
Check(!store.Update(settings => { settings.Username = "invalid"; return false; }) && File.ReadAllText(configPath) == before,
    "校验失败保持原配置");
await Task.WhenAll(Task.Run(() => store.Update(settings => { settings.EmailSmtpServer = "smtp.test"; return true; })),
    Task.Run(() => store.Update(settings => { settings.BackgroundOpacity = 0.75; return true; })));
Check(store.Read().EmailSmtpServer == "smtp.test" && store.Read().BackgroundOpacity == 0.75, "并发模块更新保留彼此字段");
File.WriteAllText(configPath, "Mode: [broken");
try { store.Update(settings => true); throw new Exception("损坏的配置被覆盖"); }
catch (YamlDotNet.Core.YamlException) { Check(File.ReadAllText(configPath) == "Mode: [broken", "损坏配置保护"); }
File.Delete(configPath);
Console.WriteLine("全部回归检查通过");

sealed class RejectProxy : IWebProxy
{
    public ICredentials? Credentials { get; set; }
    public Uri GetProxy(Uri destination) => throw new Exception("认证请求访问了系统代理");
    public bool IsBypassed(Uri host) => throw new Exception("认证请求访问了系统代理");
}
