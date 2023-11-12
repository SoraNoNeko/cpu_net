using cpu_net.Model;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Timer = System.Threading.Timer;

namespace cpu_net
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static TaskbarIcon TaskbarIcon;
        SettingModel settingData = new SettingModel();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            TaskbarIcon = (TaskbarIcon)FindResource("Taskbar");
        }
    }
}
