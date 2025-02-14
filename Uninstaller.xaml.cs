using System.Windows.Controls;




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

            this.versionOfInstalledSDKs = new List<string>();
        }


        private List<string> versionOfInstalledSDKs;
        public List<string> ListAllVersionOfInstalledSDKs()
        {
            return versionOfInstalledSDKs;
        }
    }
}
