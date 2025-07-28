using cpu_net.Model;
using cpu_net.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpu_net.ViewModel
{
    public class LoginInfoViewModel : BindableBase
    {
        private readonly ISettingService _settingService;

        public LoginInfoViewModel(ISettingService settingService)
        {
            _settingService = settingService;

            // 监听设置模型的属性变更
            SettingModel.PropertyChanged += (sender, e) =>
            {
                // 当设置变更时自动保存
                _settingService.SaveSettings();
            };
        }

        public SettingModel SettingModel => _settingService.Settings;
    }
}
