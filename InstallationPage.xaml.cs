#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Media.Animation;




namespace Installer
{
    /// <summary>
    /// SDK Installation & Download Page
    /// </summary>
    public partial class InstallationPage : System.Windows.Controls.Page
    {
        public InstallationPage(string appVersion)
        {
            InitializeComponent();
            this.DataContext = this;

            this.httpClient = new HttpClient();
            this.userAgentHeader = new ProductInfoHeaderValue("Frogman_Engine_SDK_Installer", appVersion);

            // Add a User-Agent header to the HttpClient instance. UserAgent is a metada that identifies the client application.
            this.httpClient.DefaultRequestHeaders.UserAgent.Add(userAgentHeader);
        }


        public void UpdateProgressBar(double progress)
        {
            if (InstallationProgressBar.Dispatcher.CheckAccess())
            {
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = InstallationProgressBar.Value,
                    To = progress,
                    Duration = TimeSpan.FromSeconds(2) // Adjust the duration as needed
                };
                InstallationProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
                Percent.Text = progress.ToString() + '%';
                return;
            }

            InstallationProgressBar.Dispatcher.Invoke(() =>
            {
                DoubleAnimation animation = new DoubleAnimation
                {
                    From = InstallationProgressBar.Value,
                    To = progress,
                    Duration = TimeSpan.FromSeconds(2) // Adjust the duration as needed
                };
                InstallationProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
                Percent.Text = progress.ToString() + '%';
            });
        }


        public void AppendLog(string message)
        {
            if (CommandLogTextBox.Dispatcher.CheckAccess())
            {
                CommandLogTextBox.AppendText(message + "\n");
                CommandLogTextBox.ScrollToEnd();
                return;
            }

            CommandLogTextBox.Dispatcher.Invoke(() =>
            {
                CommandLogTextBox.AppendText(message + "\n");
                CommandLogTextBox.ScrollToEnd();
            });
        }


        private Task CheckForVS2022(string sdkInstallationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    string fileName = "vswhere.exe";
                    Process process = new Process();
                    string result;

                    AppendLog("Checking if Visual Studio 2022 is available on your system...");
                    DownloadFromWeb("https://github.com/microsoft/vswhere/releases/download/3.1.7/vswhere.exe", sdkInstallationPath, fileName);

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(sdkInstallationPath, fileName),
                        Arguments = "-products * -requires Microsoft.Component.MSBuild -property installationPath",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (result is "\0")
                    {
                        AppendLog("Visual Studio 2022 not found on your system.");
                        AppendLog("Please install Visual Studio 2022 to continue.");
                        MessageBox.Show("Visual Studio 2022 not found!", "Visual Studio 2022 not found", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }
                    File.Delete(Path.Combine(sdkInstallationPath, fileName));
                    AppendLog("Found Visual Studio 2022.");
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });

        }

        private Task CheckForGit()
        {
            return Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    string result;

                    AppendLog("Checking if Git is available on your system...");

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (result is "\0")
                    {
                        AppendLog("Git is not available on your system.");
                        AppendLog("Please install Git to continue.");
                        MessageBox.Show("Git not found!", "Git not found", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }
                    AppendLog("Found Git.");
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });
        }

        private Task InstallCMake(string sdkInstallationPath)
        {
            return Task.Run(() => 
            {
                try
                {
                    AppendLog("Installing CMake...");
                    string fileName = "cmake-3.31.5-windows-x86_64.msi";
                    Process process = new Process();
                    DownloadFromWeb("https://github.com/Kitware/CMake/releases/download/v3.31.5/cmake-3.31.5-windows-x86_64.msi", sdkInstallationPath, fileName);
                    process.StartInfo.FileName = Path.Combine(sdkInstallationPath, fileName);
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    process.Start();
                    process.WaitForExit();
                    File.Delete(Path.Combine(sdkInstallationPath, fileName));
                    AppendLog("Completed installing CMake...");
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });

        }

        private Task DownloadSDK(Release targetSDK, string sdkInstallationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    string targerSdkZipFileName = targetSDK.Name + ".zip";

                    AppendLog("Downloading the Frogman Engine SDK from GitHub...");
                    if ((sdkInstallationPath is null) || (sdkInstallationPath is "\0"))
                    {
                        AppendLog("Failed to download the Frogman Engine SDK from GitHub...");
                        AppendLog("SDK installation path not set.");
                        MessageBox.Show("SDK installation path not set", "Download failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    if ((targetSDK.ZipballUrl is null) || (targetSDK.ZipballUrl is "\0"))
                    {
                        AppendLog("Failed to download the Frogman Engine SDK from GitHub...");
                        AppendLog("The URL is invalid, please contact the developer: https://github.com/Unknown-Stryker");
                        MessageBox.Show("Download failed!", "download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    if ((targetSDK.Name is null) || (targetSDK.Name is "\0"))
                    {
                        AppendLog("Failed to download the Frogman Engine SDK from GitHub...");
                        AppendLog("The target SDK version cannot be null, please contact the developer: https://github.com/Unknown-Stryker");
                        MessageBox.Show("Download failed!", "download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }
                    DownloadFromWeb(targetSDK.ZipballUrl, sdkInstallationPath, targetSDK.Name + ".zip");
                    AppendLog("Completed downloading the Frogman Engine SDK!");

                    string sdkZipPath = Path.Combine(sdkInstallationPath, targerSdkZipFileName);
                    string sdkPath = Path.Combine(sdkInstallationPath, targetSDK.Name);
                    ZipFile.ExtractToDirectory(sdkZipPath, sdkPath);
                    string[] folders = Directory.GetDirectories(sdkPath);
                    string tmpPath = sdkPath + "tmp";
                    Directory.Move(folders.First(), tmpPath);
                    File.Delete(sdkZipPath);
                    Directory.Delete(sdkPath);
                    Directory.Move(tmpPath, sdkPath);
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });

        }

        public async Task Install(Release targetSDK, string sdkInstallationPath)
        {
            try
            {
                await CheckForVS2022(sdkInstallationPath);
                await CheckForGit();
                UpdateProgressBar(10);
                await InstallCMake(sdkInstallationPath);
                UpdateProgressBar(30);
                await DownloadSDK(targetSDK, sdkInstallationPath);
                UpdateProgressBar(50);

                UpdateProgressBar(70);

                UpdateProgressBar(100);

                AppendLog("Configurating the Frogman GDK environment...");
            }
            catch (Exception e)
            {
                AppendLog(e.Message);
                MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }


        private HttpClient httpClient;
        private ProductInfoHeaderValue userAgentHeader;
        private void DownloadFromWeb(string webUrl, string desinationPath, string fileNameWithExtension)
        {
            try
            {
                // Send a GET request to the specified URL and get the response.
                HttpResponseMessage response = httpClient.GetAsync(webUrl).Result;

                // Check if the response indicates success (status code 200-299).
                if (response.IsSuccessStatusCode is false)
                {
                    // Display an error message if the request was not successful.
                    string errorMessage = $"Failed to download from {webUrl}";
                    string errorTitle = $"Error, HTTP Request Failed: status code {response.StatusCode}";
                    AppendLog(errorTitle);
                    AppendLog(errorMessage);
                    MessageBox.Show("Download Failed!", "Download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.FailFast(errorTitle + " " + errorMessage);
                }


                // Read the response content as a stream and write it to the file.
                Stream responseStream = response.Content.ReadAsStreamAsync().Result;
                FileStream fileStream = new FileStream(Path.Combine(desinationPath, fileNameWithExtension), FileMode.Create, FileAccess.Write, FileShare.None);
                responseStream.CopyTo(fileStream);

                fileStream.Close();
                responseStream.Close();
            }
            catch(Exception e)
            {
                AppendLog(e.Message);
                MessageBox.Show("Download Failed!", "Download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.FailFast(e.Message);
            }
        }
    }
}
