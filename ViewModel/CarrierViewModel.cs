using cpu_net.Model;
using cpu_net.ViewModel.Base;
using System.Collections.ObjectModel;

namespace cpu_net.ViewModel
{
    public class CarrierViewModel : ViewModelBase
    {
        private readonly SettingModel _settingData = new SettingModel();

        public CarrierViewModel()
        {
            ComboxList = new ObservableCollection<CarriersModel>
            {
                new CarriersModel { Key = 0, Text = "请选择运营商" },
                new CarriersModel { Key = 1, Text = "移动" },
                new CarriersModel { Key = 2, Text = "联通" },
                new CarriersModel { Key = 3, Text = "电信" },
            };

            if (_settingData.PathExist())
            {
                _settingData = _settingData.Read();
                ComboxItem = ComboxList[_settingData.Key];
            }
            else
            {
                ComboxItem = ComboxList[0];
            }
        }

        private CarriersModel _comboxItem;
        public CarriersModel ComboxItem
        {
            get => _comboxItem;
            set { _comboxItem = value; OnPropertyChanged(); }
        }

        private ObservableCollection<CarriersModel> _comboxList;
        public ObservableCollection<CarriersModel> ComboxList
        {
            get => _comboxList;
            set { _comboxList = value; OnPropertyChanged(); }
        }
    }
}
