using CommunityToolkit.Mvvm.ComponentModel;

namespace cpu_net.Model
{
    public class CarriersModel : ObservableObject
    {
        // Key 映射：0 = 未选择, 1 = 移动(cmcc), 2 = 联通(unicom), 3 = 电信(telecom)
        private int _key;
        public int Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        private string _text;
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }
    }
}
