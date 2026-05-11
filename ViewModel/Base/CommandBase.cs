using System;
using System.Windows.Input;

namespace cpu_net.ViewModel.Base
{
    public class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public CommandBase(Action executeAction, Func<bool> canExcuteFunc = null)
        {
            ExecuteAction = executeAction;
            CanExecuteFunc = canExcuteFunc;
        }

        public CommandBase(Action<object> executeParaAction, Func<object, bool> canExecuteParaFunc = null)
        {
            ExecuteParaAction = executeParaAction;
            CanExecuteParaFunc = canExecuteParaFunc;
        }

        public bool CanExecute(object parameter = null)
        {
            if (parameter == null)
            {
                return CanExecuteFunc == null || CanExecuteFunc.Invoke();
            }
            return CanExecuteParaFunc == null || CanExecuteParaFunc.Invoke(parameter);
        }

        public void Execute(object parameter = null)
        {
            if (parameter == null)
            {
                ExecuteAction?.Invoke();
            }
            else
            {
                ExecuteParaAction?.Invoke(parameter);
            }
        }

        public Action ExecuteAction { get; set; }
        public Func<bool> CanExecuteFunc { get; set; }
        public Action<object> ExecuteParaAction { get; set; }
        public Func<object, bool> CanExecuteParaFunc { get; set; }
    }
}
