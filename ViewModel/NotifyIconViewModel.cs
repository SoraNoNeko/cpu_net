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
                Application.Current.MainWindow.Visibility = Visibility.Visible;
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.WindowState = _windowState;
                Application.Current.MainWindow.Activate();
            }
        };

        public ICommand HideWindowCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                _windowState = Application.Current.MainWindow.WindowState;
                Application.Current.MainWindow.Visibility = Visibility.Hidden;
            }
        };

        public ICommand ExitApplicationCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                Application.Current.MainWindow.Visibility = Visibility.Visible;
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.WindowState = _windowState;
                Application.Current.MainWindow.Activate();
                Application.Current.MainWindow.Close();
            }
        };

        public class DelegateCommand : ICommand
        {
            public Action CommandAction { get; set; }
            public Func<bool> CanExecuteFunc { get; set; }

            public void Execute(object parameter)
            {
                CommandAction();
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
