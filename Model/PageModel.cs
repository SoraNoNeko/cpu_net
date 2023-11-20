using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace cpu_net.Model
{
    public class PageModel : ObservableObject
    {
        private const string FILE_NAME = "data.dat";
        private string _pageName;
        public string PageName
        {
            get { return _pageName; }
            set { _pageName = value; OnPropertyChanged(); }
        }
        private Brush _home_B;
        public Brush Home_B
        {
            get { return _home_B; }
            set
            {
                _home_B = value;OnPropertyChanged();
            }
        }

        private Brush _conf_B;
        public Brush Conf_B
        {
            get { return _conf_B; }
            set
            {
                _conf_B = value;OnPropertyChanged();
            }
        }

        /*
        public void save()
        {
            PageModel pageModel = new PageModel();
            pageModel.PageName = PageName;
            pageModel.Home_B = Home_B;
            pageModel.Conf_B = Conf_B;
            using (FileStream fs = new FileStream(FILE_NAME, FileMode.Create))
            {
                using (BinaryWriter w = new BinaryWriter(fs))
                {
                    byte[] data = Encoding.UTF8.GetBytes(pageModel);
                    w.Write(pageModel);
                }
            }
        }*/
    }
}
