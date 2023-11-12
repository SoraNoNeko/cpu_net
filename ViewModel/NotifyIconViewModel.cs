using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace cpu_net.ViewModel
{
    public class NotifyIconViewModel
    {
        /// <summary>
        /// 激活窗口
        /// </summary>
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        Application.Current.MainWindow.Visibility = Visibility.Visible;
                        Application.Current.MainWindow.Show();
                        Application.Current.MainWindow.Activate();
                    }
                };
            }
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public ICommand HideWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        Application.Current.MainWindow.Visibility = Visibility.Hidden;
                    }
                };
            }
        }


        /// <summary>
        /// 关闭软件
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
            }
        }


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
