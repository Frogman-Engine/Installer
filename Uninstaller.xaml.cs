using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
/*
 The MIT License

Copyright (c) 2025 by UNKNOWN STRYKER

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/




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


        public void OnClickDisplaySdkVersionList(object sender, RoutedEventArgs e)
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
