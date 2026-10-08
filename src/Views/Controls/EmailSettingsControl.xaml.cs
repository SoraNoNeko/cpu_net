using cpu_net.Model;
using cpu_net.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace cpu_net.Views.Controls
{
    public partial class EmailSettingsControl : UserControl
    {
        public EmailSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings(SettingModel setting)
        {
            EnabledCheckBox.IsChecked = setting.EmailEnabled;
            SmtpServerTextBox.Text = setting.EmailSmtpServer;
            SmtpPortTextBox.Text = setting.EmailSmtpPort.ToString();
            UsernameTextBox.Text = setting.EmailUsername;
            PasswordBox.Password = setting.EmailPassword;
            ToTextBox.Text = setting.EmailTo;
            AlertSubjectTextBox.Text = setting.EmailAlertSubject;
            AlertBodyTextBox.Text = setting.EmailAlertBody;
            NotBoundSubjectTextBox.Text = setting.EmailNotBoundSubject;
            NotBoundBodyTextBox.Text = setting.EmailNotBoundBody;
        }

        public bool SaveSettings(SettingModel setting)
        {
            if (EnabledCheckBox.IsChecked == true &&
                (string.IsNullOrWhiteSpace(SmtpServerTextBox.Text) || string.IsNullOrWhiteSpace(ToTextBox.Text) ||
                 !int.TryParse(SmtpPortTextBox.Text, out int port) || port < 1 || port > 65535))
            {
                MessageBox.Show("请输入 SMTP 服务器、收件人及有效端口（1–65535）", "邮件设置");
                return false;
            }
            setting.EmailEnabled = EnabledCheckBox.IsChecked ?? false;
            setting.EmailSmtpServer = SmtpServerTextBox.Text.Trim();
            setting.EmailSmtpPort = int.TryParse(SmtpPortTextBox.Text, out var p) ? p : 587;
            setting.EmailUsername = UsernameTextBox.Text.Trim();
            setting.EmailPassword = PasswordBox.Password;
            setting.EmailTo = ToTextBox.Text.Trim();
            setting.EmailAlertSubject = AlertSubjectTextBox.Text.Trim();
            setting.EmailAlertBody = AlertBodyTextBox.Text.Trim();
            setting.EmailNotBoundSubject = NotBoundSubjectTextBox.Text.Trim();
            setting.EmailNotBoundBody = NotBoundBodyTextBox.Text.Trim();
            return true;
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            TestButton.IsEnabled = false;
            TestButton.Content = "发送中...";

            try
            {
                bool success = await EmailService.SendAsync(
                    SmtpServerTextBox.Text.Trim(),
                    int.TryParse(SmtpPortTextBox.Text, out var p) ? p : 587,
                    UsernameTextBox.Text.Trim(),
                    PasswordBox.Password,
                    ToTextBox.Text.Trim(),
                    "【CPU_NET】测试邮件",
                    "这是一封来自 CPU_NET 的测试邮件。如果您收到此邮件，说明邮件配置正确。",
                    false);

                MessageBox.Show(success ? "测试邮件发送成功" : "测试邮件发送失败，请检查配置", 
                    success ? "成功" : "失败",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestButton.IsEnabled = true;
                TestButton.Content = "发送测试邮件";
            }
        }
    }
}
