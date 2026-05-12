using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cpu_net.Services
{
    /// <summary>
    /// 邮件发送服务（使用 MailKit，替代已弃用的 SmtpClient）
    /// </summary>
    public static class EmailService
    {
        /// <summary>
        /// 异步发送邮件
        /// </summary>
        public static async Task<bool> SendAsync(
            string smtpServer,
            int smtpPort,
            string username,
            string password,
            string to,
            string subject,
            string body,
            bool isBodyHtml = false)
        {
            if (string.IsNullOrWhiteSpace(smtpServer) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(to) ||
                string.IsNullOrWhiteSpace(subject))
            {
                LoggingService.WriteTextLog("邮件发送失败: SMTP服务器、发件人、收件人或主题为空", "Log", false);
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(string.Empty, username));
                message.To.Add(new MailboxAddress(string.Empty, to));
                message.Subject = subject;
                message.Body = new TextPart(isBodyHtml ? "html" : "plain")
                {
                    Text = body
                };

                using var client = new SmtpClient();
                // SecureSocketOptions.Auto 会根据端口自动选择加密方式：
                // - 465 端口 → SSL 直接连接
                // - 587 端口 → STARTTLS（先明文再升级）
                // - 25 端口  → 根据服务器能力自动决定
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.Auto);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.WriteTextLog($"邮件发送失败: {ex.Message}", "Log", false);
                return false;
            }
        }

        /// <summary>
        /// 发送低电费提醒邮件
        /// </summary>
        public static async Task<bool> SendElectricityAlertAsync(
            string smtpServer,
            int smtpPort,
            string username,
            string password,
            string to,
            decimal balance,
            decimal threshold,
            string? roomInfo = null,
            string? customSubject = null,
            string? customBody = null)
        {
            string subject = string.IsNullOrWhiteSpace(customSubject) ? "【CPU_NET】电费余额不足提醒" : customSubject;
            string body = string.IsNullOrWhiteSpace(customBody)
                ? $"<html><body><h2>电费余额不足提醒</h2><p>您的电费余额已低于设定阈值，请及时充值。</p><table border='1' cellpadding='8' style='border-collapse:collapse;'><tr><td>当前余额</td><td><b>{balance:F2} 元</b></td></tr><tr><td>提醒阈值</td><td>{threshold:F2} 元</td></tr><tr><td>房间信息</td><td>{roomInfo}</td></tr><tr><td>提醒时间</td><td>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td></tr></table><p style='color:#888;font-size:12px;'>本邮件由 CPU_NET 自动发送，请勿直接回复。</p></body></html>"
                : customBody;

            body = ReplaceTemplateVars(body, new Dictionary<string, string>
            {
                ["balance"] = balance.ToString("F2"),
                ["threshold"] = threshold.ToString("F2"),
                ["roomInfo"] = roomInfo ?? "--",
                ["time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return await SendAsync(smtpServer, smtpPort, username, password, to, subject, body, isBodyHtml: true);
        }

        /// <summary>
        /// 发送未绑定房间提醒邮件
        /// </summary>
        public static async Task<bool> SendRoomNotBoundAlertAsync(
            string smtpServer,
            int smtpPort,
            string username,
            string password,
            string to,
            string studentNo,
            string? customSubject = null,
            string? customBody = null)
        {
            string subject = string.IsNullOrWhiteSpace(customSubject) ? "【CPU_NET】电费查询异常提醒" : customSubject;
            string body = string.IsNullOrWhiteSpace(customBody)
                ? $"<html><body><h2>电费查询异常提醒</h2><p>系统在查询电费时发现您的账号未绑定房间，无法获取电费余额。</p><table border='1' cellpadding='8' style='border-collapse:collapse;'><tr><td>学号</td><td>{studentNo}</td></tr><tr><td>异常类型</td><td><b>未绑定房间</b></td></tr><tr><td>提醒时间</td><td>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td></tr></table><p>请前往能源管控平台绑定房间后重试。</p><p style='color:#888;font-size:12px;'>本邮件由 CPU_NET 自动发送，请勿直接回复。</p></body></html>"
                : customBody;

            body = ReplaceTemplateVars(body, new Dictionary<string, string>
            {
                ["studentNo"] = studentNo,
                ["time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return await SendAsync(smtpServer, smtpPort, username, password, to, subject, body, isBodyHtml: true);
        }

        /// <summary>
        /// 替换模板中的占位符变量
        /// </summary>
        private static string ReplaceTemplateVars(string template, Dictionary<string, string> vars)
        {
            if (string.IsNullOrEmpty(template)) return template;
            foreach (var kvp in vars)
            {
                template = template.Replace($"{{{kvp.Key}}}", kvp.Value);
            }
            return template;
        }
    }
}
