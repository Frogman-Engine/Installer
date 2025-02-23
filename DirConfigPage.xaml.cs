using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;




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


    /// <summary>
    /// Path Configuration Page
    /// </summary>
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

            this.releases = FetchSdkVersionListFromGitHub();

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

            // Fetch the SDK version list from GitHub.
            foreach (Release release in releases)
            {
                string productInfo = $"Github Branch:  {release.ProductBranch};\nIs Pre-Release:  {release.PreRelease};\nPublished At:  {release.PublishedAt};";
                productInfo += $"\nRelease Title (Version):  {release.Name}";
                SdkVersionListBox.Items.Add(productInfo);
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

            int index = selectedItem.IndexOf("Release Title (Version):");
            string targetString = selectedItem.Substring(index + "Release Title (Version):  ".Length);
            Debug.Assert(targetString is not null);
            string sdkVersion = targetString;
            targetString = "Selected: " + targetString + "";

            MessageBoxResult result = MessageBox.Show(targetString, "SDK Version", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result is MessageBoxResult.No)
            {
                return;
            }

            foreach (Release release in releases)
            {
                if (release.Name == sdkVersion)
                {
                    targetSDK = release;
                    return;
                }
            }
        }


        private List<Release> releases;
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