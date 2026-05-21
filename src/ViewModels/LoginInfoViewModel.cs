using cpu_net.Model;
using Prism.Mvvm;
using System.ComponentModel;

namespace cpu_net.ViewModel
{
    public class LoginInfoViewModel : BindableBase
    {
        private SettingModel _settingModel;

        public LoginInfoViewModel(SettingModel settingModel)
        {
            _settingModel = settingModel;
            _settingModel.PropertyChanged += (object sender, PropertyChangedEventArgs e) => _settingModel.Save();
        }

        public SettingModel SettingModel
        {
            get => _settingModel;
            set => SetProperty(ref _settingModel, value);
        }
    }
}
