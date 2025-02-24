#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Media.Animation;




namespace Installer
{
    public partial class InstallationPage : System.Windows.Controls.Page
    {
        public InstallationPage(string appVersion)
        {
            InitializeComponent();
            this.DataContext = this;

            this.httpClient = new HttpClient();
            this.userAgentHeader = new ProductInfoHeaderValue("Frogman_Engine_GDK_Installer", appVersion);

            // Add a User-Agent header to the HttpClient instance. UserAgent is a metada that identifies the client application.
            this.httpClient.DefaultRequestHeaders.UserAgent.Add(userAgentHeader);

            this.progressBarAnimation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(2)
            };
        }

        DoubleAnimation progressBarAnimation;
        public void UpdateProgressBar(double progress)
        {
            if (InstallationProgressBar.Dispatcher.CheckAccess())
            {
                progressBarAnimation.From = InstallationProgressBar.Value;
                progressBarAnimation.To = progress;

                InstallationProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressBarAnimation);
                Percent.Text = progress.ToString() + '%';
                return;
            }

            InstallationProgressBar.Dispatcher.Invoke(() =>
            {
                InstallationProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressBarAnimation);
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


        private Task CheckForVS2022(string gdkInstallationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    string fileName = "vswhere.exe";
                    Process process = new Process();

                    AppendLog("Checking if Visual Studio 2022 is available on your system...");
                    DownloadFromWeb(DataBase.VsWhereUrl, gdkInstallationPath, fileName);

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = System.IO.Path.Combine(gdkInstallationPath, fileName),
                        Arguments = DataBase.VsWhereOptions,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit();

                    if (process.StandardOutput.ReadToEnd() is "\0")
                    {
                        AppendLog("Visual Studio 2022 not found on your system.");
                        AppendLog("Please install Visual Studio 2022 to continue.");
                        MessageBox.Show("Visual Studio 2022 not found!", "Visual Studio 2022 not found", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }
                    File.Delete(System.IO.Path.Combine(gdkInstallationPath, fileName));
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
                    process.WaitForExit();

                    if (process.StandardOutput.ReadToEnd() is "\0")
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

        private Task InstallCMake(string gdkInstallationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog("Installing CMake...");
                    string fileName = "cmake-3.31.5-windows-x86_64.msi";
                    Process process = new Process();
                    DownloadFromWeb(DataBase.CMakeUrl, gdkInstallationPath, fileName);
                    process.StartInfo.FileName = System.IO.Path.Combine(gdkInstallationPath, fileName);
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    process.Start();
                    process.WaitForExit();
                    File.Delete(System.IO.Path.Combine(gdkInstallationPath, fileName));

                    process.StartInfo.FileName = "cmake";
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();

                    if (process.StandardOutput.ReadToEnd() is "\0")
                    {
                        AppendLog("CMake installation failed!");
                        MessageBox.Show("CMake not found!", "CMake not found", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

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

        private Task DownloadGDK(Release targetGDK, string gdkInstallationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    string targerGDKZipFileName = targetGDK.Name + ".zip";

                    AppendLog("Downloading the Frogman Engine GDK from GitHub...");
                    if ((gdkInstallationPath is null) || (gdkInstallationPath is "\0"))
                    {
                        AppendLog("Failed to download the Frogman Engine GDK from GitHub...");
                        AppendLog("GDK installation path not set.");
                        MessageBox.Show("GDK installation path not set", "Download failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    if ((targetGDK.ZipballUrl is null) || (targetGDK.ZipballUrl is "\0"))
                    {
                        AppendLog("Failed to download the Frogman Engine GDK from GitHub...");
                        AppendLog($"The URL is invalid, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                        MessageBox.Show("Download failed!", "download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    if ((targetGDK.Name is null) || (targetGDK.Name is "\0") ||
                        (targetGDK.Tag is null) || (targetGDK.Tag is "\0"))
                    {
                        AppendLog("Failed to download the Frogman Engine GDK from GitHub...");
                        AppendLog($"The release title or the release tag is null, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                        MessageBox.Show("Download failed!", "download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    DownloadFromWeb(targetGDK.ZipballUrl, gdkInstallationPath, targetGDK.Name + ".zip");
                    AppendLog("Completed downloading the Frogman Engine GDK!");

                    string gdkZipPath = System.IO.Path.Combine(gdkInstallationPath, targerGDKZipFileName);
                    string gdkPath = System.IO.Path.Combine(gdkInstallationPath, targetGDK.Name);
                    ZipFile.ExtractToDirectory(gdkZipPath, gdkPath);
                    string[] folders = Directory.GetDirectories(gdkPath);
                    string tmpPath = gdkPath + "tmp";
                    Directory.Move(folders.First(), tmpPath);
                    File.Delete(gdkZipPath);
                    Directory.Delete(gdkPath);
                    Directory.Move(tmpPath, gdkPath);
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });

        }

        private Task DownloadAndBuildBoostLibraries(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Downloading the Boost libraries v{DataBase.BoostVersion} ...");
                    string boostFolderName = $"boost-{DataBase.BoostVersion}";
                    string boostZipFileName = boostFolderName + ".zip";
                    string boostZipFilePath = System.IO.Path.Combine(thirdPartyLibrariesPath, boostZipFileName);
                    DownloadFromWeb(DataBase.BoostUrl, thirdPartyLibrariesPath, boostZipFileName);
                    ZipFile.ExtractToDirectory(boostZipFilePath, thirdPartyLibrariesPath);
                    File.Delete(boostZipFilePath);
                    string boostFolderPath = System.IO.Path.Combine(thirdPartyLibrariesPath, boostFolderName);

                    Directory.SetCurrentDirectory(boostFolderPath);
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "bootstrap.bat",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit();
                    AppendLog(process.StandardOutput.ReadToEnd());

                    process.StartInfo.FileName = "b2.exe";
                    process.StartInfo.Arguments = DataBase.BoostDebugBuildB2Options;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    AppendLog($"Building debug version of the Boost libraries with {process.StartInfo.Arguments} ...");
                    process.Start();
                    process.WaitForExit();

                    process.StartInfo.Arguments = DataBase.BoostReleaseBuildB2Options;
                    AppendLog($"Building release version of the Boost libraries with {process.StartInfo.Arguments} ...");
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });
        }

        private Task BuildImGUI(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Building ImGUI version {DataBase.ImGuiVersion} ...");
                    string imguiPath = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                                System.IO.Path.Combine(DataBase.FrogmanEngineThirdPartyFolderRelativePath, $"imgui-{DataBase.ImGuiVersion}"));
                    Directory.SetCurrentDirectory(imguiPath);
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "build.bat",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });
        }

        private Task BuildThirdPartyLibraries(string gdkInstallationPath)
        {
            return Task.Run(async () =>
            {
                try
                {
                    string thirdPartyLibrariesPath = System.IO.Path.Combine(gdkInstallationPath, DataBase.FrogmanEngineThirdPartyFolderRelativePath);
                    await DownloadAndBuildBoostLibraries(thirdPartyLibrariesPath);
                    await BuildImGUI(gdkInstallationPath);
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });
        }

        private Task BuildFrogmanGDK(string gdkInstallationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "build.bat",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    AppendLog($"Building Frogman Engine Core...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Core\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building Frogman Engine Framework...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Framework\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building Frogman Engine...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Engine\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building Frogman Engine Header Tool...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Header-Tool\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building Frogman Engine Unit Test Cases...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Tests\\Unit-Tests"));
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });
        }

        private void SetGdkEnvironmentVariable(Release targetGDK, string gdkPath)
        {
            if ((targetGDK.Tag is null) || (targetGDK.Tag is "\0"))
            {
                AppendLog("Failed to set the environment variable for the Frogman Engine GDK...");
                AppendLog($"The target GDK version cannot be null, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
            string variableName = DataBase.GenerateGDKSystemPathVariableName(targetGDK.Tag);

            try
            {
                Environment.SetEnvironmentVariable(variableName, gdkPath, EnvironmentVariableTarget.Machine);
                AppendLog($"Set {variableName} environment variable to {gdkPath}.");
            }
            catch (Exception e)
            {
                AppendLog(e.Message);
                MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            try
            {
                File.WriteAllText(System.IO.Path.Combine(gdkPath, $"{variableName}.txt"), $"{variableName}={gdkPath}");
            }
            catch (Exception e)
            {
                Environment.SetEnvironmentVariable(variableName, null, EnvironmentVariableTarget.Machine);

                AppendLog(e.Message);
                MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }

        public async Task Install(Release targetGDK, string gdkInstallationPath)
        {
            try
            {
                if ((targetGDK.Name is null) || (targetGDK.Name is "\0"))
                {
                    AppendLog("Failed to download the Frogman Engine GDK from GitHub...");
                    AppendLog($"The target GDK version cannot be null, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                    MessageBox.Show("Download failed!", "download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
                UpdateProgressBar(1);

                await CheckForVS2022(gdkInstallationPath);
                UpdateProgressBar(2);

                await CheckForGit();
                UpdateProgressBar(4);

                await InstallCMake(gdkInstallationPath);
                UpdateProgressBar(10);

                await DownloadGDK(targetGDK, gdkInstallationPath);
                UpdateProgressBar(40);

                string gdkPath = System.IO.Path.Combine(gdkInstallationPath, targetGDK.Name);
                await BuildThirdPartyLibraries(gdkPath);
                UpdateProgressBar(60);

                await BuildFrogmanGDK(gdkPath);
                UpdateProgressBar(80);

                // Set PATH environment variable
                AppendLog("Configurating the Frogman GDK environment...");
                SetGdkEnvironmentVariable(targetGDK, gdkPath);

                AppendLog("Successfully installed Frogman Engine GDK!");
                UpdateProgressBar(100);
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
                FileStream fileStream = new FileStream(System.IO.Path.Combine(desinationPath, fileNameWithExtension), FileMode.Create, FileAccess.Write, FileShare.None);
                responseStream.CopyTo(fileStream);

                fileStream.Close();
                responseStream.Close();
            }
            catch (Exception e)
            {
                AppendLog(e.Message);
                MessageBox.Show("Download Failed!", "Download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.FailFast(e.Message);
            }
        }
    }
}