using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;




namespace Installer
{
    enum PageIndex
    {
        MainPageLicenseDisplayingStage,
        DirConfigStage,
        InstallationStage,
        UninstallerStage,
        UninstallationStage,
        FinalPage
    }


    public class GradientAnimator
    {
        private Storyboard storyboard;
        public Storyboard Storyboard
        {
            get { return storyboard; }
        }
        private ColorAnimation colorAnimationAlpha;
        private ColorAnimation colorAnimationZulu;


        public GradientAnimator(Color alpha, Color zulu)
        {
            this.colorAnimationAlpha = new ColorAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(7)),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                From = alpha,// Colors.SeaGreen,
                To = zulu//Colors.MidnightBlue
            };

            this.colorAnimationZulu = new ColorAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(7)),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                From = zulu,
                To = alpha
            };

            this.storyboard = new Storyboard
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            this.storyboard.Children.Add(colorAnimationAlpha);
            this.storyboard.Children.Add(colorAnimationZulu);

            Storyboard.SetTargetName(colorAnimationAlpha, nameof(colorAnimationAlpha));
            Storyboard.SetTargetProperty(colorAnimationAlpha, new PropertyPath(GradientStop.ColorProperty));

            Storyboard.SetTargetName(colorAnimationZulu, nameof(colorAnimationZulu));
            Storyboard.SetTargetProperty(colorAnimationZulu, new PropertyPath(GradientStop.ColorProperty));
        }


        public void PaintLeftPanelWithRedGradient(Window window)
        {
            this.colorAnimationAlpha.From = ColorConverter.ConvertFromString("#C81D77") as Color?;
            Debug.Assert(this.colorAnimationAlpha.From is not null);

            this.colorAnimationAlpha.To = ColorConverter.ConvertFromString("#F89B29") as Color?;
            Debug.Assert(this.colorAnimationAlpha.To is not null);

            this.colorAnimationZulu.From = ColorConverter.ConvertFromString("#F89B29") as Color?;
            Debug.Assert(this.colorAnimationZulu.From is not null);

            this.colorAnimationZulu.To = ColorConverter.ConvertFromString("#C81D77") as Color?;
            Debug.Assert(this.colorAnimationZulu.To is not null);

            this.Storyboard.Begin(window, HandoffBehavior.Compose);
        }
    }




    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            this.toNextButtonText = "Accept";
            this.toPreviousButtonText = "Decline";

            this.MainFrame.Navigating += OnNavigation;
            this.MainFrame.Navigated += OnNavigated;
            this.Closing += OnClose;
           
            this.pageIndex = PageIndex.MainPageLicenseDisplayingStage;
            this.dirConfigStage = new DirConfigPage(DataBase.GDKInstallerVersion);
            this.installationStage = new InstallationPage(DataBase.GDKInstallerVersion);
            this.finalPage = new FinalPage();
            this.uninstaller = new Uninstaller();
            this.uninstallationStage = new UninstallationPage();
            this.gradientAnimator = new GradientAnimator(Colors.SeaGreen, Colors.MidnightBlue);
            this.gradientAnimator.Storyboard.Begin(this, HandoffBehavior.Compose);
            installedVersionsOfGDKs = this.uninstaller.ListAllVersionOfInstalledSDKs();
            this.isAlreadyInstalled = installedVersionsOfGDKs.Count is not 0;
        }


        private GradientAnimator gradientAnimator;
        private static List<string> installedVersionsOfGDKs = new List<string>();
        public static List<string> InstalledVersionsOfGDKs
        {
            get { return installedVersionsOfGDKs; }
        }
        private bool isAlreadyInstalled;


        private PageIndex pageIndex;
        private DirConfigPage dirConfigStage;
        private InstallationPage installationStage;
        private FinalPage finalPage;
        private Uninstaller uninstaller;
        private UninstallationPage uninstallationStage;
        private void OnClickGoNext(object sender, RoutedEventArgs e)
        {
            switch (pageIndex)
            {
            case PageIndex.MainPageLicenseDisplayingStage:
                if (isAlreadyInstalled is true)
                {
                    MainFrame.Navigate(uninstaller);
                    return;
                }
                MainFrame.Navigate(dirConfigStage);
                return;

            case PageIndex.DirConfigStage:
                if (dirConfigStage.TargetSDK.Name is null)
                {
                    MessageBox.Show("Please select the SDK version.", "Frogman Engine SDK Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(dirConfigStage.InstallationPathTextBox.Text))
                {
                    MessageBox.Show("Please provide a valid file path.", "Frogman Engine SDK Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MainFrame.Navigate(installationStage);
                return;

            case PageIndex.InstallationStage:
                if (installationStage.InstallationProgressBar.Value < 100)
                {
                    MessageBox.Show("The installation process is not completed yet.", "Frogman Engine SDK Installer", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                finalPage.SetOpMode(OpMode.Install);
                MainFrame.Navigate(finalPage);
                return;

            case PageIndex.UninstallationStage:
                if (uninstallationStage.UninstallationProgressBar.Value < 100)
                {
                    MessageBox.Show("The uninstallation process is not completed yet.", "Frogman Engine SDK Uninstaller", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                finalPage.SetOpMode(OpMode.Uninstall);
                MainFrame.Navigate(finalPage);
                return;

                case PageIndex.UninstallerStage:
                if (uninstaller.TargetSDK.Name is null)
                {
                    MessageBox.Show("Please select the SDK version.", "Frogman Engine SDK Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MainFrame.Navigate(uninstallationStage);
                break;

            case PageIndex.FinalPage:
                Application.Current.Shutdown();
                return;
            }
        }


        private void OnClickGoBack(object sender, RoutedEventArgs e)
        {
            switch (pageIndex)
            {
            case PageIndex.MainPageLicenseDisplayingStage:
            case PageIndex.FinalPage:
                Environment.Exit(0);
                return;

            case PageIndex.UninstallerStage:
                MainFrame.Navigate(dirConfigStage);
                return;

            case PageIndex.DirConfigStage:
                pageIndex = PageIndex.MainPageLicenseDisplayingStage;
                ToNextButtonText = "Accept";
                ToPreviousButtonText = "Decline";
                MainFrame.GoBack();
                return;
            }
        }


        private void OnNavigation(object? sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            switch (e.Content)
            {
            case Uninstaller:
                pageIndex = PageIndex.UninstallerStage;
                gradientAnimator.PaintLeftPanelWithRedGradient(this);
                ToNextButtonText = "Proceed";
                ToPreviousButtonText = "Skip";
                return;

            case UninstallationPage:
                pageIndex = PageIndex.UninstallationStage;
                NextButton.Visibility = Visibility.Hidden;
                PreviousButton.Visibility = Visibility.Hidden;
                return;

            case DirConfigPage:
                pageIndex = PageIndex.DirConfigStage;
                ToNextButtonText = "Next";
                ToPreviousButtonText = "Previous";
                return;

            case InstallationPage:
                pageIndex = PageIndex.InstallationStage;
                gradientAnimator.PaintLeftPanelWithRedGradient(this);
                NextButton.Visibility = Visibility.Hidden;
                PreviousButton.Visibility = Visibility.Hidden;
                return;

            case FinalPage:
                pageIndex = PageIndex.FinalPage;
                
                return;
            }
        }


        private async void OnNavigated(object? sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            switch (e.Content)
            {
            case UninstallationPage:
                await uninstallationStage.Uninstall(uninstaller.TargetSDK);
                NextButton.Visibility = Visibility.Visible;
                ToNextButtonText = "Complete";
                return;

            case InstallationPage:
                await installationStage.RunVisualStudioWhere(dirConfigStage.InstallationPathTextBox.Text);
                await installationStage.Install(dirConfigStage.TargetSDK, dirConfigStage.InstallationPathTextBox.Text);
                NextButton.Visibility = Visibility.Visible;
                ToNextButtonText = "Complete";
                return;
            }
        }


        private void OnClose(object? sender, CancelEventArgs e)
        {
            string messageBoxText;
            MessageBoxResult messageBoxResult;

            switch (pageIndex)
            {
            case PageIndex.FinalPage:
                return;

            case PageIndex.UninstallationStage:
                messageBoxText = "Frogman Engine GDK Installer: Unable to terminate the process while uninstalling the SDK.";
                MessageBox.Show(messageBoxText, "Exit Confirmation", MessageBoxButton.OK, MessageBoxImage.Stop);

                // Prevent the window from closing.
                e.Cancel = true;
                return;

            case PageIndex.InstallationStage:
                messageBoxText = "Frogman Engine GDK Installer: Unable to terminate the process while installing the SDK.";
                MessageBox.Show(messageBoxText, "Exit Confirmation", MessageBoxButton.OK, MessageBoxImage.Stop);
                  
                // Prevent the window from closing.
                e.Cancel = true;
                return;

            default:
                messageBoxText = "Frogman Engine GDK Installer: Are you sure you want to terminate the installer?";
                messageBoxResult = MessageBox.Show(messageBoxText, "Exit Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                // Handle the user's response.
                if (messageBoxResult is MessageBoxResult.No)
                {
                    // Prevent the window from closing.
                    e.Cancel = true;
                }
                break;
            }
        }


        private string toNextButtonText;
        public string ToNextButtonText
        {
            get { return toNextButtonText; }
            set
            {
                toNextButtonText = value;
                OnPropertyChanged(nameof(toNextButtonText));
            }
        }

        private string toPreviousButtonText;
        public string ToPreviousButtonText
        {
            get { return toPreviousButtonText; }
            set
            {
                toPreviousButtonText = value;
                OnPropertyChanged(nameof(toPreviousButtonText));
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "\0")
        {
            Debug.Assert(PropertyChanged is not null);
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}