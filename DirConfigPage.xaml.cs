using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
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
    public struct Release
    {
        public Release()
        {
            this.Tag = null;
            this.ProductBranch = null;
            this.Name = null;
            this.Draft = false;
            this.PreRelease = false;
            this.CreatedAt = null;
            this.PublishedAt = null;
            this.TarballUrl = null;
            this.ZipballUrl = null;
        }
        public string? Tag;
        public string? ProductBranch;
        public string? Name;
        public bool Draft;
        public bool PreRelease;
        public string? CreatedAt;
        public string? PublishedAt;
        public string? TarballUrl;
        public string? ZipballUrl;
    }




    public partial class DirConfigPage : System.Windows.Controls.Page
    {
        public DirConfigPage(string appVersion)
        {
            InitializeComponent();

            this.httpClient = new HttpClient();
            this.userAgentHeader = new ProductInfoHeaderValue("Frogman_Engine_SDK_Installer", appVersion);

            // Add a User-Agent header to the HttpClient instance. UserAgent is a metada that identifies the client application.
            this.httpClient.DefaultRequestHeaders.UserAgent.Add(userAgentHeader);

            this.url = DataBase.FrogmanEngineGdkReleaseListUrl;

            releases = FetchSdkVersionListFromGitHub();

            this.dialog = new OpenFolderDialog();
            this.dialog.ValidateNames = false;
        }


        private OpenFolderDialog dialog;
        public void OnClickBrowseButton(object sender, RoutedEventArgs e)
        {
            bool? result = this.dialog.ShowDialog();
            Debug.Assert(result is not null);
            if (result is false)
            {
                return;
            }

            InstallationPathTextBox.Text = dialog.FolderName;
        }


        public void OnClickDisplaySdkVersionList(object sender, RoutedEventArgs e)
        {
            SdkVersionListBox.Items.Clear();

            if (MainWindow.InstalledVersionsOfGDKs.Count is 0)
            {
                foreach (Release release in releases)
                {
                    Debug.Assert(release.Tag is not null);
                    string productInfo = $"Github Branch:  {release.ProductBranch}\n";
                    productInfo += $"Is Pre-Release:  {release.PreRelease}\n";
                    productInfo += $"Published at:  {release.PublishedAt}\n";
                    productInfo += $"Release Title:  {release.Name}\n";
                    productInfo += $"Release Tag:  {release.Tag}\n";
                    SdkVersionListBox.Items.Add(productInfo);
                }
                return;
            }

            foreach (Release release in releases)
            {
                Debug.Assert(release.Tag is not null);
                foreach (string installedGDK in MainWindow.InstalledVersionsOfGDKs)
                {
                    if (installedGDK.Contains(release.Tag) is false)
                    {
                        string productInfo = $"Github Branch:  {release.ProductBranch}\n";
                        productInfo += $"Is Pre-Release:  {release.PreRelease}\n";
                        productInfo += $"Published at:  {release.PublishedAt}\n";
                        productInfo += $"Release Title:  {release.Name}\n";
                        productInfo += $"Release Tag:  {release.Tag}\n";
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

            int index = selectedItem.IndexOf("Release Title:");
            string targetString = selectedItem.Substring(index + "Release Title:  ".Length);
            Debug.Assert(targetString is not null);
            targetString = "Selected: " + targetString + "";

            MessageBoxResult result = MessageBox.Show(targetString, "GDK Version", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result is MessageBoxResult.No)
            {
                return;
            }

            foreach (Release release in releases)
            {
                Debug.Assert(release.Tag is not null);

                if (selectedItem.Contains(release.Tag) is true)
                {
                    targetSDK = release;
                    return;
                }
            }
        }


        private static List<Release> releases = new List<Release>();
        public static List<Release> Releases
        {
            get { return releases; }
        }
        private HttpClient httpClient;
        private ProductInfoHeaderValue userAgentHeader;
        private string url;
        public List<Release> FetchSdkVersionListFromGitHub()
        {
            // Send a GET request to the specified URL and get the response.
            HttpResponseMessage response = httpClient.GetAsync(url).Result;

            // Check if the response indicates success (status code 200-299).
            if (response.IsSuccessStatusCode is false)
            {
                // Display an error message if the request was not successful.
                string errorMessage = $"Failed to fetch releases: {response.StatusCode}";
                string errorTitle = "Error, HTTP Request Failed!";
                MessageBox.Show(errorMessage, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.FailFast(errorTitle + " " + errorMessage);
            }

            // Read the response content as a string.
            string json = response.Content.ReadAsStringAsync().Result;

            // Parse the JSON string into a JsonDocument.
            JsonElement releases = JsonDocument.Parse(json).RootElement;

            List<Release> list_of_releases = new List<Release>();
            Release release_info = new Release();

            // Iterate over each release in the JSON array.
            foreach (JsonElement release in releases.EnumerateArray())
            {
                release_info.Tag = release.GetProperty("tag_name").GetString();
                release_info.ProductBranch = release.GetProperty("target_commitish").GetString();
                release_info.Name = release.GetProperty("name").GetString();
                release_info.Draft = release.GetProperty("draft").GetBoolean();
                release_info.PreRelease = release.GetProperty("prerelease").GetBoolean();
                release_info.CreatedAt = release.GetProperty("created_at").GetString();
                release_info.PublishedAt = release.GetProperty("published_at").GetString();
                release_info.TarballUrl = release.GetProperty("tarball_url").GetString();
                release_info.ZipballUrl = release.GetProperty("zipball_url").GetString();
                list_of_releases.Add(release_info);
            }
            return list_of_releases;
        }
    }
}