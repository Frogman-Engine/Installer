using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
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
    public partial class UninstallationPage : System.Windows.Controls.Page
    {
        public UninstallationPage()
        {
            InitializeComponent();
            this.DataContext = this;

            this.progressBarAnimation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(2)
            };
        }


        DoubleAnimation progressBarAnimation;
        public void UpdateProgressBar(double progress)
        {
            if (UninstallationProgressBar.Dispatcher.CheckAccess())
            {
                progressBarAnimation.From = UninstallationProgressBar.Value;
                progressBarAnimation.To = progress;

                UninstallationProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressBarAnimation);
                Percent.Text = progress.ToString() + '%';
                return;
            }

            UninstallationProgressBar.Dispatcher.Invoke(() =>
            {
                UninstallationProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressBarAnimation);
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


        private Task RemoveGDK(Release targetGDK)
        {
            return Task.Run(() =>
            {
                try
                {
                    if ((targetGDK.Tag is null) || (targetGDK.Tag.Length is 0))
                    {
                        AppendLog("Failed to uninstall the Frogman Engine GDK!");
                        AppendLog($"The release tag is null, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                        MessageBox.Show("Uninstallation failed!", "Uninstallation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    IDictionary environmentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
                    string? path = environmentVariables[DataBase.GenerateGDKSystemPathVariableName(targetGDK.Tag)] as string;
                    Debug.Assert(path is not null);
                    Directory.Delete(path, true);
                }
                catch(Exception e)
                {
                    AppendLog(e.Message);
                    MessageBox.Show("Failed to uninstall the Frogman Engine GDK!", "Uninstallation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }
            });
        }

        private void UnsetGdkEnvironmentVariable(Release targetGDK)
        {
            try
            {
                if ((targetGDK.Tag is null) || (targetGDK.Tag.Length is 0))
                {
                    AppendLog("Failed to uninstall the Frogman Engine GDK!");
                    AppendLog($"The release tag is null, please contact the developer: {DataBase.FrogmanEngineDeveloperGitHubProfileUrl}");
                    MessageBox.Show("Uninstallation failed!", "Uninstallation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(-1);
                }

                IDictionary environmentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable(DataBase.GenerateGDKSystemPathVariableName(targetGDK.Tag), null, EnvironmentVariableTarget.Machine);
            }
            catch (Exception e)
            {
                AppendLog(e.Message);
                MessageBox.Show("Failed to uninstall the Frogman Engine GDK!", "Uninstallation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }

        public async Task Uninstall(Release targetGDK)
        {
            try
            {
                await RemoveGDK(targetGDK);
                UpdateProgressBar(90);

                UnsetGdkEnvironmentVariable(targetGDK);
                AppendLog("Successfully uninstalled Frogman Engine GDK!");
                UpdateProgressBar(100);
            }
            catch (Exception e)
            {
                AppendLog(e.Message);
                MessageBox.Show("Installation failed!", "Installation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }
    }
}
