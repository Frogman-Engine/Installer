using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Installer
{
    /// <summary>
    /// Interaction logic for Uninstaller.xaml
    /// </summary>
    public partial class Uninstaller : Page
    {
        public Uninstaller()
        {
            InitializeComponent();

            this.versionOfInstalledSDKs = new ArrayList();
        }


        private ArrayList versionOfInstalledSDKs;
        public ArrayList ListAllVersionOfInstalledSDKs()
        {
            return versionOfInstalledSDKs;
        }
    }
}
