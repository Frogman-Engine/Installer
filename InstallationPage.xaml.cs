#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using Windows.Devices.Geolocation;




namespace Installer
{
    enum VisualStudioVersion
    {   
        None = 0,
        VisualStudio2022 = 17,
        VisualStudio2026 = 18
    }
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


        List<string?> versions = new();
        string buildBatchFileName = String.Empty;
        VisualStudioVersion visualStudioVersion = VisualStudioVersion.None;
        public Task RunVisualStudioWhere(string gdkInstallationPath)
        {
            return Task.Run(async () =>
            {
                try
                {
                    AppendLog("Checking if Visual Studio Community/Pro/Enterprise is available on your system...");

                    Process process = new Process();
                    string fileName = DownloadFromWeb(DataBase.VsWhereUrl, ".", "vswhere.exe");

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = DataBase.VsWhereOptions,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    string jsonOutput = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    using JsonDocument doc = JsonDocument.Parse(jsonOutput);

                    if (jsonOutput.Length is 0 ||
                        ((doc.RootElement.ValueKind is JsonValueKind.Array) && (doc.RootElement.GetArrayLength() is 0))
                        )
                    {
                        throw new ApplicationException("Visual Studio Community/Pro/Enterprise is not available on your system.");
                    }

                    File.Delete(fileName);
                    AppendLog("Found Visual Studio Community/Pro/Enterprise.");

                    foreach (JsonElement element in doc.RootElement.EnumerateArray())
                    {
                        string? version = element.GetProperty("catalog").GetProperty("productLineVersion").GetString();
                        switch(version)
                        {
                        case "17":
                        case "2022":
                            version = "Visual Studio 17 2022";
                            break;

                        case "18":
                        case "2026":
                            version = "Visual Studio 18 2026";
                            break;

                        default:
                            break;
                        }

                        versions.Add(version);
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var dialog = new Window
                        {
                            Title = "Please choose Visual Studio version",
                            Width = 300,
                            Height = 150,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };

                        var panel = new StackPanel();

                        foreach (var version in versions)
                        {
                            var button = new Button
                            {
                                Content = version,
                                Margin = new Thickness(5),
                                Tag = version,
                                Height = 25
                            };
              

                            button.Click += (s, e) =>
                            {
                                dialog.Tag = ((Button)s).Tag;
                                dialog.DialogResult = true;
                                dialog.Close();
                            };

                            panel.Children.Add(button);
                        }

                        dialog.Content = panel;

                        if (dialog.ShowDialog() == true)
                        {
                            string? selectedVersion = dialog.Tag?.ToString();
                            Debug.Assert(selectedVersion is not null);
                            switch (selectedVersion)
                            {
                                case "Visual Studio 17 2022":
                                    buildBatchFileName = "build-with-vs2022.bat";
                                    visualStudioVersion = VisualStudioVersion.VisualStudio2022;
                                    break;

                                case "Visual Studio 18 2026":
                                    buildBatchFileName = "build-with-vs2026.bat";
                                    visualStudioVersion = VisualStudioVersion.VisualStudio2026;
                                    break;

                                default:
                                    Debug.Assert(false, "No Default!");
                                    break;
                            }
                            MessageBox.Show($"{selectedVersion}", "Selected version:");
                        }
                    });
                }
                catch (Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });

        }

        private Task InstallGit()
        {
            return Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    AppendLog("Checking if Git is available on your system...");
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c git --version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit();

                    if (process.StandardOutput.ReadToEnd().Length is 0)
                    {
                        AppendLog("Git is not available on your system.");
                        AppendLog("Installing the latest Git...");
                        string cmd = "winget install --id Git.Git -e --source winget";
                        AppendLog(cmd);
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = "/c " + cmd;
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.CreateNoWindow = false;
                        process.StartInfo.RedirectStandardInput = false;
                        process.StartInfo.RedirectStandardOutput = false;
                        process.StartInfo.Verb = "runas";

                        process.Start();
                        process.WaitForExit();
                        AppendLog("Successfully installed Git.");
                        return;
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
                    AppendLog("Checking if CMake is available on your system...");
                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe ";
                    process.StartInfo.Arguments = "/c cmake --version";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();

                    string response = process.StandardOutput.ReadToEnd();
                    if (response.Contains("cmake version 4.2") is false)
                    {
                        AppendLog("CMake 4.2.0 is not available on your system.");
                        AppendLog("Installing the CMake version 4.2.0 ...");

                        string cmd = String.Empty;
                        if (response.Length > 0)
                        {
                            cmd = "winget uninstall Kitware.CMake && ";
                        }
                        cmd += "winget install --id Kitware.CMake --version 4.2.0 -e --source winget";
                        AppendLog(cmd);
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = "/c " + cmd;
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.CreateNoWindow = false;
                        process.StartInfo.RedirectStandardInput = false;
                        process.StartInfo.RedirectStandardOutput = false;
                        process.StartInfo.Verb = "runas";

                        process.Start();
                        process.WaitForExit();
                        AppendLog("Completed installing CMake...");
                        return;
                    }
                    AppendLog("Found CMake.");

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
                    if ((gdkInstallationPath is null) || (gdkInstallationPath.Length is 0))
                    {
                        AppendLog("Failed to download the Frogman Engine GDK from GitHub...");
                        AppendLog("GDK installation path not set.");
                        MessageBox.Show("GDK installation path not set", "Download failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    if ((targetGDK.ZipballUrl is null) || (targetGDK.ZipballUrl.Length is 0))
                    {
                        AppendLog("Failed to download the Frogman Engine GDK from GitHub...");
                        AppendLog($"The URL is invalid, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                        MessageBox.Show("Download failed!", "download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    if ((targetGDK.Name is null) || (targetGDK.Name.Length is 0) ||
                        (targetGDK.Tag is null) || (targetGDK.Tag.Length is 0))
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

        private Task BuildABSL(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Building the absl version {DataBase.ABSLVersion} ...");
                    string abslPath = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                             $"abseil-cpp-{DataBase.ABSLVersion}");
                    Directory.SetCurrentDirectory(abslPath);
                    Process process = new Process();

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = buildBatchFileName,
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

                    string underscored = boostFolderName.Replace('.', '_');
                    underscored = underscored.Replace('-', '_');
                    underscored = System.IO.Path.Combine(thirdPartyLibrariesPath, underscored);

                    string boostFolderPath = System.IO.Path.Combine(thirdPartyLibrariesPath, boostFolderName);
                    if (Directory.Exists(underscored))
                    {
                        Directory.Move(underscored, boostFolderPath);
                    }

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
                    process.StartInfo.Arguments += " ";
                    switch (visualStudioVersion)
                    {
                        case VisualStudioVersion.VisualStudio2022:
                            process.StartInfo.Arguments += "toolset=msvc-14.3";
                            break;

                        case VisualStudioVersion.VisualStudio2026:
                            process.StartInfo.Arguments += "toolset=msvc-14.5";
                            break;
                    }

                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    AppendLog($"Building debug version of the Boost libraries with {process.StartInfo.Arguments} ...");
                    process.Start();
                    process.WaitForExit();

                    process.StartInfo.Arguments = DataBase.BoostReleaseBuildB2Options;
                    process.StartInfo.Arguments += " ";
                    switch (visualStudioVersion)
                    {
                        case VisualStudioVersion.VisualStudio2022:
                            process.StartInfo.Arguments += "toolset=msvc-14.3";
                            break;

                        case VisualStudioVersion.VisualStudio2026:
                            process.StartInfo.Arguments += "toolset=msvc-14.5";
                            break;
                    }

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
                    AppendLog($"Building the ImGUI version {DataBase.ImGuiVersion} ...");
                    string imguiPath = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                              $"imgui-{DataBase.ImGuiVersion}");
                    Directory.SetCurrentDirectory(imguiPath);
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = buildBatchFileName,
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

        private Task BuildGLFW(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Building the GLFW version {DataBase.GLFWVersion} ...");
                    string glfwPath = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                              $"glfw-{DataBase.GLFWVersion}");
                    Directory.SetCurrentDirectory(glfwPath);
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = buildBatchFileName,
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

        private Task BuildLZ4(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Building the LZ4 version {DataBase.LZ4Version} ...");
                    string lz4Path = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                                 $"lz4-{DataBase.LZ4Version}");
                    Directory.SetCurrentDirectory( Path.Combine(lz4Path, "build\\cmake") );
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = buildBatchFileName,
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
                    await BuildABSL(thirdPartyLibrariesPath);
                    await DownloadAndBuildBoostLibraries(thirdPartyLibrariesPath);
                    await BuildGLFW(thirdPartyLibrariesPath); // ImGUI build fails if the GLFW does not exist.
                    await BuildImGUI(thirdPartyLibrariesPath);
                    await BuildLZ4(thirdPartyLibrariesPath);
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
                        FileName = buildBatchFileName,
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    AppendLog($"Building the Frogman Engine Audio...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Audio\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building the Frogman Engine Core...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Core\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building the Frogman Engine Framework...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Framework\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building the Frogman Engine Renderer...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Renderer\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building the Frogman Engine...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Engine\\CMake"));
                    process.Start();
                    process.WaitForExit();

                    AppendLog($"Building the Frogman Engine Header Tool...");
                    Directory.SetCurrentDirectory(System.IO.Path.Combine(gdkInstallationPath, "SDK\\Header-Tool\\CMake"));
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
            if ((targetGDK.Tag is null) || (targetGDK.Tag.Length is 0))
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
                if ((targetGDK.Name is null) || (targetGDK.Name.Length is 0))
                {
                    AppendLog("Failed to download the Frogman Engine GDK from GitHub...");
                    AppendLog($"The target GDK version cannot be null, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                    MessageBox.Show("Download failed!", "download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
                UpdateProgressBar(5);


                AppendLog("Configuring large page memory support...");
                if (LargePagePrivilegeHelper.EnsureGranted(out bool rebootRequired, out string err))
                {
                    if (rebootRequired)
                    {
                        AppendLog("[LargePage] SeLockMemoryPrivilege granted. Reboot required after the installation.");
                    }
                    else
                    {
                        AppendLog("Windows Large Page already enabled. Skipping...");
                    }
                }
                else
                {
                    AppendLog($"[LargePage] Failed ({err}). Falling back to 4KB pages.");
                }
                UpdateProgressBar(10);

                await InstallGit();
                UpdateProgressBar(20);

                await InstallCMake(gdkInstallationPath);
                UpdateProgressBar(30);

                await DownloadGDK(targetGDK, gdkInstallationPath);
                UpdateProgressBar(50);

                string gdkPath = System.IO.Path.Combine(gdkInstallationPath, targetGDK.Name);
                await BuildThirdPartyLibraries(gdkPath);
                UpdateProgressBar(70);

                await BuildFrogmanGDK(gdkPath);
                UpdateProgressBar(90);

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
        private string DownloadFromWeb(string webUrl, string desinationPath, string fileNameWithExtension)
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
                using FileStream fileStream = new FileStream(System.IO.Path.Combine(desinationPath, fileNameWithExtension), FileMode.Create, FileAccess.Write, FileShare.None);
                responseStream.CopyTo(fileStream);

                fileStream.Close();
                responseStream.Close();
                return fileStream.Name;
            }
            catch (Exception e)
            {
                AppendLog(e.Message);
                MessageBox.Show("Download Failed!", "Download Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.FailFast(e.Message);
                return string.Empty;
            }
        }
    }
}