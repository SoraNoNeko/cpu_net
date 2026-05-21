using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cpu_net
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static TaskbarIcon TaskbarIcon;

        private static Mutex? _mutex;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 防止多实例启动：即使注册表中有多个启动项，也只会运行一个实例
            const string mutexName = "cpu_net_SingleInstance_Mutex";
            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // 已有实例在运行，激活已有窗口后退出当前实例
                ActivateExistingInstance();
                Shutdown();
                return;
            }

            // 初始化自动更新服务（必须在任何 UI 操作之前）
            UpdateService.Initialize();

            // 覆盖安装后检测并修复指向旧路径的开机启动项
            try { new AutoStart().FixStaleAutoStartEntry(); } catch { /* 忽略 */ }

            RegisterEvents();
            base.OnStartup(e);

            // 单文件发布下，XAML 资源可能无法通过 FindResource 解析，全部改为代码创建
            var notifyVm = new NotifyIconViewModel();

            var sysTrayMenu = new System.Windows.Controls.ContextMenu();
            sysTrayMenu.Items.Add(new MenuItem { Header = "显示窗口", Command = notifyVm.ShowWindowCommand });
            sysTrayMenu.Items.Add(new MenuItem { Header = "关闭窗口", Command = notifyVm.HideWindowCommand });
            sysTrayMenu.Items.Add(new Separator());
            sysTrayMenu.Items.Add(new MenuItem { Header = "检查更新", Command = notifyVm.CheckUpdateCommand });
            sysTrayMenu.Items.Add(new Separator());
            sysTrayMenu.Items.Add(new MenuItem { Header = "退出", Command = notifyVm.ExitApplicationCommand });

            TaskbarIcon = new TaskbarIcon
            {
                ToolTipText = "CPU_NET",
                IconSource = new BitmapImage(new Uri("pack://application:,,,/assets/shinnku.ico")),
                DataContext = notifyVm,
                ContextMenu = sysTrayMenu,
                DoubleClickCommand = notifyVm.ShowWindowCommand
            };

            // 后台静默检查更新（不阻塞启动）
            _ = UpdateService.CheckForUpdatesSilentAsync();
        }

        /// <summary>
        /// 激活已运行的实例窗口
        /// </summary>
        private static void ActivateExistingInstance()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                foreach (var process in processes)
                {
                    if (process.Id != currentProcess.Id)
                    {
                        var hWnd = process.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            if (IsIconic(hWnd))
                                ShowWindowAsync(hWnd, SW_RESTORE);
                            SetForegroundWindow(hWnd);
                        }
                        break;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }
        private void RegisterEvents()
        {
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;//Task异常 

            //UI线程未捕获异常处理事件（UI主线程）
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var exception = e.Exception as Exception;
                if (exception != null)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                e.SetObserved();
            }
        }

        //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)      
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                //ignore
            }
        }

        //UI线程未捕获异常处理事件（UI主线程）
        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                HandleException(e.Exception);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                e.Handled = true;
            }
        }
        private static void HandleException(Exception ex)
        {
            MessageBox.Show("出错了，请与开发人员联系：" + ex.Message);
            //记录日志
            LoggingService.WriteErrorLog(ex);
        }
    }
}
