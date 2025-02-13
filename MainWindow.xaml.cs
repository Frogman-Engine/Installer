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
        FinalPage,
        UninstallationStage
    }


    public class GradientAnimator
    {
        private Storyboard storyboard;
        private ColorAnimation colorAnimationAlpha;
        private ColorAnimation colorAnimationZulu;


        public GradientAnimator()
        {
            this.colorAnimationAlpha = new ColorAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(7)),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                From = Colors.SeaGreen,
                To = Colors.MidnightBlue
            };

            this.colorAnimationZulu = new ColorAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(7)),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                From = Colors.MidnightBlue,
                To = Colors.SeaGreen
            };

            this.storyboard = new Storyboard
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            this.storyboard.Children.Add(colorAnimationAlpha);
            this.storyboard.Children.Add(colorAnimationZulu);

            Storyboard.SetTargetName(colorAnimationAlpha, "GradientStopAlpha");
            Storyboard.SetTargetProperty(colorAnimationAlpha, new PropertyPath(GradientStop.ColorProperty));

            Storyboard.SetTargetName(colorAnimationZulu, "GradientStopZulu");
            Storyboard.SetTargetProperty(colorAnimationZulu, new PropertyPath(GradientStop.ColorProperty));
        }


        public void PaintLeftPanelWithRedGradient()
        {
            this.colorAnimationAlpha.From = ColorConverter.ConvertFromString("#C81D77") as Color?;
            Debug.Assert(this.colorAnimationAlpha.From is not null);

            this.colorAnimationAlpha.To = ColorConverter.ConvertFromString("#F89B29") as Color?;
            Debug.Assert(this.colorAnimationAlpha.To is not null);

            this.colorAnimationZulu.From = ColorConverter.ConvertFromString("#F89B29") as Color?;
            Debug.Assert(this.colorAnimationZulu.From is not null);

            this.colorAnimationZulu.To = ColorConverter.ConvertFromString("#C81D77") as Color?;
            Debug.Assert(this.colorAnimationZulu.To is not null);
        }


        public void BeginStoryboard(Window window)
        {
            this.storyboard.Begin(window, HandoffBehavior.Compose);
        }
    }


    /// <summary>
    /// + feature: install another version of the SDK
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            this.toNextButtonText = "Accept";
            this.toPreviousButtonText = "Decline";
   
            this.Closing += MainWindow_Closing;

            this.pageIndex = PageIndex.MainPageLicenseDisplayingStage;
            this.DirConfigStage = new SecondPage("v2025.02.14");
            this.InstallationStage = new ThirdPage();
            this.uninstaller = new Uninstaller();
            this.gradientAnimator = new GradientAnimator();

            this.isAlreadyInstalled = this.uninstaller.ListAllVersionOfInstalledSDKs().Count is not 0;
            this.targetInstallationVersionOfSDK = "v0.0.0c";

            this.gradientAnimator.BeginStoryboard(this);
        }


        private bool isAlreadyInstalled;
        private string targetInstallationVersionOfSDK;

        private GradientAnimator gradientAnimator;

        private PageIndex pageIndex;
        private SecondPage DirConfigStage;
        private ThirdPage InstallationStage;
        private Uninstaller uninstaller;
        private void OnClickButtonAccept(object sender, RoutedEventArgs e)
        {
            switch (pageIndex)
            {
            case PageIndex.MainPageLicenseDisplayingStage:
                ToNextButtonText = "Next";
                ToPreviousButtonText = "Previous";

                if (isAlreadyInstalled is true)
                {
                    gradientAnimator.PaintLeftPanelWithRedGradient();
                    gradientAnimator.BeginStoryboard(this);
                    NextButton.Visibility = Visibility.Hidden;
                    PreviousButton.Visibility = Visibility.Hidden;

                    MainFrame.Navigate(uninstaller);
                    pageIndex = PageIndex.UninstallationStage;
                    return;
                }
                
                MainFrame.Navigate(DirConfigStage);
                pageIndex = PageIndex.DirConfigStage;
                break;

            case PageIndex.DirConfigStage:
                gradientAnimator.PaintLeftPanelWithRedGradient();
                gradientAnimator.BeginStoryboard(this);
                NextButton.Visibility = Visibility.Hidden;
                PreviousButton.Visibility = Visibility.Hidden;
                
                MainFrame.Navigate(InstallationStage);
                pageIndex = PageIndex.InstallationStage;
                break;
            }
        }


        private void OnClickButtonDecline(object sender, RoutedEventArgs e)
        {
            switch (pageIndex)
            {
            case PageIndex.MainPageLicenseDisplayingStage:
                Application.Current.Shutdown();
                break;

            case PageIndex.DirConfigStage:
                ToNextButtonText = "Accept";
                ToPreviousButtonText = "Decline";
                pageIndex = PageIndex.MainPageLicenseDisplayingStage;
                break;
            }

            MainFrame.GoBack();
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


        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            string messageBoxText;
            MessageBoxResult messageBoxResult;
            MessageBoxImage messageBoxImage;

            switch (pageIndex)
            {
            case PageIndex.InstallationStage:
                messageBoxText = "Frogman Engine SDK Installer: Installation is in progress.\n Are you sure you want to abort the installation?";
                messageBoxImage = MessageBoxImage.Warning;
                break;

            case PageIndex.UninstallationStage:
                messageBoxText = "Frogman Engine SDK Installer: Uninstallation is in progress.\n Are you sure you want to terminate the uninstallator?";
                messageBoxImage = MessageBoxImage.Warning;
                break;

            default:
                messageBoxText = "Frogman Engine SDK Installer: Are you sure you want to terminate the installator?";
                messageBoxImage = MessageBoxImage.Question;
                break;
            }

            messageBoxResult = MessageBox.Show(messageBoxText, "Exit Confirmation", MessageBoxButton.YesNo, messageBoxImage);
            
            // Handle the user's response.
            if (messageBoxResult is MessageBoxResult.No)
            {
                // Prevent the window from closing.
                e.Cancel = true;
            }
        }
    }
}