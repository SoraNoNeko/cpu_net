using cpu_net.Model;
using Hardcodet.Wpf.TaskbarNotification;
using System.Threading.Tasks;
using System.Windows;

namespace cpu_net.Services
{
    /// <summary>
    /// 统一通知服务：支持 Windows 系统通知（托盘气泡）和邮件通知
    /// </summary>
    public static class NotifyService
    {
        /// <summary>
        /// 发送低电费提醒（根据设置自动选择通知方式）
        /// </summary>
        public static async Task SendElectricityAlertAsync(SettingModel setting, decimal balance, string? roomInfo = null)
        {
            string title = "电费余额不足提醒";
            string message = $"您的电费余额已低于设定阈值，当前余额：{balance:F2} 元，请及时充值。";

            // 系统通知
            if (setting.NotifyType == 0 || setting.NotifyType == 2)
            {
                ShowSystemNotify(title, message, BalloonIcon.Warning);
            }

            // 邮件通知
            if (setting.NotifyType == 1 || setting.NotifyType == 2)
            {
                if (!string.IsNullOrWhiteSpace(setting.EmailTo))
                {
                    await EmailService.SendElectricityAlertAsync(
                        setting.EmailSmtpServer, setting.EmailSmtpPort,
                        setting.EmailUsername, setting.EmailPassword,
                        setting.EmailTo, balance, setting.ElectricityThreshold, roomInfo,
                        setting.EmailAlertSubject, setting.EmailAlertBody);
                }
            }
        }

        /// <summary>
        /// 发送未绑定房间提醒
        /// </summary>
        public static async Task SendRoomNotBoundAlertAsync(SettingModel setting, string studentNo)
        {
            string title = "电费查询异常提醒";
            string message = $"学号 {studentNo} 未绑定房间，无法获取电费余额。";

            // 系统通知
            if (setting.NotifyType == 0 || setting.NotifyType == 2)
            {
                ShowSystemNotify(title, message, BalloonIcon.Error);
            }

            // 邮件通知
            if (setting.NotifyType == 1 || setting.NotifyType == 2)
            {
                if (!string.IsNullOrWhiteSpace(setting.EmailTo))
                {
                    await EmailService.SendRoomNotBoundAlertAsync(
                        setting.EmailSmtpServer, setting.EmailSmtpPort,
                        setting.EmailUsername, setting.EmailPassword,
                        setting.EmailTo, studentNo,
                        setting.EmailNotBoundSubject, setting.EmailNotBoundBody);
                }
            }
        }

        /// <summary>
        /// 发送服务器不可达提醒
        /// </summary>
        public static void SendServerUnreachableNotify(string message)
        {
            ShowSystemNotify("电费服务器不可达", message, BalloonIcon.Error);
        }

        /// <summary>
        /// 发送自定义系统通知
        /// </summary>
        public static void ShowSystemNotify(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            try
            {
                if (App.TaskbarIcon != null)
                {
                    App.TaskbarIcon.ShowBalloonTip(title, message, icon);
                }
            }
            catch
            {
                // 静默失败，不影响主流程
            }
        }
    }
}
