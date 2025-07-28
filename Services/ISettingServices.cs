using cpu_net.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpu_net.Services
{
    public interface ISettingService
    {
        SettingModel Settings { get; }
        void SaveSettings();
        void ReloadSettings();
        void Initialize();
    }
}
