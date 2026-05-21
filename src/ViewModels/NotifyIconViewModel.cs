using cpu_net.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace cpu_net.ViewModel
{
    public class NotifyIconViewModel
    {
        private WindowState _windowState;

        public ICommand ShowWindowCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow == null) return;
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.Show();
                mainWindow.WindowState = _windowState;
                mainWindow.Activate();
            }
        };

        public ICommand HideWindowCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow == null) return;
                _windowState = mainWindow.WindowState;
                mainWindow.Visibility = Visibility.Hidden;
            }
        };

        public ICommand ExitApplicationCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Visibility = Visibility.Visible;
                    mainWindow.Show();
                    mainWindow.WindowState = _windowState;
                    mainWindow.Activate();
                    mainWindow.Close();
                }
                else
                {
                    Application.Current?.Shutdown();
                }
            }
        };

        public ICommand CheckUpdateCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                _ = UpdateService.CheckAndPromptUpdateAsync(Application.Current?.MainWindow);
            }
        };

        public class DelegateCommand : ICommand
        {
            public Action CommandAction { get; set; }
            public Func<bool> CanExecuteFunc { get; set; }

            public void Execute(object parameter)
            {
                CommandAction?.Invoke();
            }

            public bool CanExecute(object parameter)
            {
                return CanExecuteFunc == null || CanExecuteFunc();
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}
