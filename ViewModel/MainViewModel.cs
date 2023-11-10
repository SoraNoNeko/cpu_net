using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cpu_net.Views.Pages;
using System.Windows.Controls;

namespace cpu_net.ViewModel
{
    public class MainViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Boolean isAutoRunCheck;

        public Boolean IsAutoRunCheck
        {
            get { return isAutoRunCheck; }
            set { isAutoRunCheck = value; OnPropertyChanged(nameof(IsAutoRunCheck)); }
        }
        private string? txtLog;

        public String? TxtLog
        {
            get { return txtLog; }
            set { txtLog = value; OnPropertyChanged(nameof(TxtLog)); }
        }

        public void Info(string message)
        {
             TxtLog = TxtLog + message + Environment.NewLine;
        }
    }

}
