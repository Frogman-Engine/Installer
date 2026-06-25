using System.Collections;
using System.Diagnostics;
using System.Windows;
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
        }


        public void OnClickDisplayGdkVersionList(object sender, RoutedEventArgs e)
        {
            SdkVersionListBox.Items.Clear();

            foreach (string installedGDK in MainWindow.InstalledVersionsOfGDKs)
            {
                foreach (Release release in DirConfigPage.Releases)
                {
                    Debug.Assert(release.Tag is not null);

                    if (installedGDK.Contains(release.Tag) is true)
                    {
                        string productInfo = $"Github Branch:  {release.ProductBranch}\n";
                        productInfo += $"Is Pre-Release:  {release.PreRelease}\n";
                        productInfo += $"Published at:  {release.PublishedAt}\n";
                        productInfo += $"Release Title:  {release.Name}\n";
                        productInfo += $"Release Tag:  {release.Tag}";
                        SdkVersionListBox.Items.Add(productInfo);
                    }
                }
            }
        }


        private Release targetSDK;
        public Release TargetSDK
        {
            get { return targetSDK; }
        }
        private void OnSelectSdkVersion(object sender, RoutedEventArgs e)
        {
            if (SdkVersionListBox.SelectedItem is null)
            {
                return;
            }

            string? selectedItem = SdkVersionListBox.SelectedItem.ToString();
            if (selectedItem is null)
            {
                return;
            }

            int index = selectedItem.IndexOf("Release Tag:");
            string targetString = selectedItem.Substring(index + "Release Tag:  ".Length);
            Debug.Assert(targetString is not null);
            targetString = "Selected: " + targetString + "";

            MessageBoxResult result = MessageBox.Show(targetString, "GDK Version", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result is MessageBoxResult.No)
            {
                return;
            }

            foreach (Release release in DirConfigPage.Releases)
            {
                Debug.Assert(release.Tag is not null);

                if (selectedItem.Contains(release.Tag) is true)
                {
                    targetSDK = release;
                    return;
                }
            }
        }


        public List<string> ListAllVersionOfInstalledSDKs()
        {
            List<string> versionsOfInstalledSDKs = new List<string>();
            IDictionary environmentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);

            foreach (DictionaryEntry environmentVariable in environmentVariables)
            {
                if ((environmentVariable.Key.ToString()?.StartsWith(DataBase.GDKSystemPathVariableNamePrefix) == true) &&
                    (environmentVariable.Key.ToString()?.EndsWith(DataBase.GDKSystemPathVariableNameSuffix) == true))
                {
                    versionsOfInstalledSDKs.Add($"{environmentVariable.Key.ToString()}={environmentVariable.Value?.ToString()}");
                }
            }

            return versionsOfInstalledSDKs;
        }
    }
}
