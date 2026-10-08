using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace cpu_net.Services
{
    public sealed record TunDetectionResult(bool Active, string Detail, bool Reliable = true);

    public sealed record NetworkAdapterSnapshot(string Name, string Description,
        OperationalStatus Status, NetworkInterfaceType Type, IReadOnlyList<IPAddress> Addresses);

    public static class TunDetectionService
    {
        // 驱动描述用于识别用户重命名的网卡；只检查活动网卡，不依赖代理进程是否运行。
        private static readonly Regex TunIdentity = new Regex(
            @"wintun|sing[\s_-]?box|clash|mihomo|v2ray|xray|nekoray|hiddify|tun2socks|wireguard|\btun(?:nel)?(?:\d+|\b|_)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static TunDetectionResult Detect()
        {
            try
            {
                return Evaluate(NetworkInterface.GetAllNetworkInterfaces()
                    .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up &&
                        TunIdentity.IsMatch(adapter.Name + " " + adapter.Description))
                    .Select(adapter =>
                    new NetworkAdapterSnapshot(adapter.Name, adapter.Description, adapter.OperationalStatus,
                        adapter.NetworkInterfaceType,
                        adapter.GetIPProperties().UnicastAddresses.Select(address => address.Address).ToArray())));
            }
            catch (NetworkInformationException)
            {
                return new TunDetectionResult(false, "暂时无法读取网卡状态，等待下次检测", false);
            }
        }

        public static TunDetectionResult Evaluate(IEnumerable<NetworkAdapterSnapshot> adapters)
        {
            foreach (var adapter in adapters)
            {
                if (adapter.Status != OperationalStatus.Up || adapter.Type == NetworkInterfaceType.Loopback)
                    continue;
                if (!TunIdentity.IsMatch(adapter.Name + " " + adapter.Description)) continue;
                if (!adapter.Addresses.Any(IsUsableAddress)) continue;
                return new TunDetectionResult(true, $"{adapter.Name}（{adapter.Description}）");
            }
            return new TunDetectionResult(false, string.Empty);
        }

        private static bool IsUsableAddress(IPAddress address)
        {
            if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                return false;
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
                return !address.IsIPv6LinkLocal && !address.IsIPv6Multicast;
            byte[] bytes = address.GetAddressBytes();
            return address.AddressFamily == AddressFamily.InterNetwork && !(bytes[0] == 169 && bytes[1] == 254);
        }
    }

    public sealed class TunLoginGuard
    {
        private readonly object _gate = new object();
        private string _lastReason = string.Empty;

        public bool ShouldPause(TunDetectionResult result, Action<string> report)
        {
            lock (_gate)
            {
                string reason = result.Active ? $"检测到 TUN 模式：{result.Detail}，已暂停校园网登录尝试。"
                    : !result.Reliable ? $"{result.Detail}，已暂停校园网登录尝试。" : string.Empty;
                if (reason != _lastReason)
                {
                    report(reason.Length > 0 ? reason : "TUN 检测已通过，恢复校园网登录检测。");
                    _lastReason = reason;
                }
                return reason.Length > 0;
            }
        }
    }
}
