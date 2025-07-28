using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel;
using cpu_net.Views;
using cpu_net.Views.Pages;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Toolkit.Uwp.Notifications;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Navigation.Regions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation.Collections;

namespace cpu_net
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        public static TaskbarIcon TaskbarIcon;
        protected override void OnInitialized()
        {
            // 初始化任务栏图标
            TaskbarIcon = (TaskbarIcon)FindResource("Taskbar");
            base.OnInitialized();

            // 注册事件和Toast通知
            RegisterEvents();
            RegisterToastNotifications();
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册设置服务为单例
            // containerRegistry.RegisterSingleton<ISettingService, SettingService>();
            containerRegistry.RegisterSingleton<ISettingService>(() =>
            {
                var service = new SettingService();
                service.Initialize(); // 确保初始化
                return service;
            });

            // 注册主窗口和主ViewModel
            containerRegistry.Register<MainWindow>();
            containerRegistry.RegisterForNavigation<HomePage>("HomePage");
            containerRegistry.RegisterForNavigation<ConfigurationPage>("ConfigurationPage");
            containerRegistry.Register<MainViewModel>();
            containerRegistry.Register<LoginInfoViewModel>();
            containerRegistry.Register<UserViewModel>();
            containerRegistry.Register<CarrierViewModel>();
            containerRegistry.Register<NotifyIconViewModel>(); // 添加此行
            containerRegistry.Register<ConfigurationViewModel>();
            containerRegistry.Register<AutoStart>();
            containerRegistry.RegisterSingleton<IRegionManager, RegionManager>();
        }
        protected override Window CreateShell()
        {
            // 创建主窗口并显示
            var mainWindow = Container.Resolve<MainWindow>(); 
            try {
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                // 捕获并显示容器解析错误
                MessageBox.Show($"容器解析错误: {ex}");
                throw;
            }
            return mainWindow;
        }

        private void RegisterToastNotifications()
        {
            // 监听通知激活
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                Current.Dispatcher.Invoke(() =>
                {
                    if (Current.MainWindow != null)
                    {
                        Current.MainWindow.Visibility = Visibility.Visible;
                        Current.MainWindow.Show();
                        Current.MainWindow.Activate();
                    }
                });
            };
        }
        private void RegisterEvents()
        {
            // Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // UI线程未捕获异常处理事件
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 非UI线程未捕获异常处理事件
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
            Utils.LogWrite(ex);

        }
    }
}
