using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace cpu_net.Model
{
    public class CarriersModel : ObservableObject
    {
        //value of key
        // 0 : Null
        // 1 : cmcc
        // 2 : unicom
        // 3 : telecom
        private int _key;
        public int Key
        {
            get { return _key; }
            set { _key = value; OnPropertyChanged(nameof(Key)); }
        }

        private String _text;
        public String Text
        {
            get { return _text; }
            set { _text = value; OnPropertyChanged(nameof(Text)); }
        }
    }
}
