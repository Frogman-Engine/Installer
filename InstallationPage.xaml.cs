#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using Windows.Devices.Geolocation;
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


        private Task InstallVisualStudio2022(string gdkInstallationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    string fileName = "vswhere.exe";
                    Process process = new Process();

                    AppendLog("Checking if the Visual Studio 2022 is available on your system...");
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

                    if (process.StandardOutput.ReadToEnd().Length is 0)
                    {
                        throw new ApplicationException("The Visual Studio 2022 is not available on your system.");
                        //AppendLog("Visual Studio 2022 is not available on your system.");
                        //AppendLog("Installing the latest version of Visual Studio 2022 Community...");
                        //string cmd = "winget install --id Microsoft.VisualStudio.2022.Community -e --source winget --override \"--add Microsoft.VisualStudio.Workload.NativeDesktop --add Microsoft.VisualStudio.Workload.NativeGame --add Microsoft.VisualStudio.Workload.ManagedDesktop --add Microsoft.VisualStudio.Workload.NativeMobile\"";
                        //AppendLog(cmd);
                        //process.StartInfo.FileName = "cmd.exe";
                        //process.StartInfo.Arguments = "/c " + cmd;
                        //process.StartInfo.UseShellExecute = true;
                        //process.StartInfo.CreateNoWindow = false;
                        //process.StartInfo.RedirectStandardInput = false;
                        //process.StartInfo.RedirectStandardOutput = false;

                        //process.Start();
                        //process.WaitForExit();
                        //AppendLog("Completed installing the latest version of Visual Studio 2022 Community...");
                    }
                    File.Delete(System.IO.Path.Combine(gdkInstallationPath, fileName));
                    AppendLog("Found the Visual Studio 2022.");
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
                    AppendLog("Checking if the Git is available on your system...");
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
                        AppendLog("The Git is not available on your system.");
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
                        AppendLog("Successfully installed the Git.");
                        return;
                    }
                    AppendLog("Found the Git.");
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
                    AppendLog("Checking if the CMake is available on your system...");
                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe ";
                    process.StartInfo.Arguments = "/c cmake --version";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();

                    if (process.StandardOutput.ReadToEnd().Length is 0)
                    {
                        AppendLog("The CMake is not available on your system.");
                        AppendLog("Installing the CMake version 3.31.5 ...");
                        string cmd = "winget install --id Kitware.CMake --version 3.31.5 -e --source winget";
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
                        AppendLog("Completed installing the CMake...");
                        return;
                    }
                    AppendLog("Found the CMake.");

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

        private Task BuildAssimp(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Building the assimp version {DataBase.AssimpVersion} ...");
                    string assimpPath = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                               $"assimp-{DataBase.AssimpVersion}");
                    Directory.SetCurrentDirectory(assimpPath);
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
                    AppendLog($"Building the ImGUI version {DataBase.ImGuiVersion} ...");
                    string imguiPath = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                              $"imgui-{DataBase.ImGuiVersion}");
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

        private Task BuildLZ4(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Building the LZ4 version {DataBase.LZ4Version} ...");
                    string lz4Path = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                                 $"lz4-{DataBase.LZ4Version}");
                    Directory.SetCurrentDirectory(lz4Path);
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "build\\cmake\\build.bat",
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

        private Task BuildSTB(string thirdPartyLibrariesPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    AppendLog($"Building the STB...");
                    string stbPath = System.IO.Path.Combine(thirdPartyLibrariesPath,
                                                              $"stb");
                    Directory.SetCurrentDirectory(stbPath);
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
                    await BuildABSL(thirdPartyLibrariesPath);
                    await BuildAssimp(thirdPartyLibrariesPath);
                    await DownloadAndBuildBoostLibraries(thirdPartyLibrariesPath);
                    await BuildGLFW(thirdPartyLibrariesPath); // ImGUI build fails if the GLFW does not exist.
                    await BuildImGUI(thirdPartyLibrariesPath);
                    await BuildLZ4(thirdPartyLibrariesPath);
                    await BuildSTB(thirdPartyLibrariesPath);
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

                    AppendLog($"Building the Frogman Engine Unit Test Cases...");
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
                UpdateProgressBar(1);

                await InstallVisualStudio2022(gdkInstallationPath);
                UpdateProgressBar(2);

                await InstallGit();
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