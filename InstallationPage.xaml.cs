#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Installer.Script;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using Windows.Devices.Geolocation;




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
        double currentProgress = 0.0;
        const double maxProgress = 100.0;
        public void UpdateProgressBar(double progress)
        {
            this.currentProgress = progress;
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
#pragma warning disable CS0414
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
                            Title = "Choose Visual Studio Version",
                            Width = 250,
                            Height = 125,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ResizeMode = ResizeMode.NoResize
                        };

                        bool closingBySelection = false;
                        dialog.Closing += (s, e) =>
                        {
                            if (closingBySelection == true)
                            {
                                return;
                            }

                            e.Cancel = true;
                            var messageBoxResult = MessageBox.Show("Frogman Engine GDK Installer: Are you sure you want to terminate the installer?", "Exit Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (messageBoxResult is MessageBoxResult.Yes)
                            {
                                Environment.Exit(0);
                            }
                        };

                        var panel = new StackPanel();

                        foreach (var version in versions)
                        {
                            var button = new Button
                            {
                                Content = version,
                                Margin = new Thickness(5),
                                Tag = version,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };
              

                            button.Click += (s, e) =>
                            {
                                closingBySelection = true;
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
                    if (response.Contains("cmake version 4.2.0") is false)
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

        private async Task RunBuildScript(string gdkInstallationPath)
        {
            AppendLog($"\n\nLoading the installation script.\n");
            string dllPath = Path.Combine(gdkInstallationPath, DataBase.GdkInstallScriptDllPathFromGdkRoot);
            string pdbPath = Path.ChangeExtension(dllPath, ".pdb");

            byte[] dll = File.ReadAllBytes(dllPath);
            byte[] pdb = File.ReadAllBytes(pdbPath);
            Assembly assembly = Assembly.Load(dll, pdb);

            Type[] candidates = assembly.GetTypes()
                .Where(type => type.IsPublic && (type.IsAbstract is false) && type.IsSubclassOf(typeof(ScriptMain)))
                .ToArray();

            foreach (Type type in assembly.GetTypes())
            {
                AppendLog($"{type.FullName} public={type.IsPublic} base={type.BaseType?.AssemblyQualifiedName}");
            }

            if (candidates.Length is not 1)
            {
                string errMsg = "Installation Failed! Installer.ScriptMain.ScheduleJobs(Script.JobParameters)'s implementation is undefined and is not overriden or, more than one definitions of Installer.ScriptMain.ScheduleJobs(Script.JobParameters) coexists.";
                AppendLog(errMsg);
                MessageBox.Show(errMsg, "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }


            ScriptMain script = (ScriptMain)Activator.CreateInstance(candidates[0])!;
            Installer.Script.JobParameters jobParameters = new Installer.Script.JobParameters
            {
                BuildBatchFileName = buildBatchFileName,
                VisualStudioVersion = visualStudioVersion,
                GdkInstallationPath = gdkInstallationPath
            };

            Queue<Job> queue = script.ScheduleJobs(jobParameters);
            AppendLog($"\nLoaded {queue.Count} jobs from the installation script.\n");

            double remainingProgress = maxProgress - currentProgress;
            double progressPerJob = remainingProgress / queue.Count;
            
            while (queue.Count > 0)
            {
                try
                {
                    Job job = queue.Dequeue();
                    AppendLog(job.DisplayedMessage);
                    // Run the synchronous Job.Run on a background thread and await its completion.
                    await Task.Run(() => job.Run(jobParameters));
                    UpdateProgressBar(currentProgress + progressPerJob);
                }
                catch(Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            }
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
                UpdateProgressBar(15);

                await InstallCMake(gdkInstallationPath);
                UpdateProgressBar(20);

                await DownloadGDK(targetGDK, gdkInstallationPath);
                UpdateProgressBar(30);

                string gdkPath = System.IO.Path.Combine(gdkInstallationPath, targetGDK.Name);
                await RunBuildScript(gdkPath);

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
                HttpResponseMessage response = httpClient.GetAsync(webUrl, HttpCompletionOption.ResponseHeadersRead).Result;

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