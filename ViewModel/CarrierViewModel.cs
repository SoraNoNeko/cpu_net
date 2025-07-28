using cpu_net.Model;
using cpu_net.Services;
using cpu_net.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpu_net.ViewModel
{
    public class CarrierViewModel : ViewModelBase
    {
        private readonly ISettingService _settingService;
        public CarrierViewModel(ISettingService settingService)
        {
            var _settingService = settingService;
            var settings = _settingService.Settings;
            ComboxList = new ObservableCollection<CarriersModel>() {
          new CarriersModel() { Key = 0,Text = "请选择运营商" },
          new CarriersModel() { Key = 1,Text = "移动" },
          new CarriersModel() { Key = 2,Text = "联通" },
          new CarriersModel() { Key = 3,Text = "电信" },
        };
            if (settings.PathExist())
            {
                ComboxItem = ComboxList[settings.Key];
            }
            else
            {
                ComboxItem = ComboxList[0];
            }

        }

        private CarriersModel comboxItem;
        /// <summary>
        /// 下拉框选中信息
        /// </summary>
        public CarriersModel ComboxItem
        {
            get { return comboxItem; }
            set
            {
                comboxItem = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<CarriersModel> comboxList;
        /// <summary>
        /// 下拉框列表
        /// </summary>
        public ObservableCollection<CarriersModel> ComboxList
        {
            get { return comboxList; }
            set { comboxList = value; OnPropertyChanged(); }
        }
    }
}
