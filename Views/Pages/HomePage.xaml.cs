using System;
using System.Windows.Controls;

namespace cpu_net.Views.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        readonly String noticeText = @"欢迎使用本程序，
本程序旨在帮助药大学生自动登录校园网，免受断网困扰
23级本本答疑群：819668931
使用本程序前，需要绑定运营商账号，
绑定方法点击下方按钮可以显示
本程序所有数据均存放在本地，
如有使用困难请带着右边的日志截图去本本群询问
注意事项：
如果需要自动登录功能，请勾选定时登录
其功能为每6小时刷新登录状态
设置中的自动登录为启动时自动登录
需要电脑不关机并连宿舍网
尝试使用CPU模式强制连接CPU时可能会卡住，是正常现象";

        public HomePage()
        {
            InitializeComponent();
            textNotice.Text = noticeText;
        }
    }
}
