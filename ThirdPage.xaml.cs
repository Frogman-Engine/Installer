using System.Net.Http;
using System.IO;
using System.Threading.Tasks;




namespace Installer
{
    /// <summary>
    /// SDK Installation & Download Page
    /// </summary>
    public partial class ThirdPage : System.Windows.Controls.Page
    {
        public ThirdPage()
        {
            InitializeComponent();
            this.DataContext = this;
        }


        public void UpdateProgressBar(uint progress)
        {
            Percent.Text = progress.ToString() + '%';
            InstallationProgressBar.Value = progress;
        }

        public void AppendLog(string message)
        {
            CommandLogTextBox.AppendText(message + "\n");
            CommandLogTextBox.ScrollToEnd();
        }


        public void Install()
        {
            /*
             * 1. Download the Frogman Engine SDK from GitHub.
             * 2. Extract the SDK to the installation directory.
             * 3. Add the SDK to the system PATH.
             * 4. Install the Frogman Engine SDK.
             */
        }


        // Event handler for the "Install" button.
    }
}
