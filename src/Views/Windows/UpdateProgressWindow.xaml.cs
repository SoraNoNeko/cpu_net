using System.Windows;

namespace cpu_net.Views.Windows
{
    /// <summary>
    /// UpdateProgressWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置进度条进度（0-100）
        /// </summary>
        public void SetProgress(int percentage)
        {
            if (Dispatcher.CheckAccess())
            {
                ProgressBar.Value = percentage;
                ProgressText.Text = $"{percentage}%";
            }
            else
            {
                Dispatcher.Invoke(() => SetProgress(percentage));
            }
        }
    }
}
