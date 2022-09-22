using System;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace TimoPrimesNow.Views
{
    public sealed partial class AboutPage : Page
    {
        private readonly MainPage mainPage;

        public AboutPage()
        {
            InitializeComponent();

            SizeChanged += AboutPage_SizeChanged;

            Loaded += AboutPage_Loaded;

            mainPage = MainPage.CurrentMainPage;
        }

        private void AboutPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        private void AboutPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            Package package = Package.Current;

            TimoPrimesNowImage.Source = new BitmapImage(package.Logo);

            AppDisplayNameTextBlock.Text = package.DisplayName;

            PublisherTextBlock.Text = package.PublisherDisplayName;

            PackageVersion packageVersion = package.Id.Version;
            VersionTextBlock.Text = $"Version {packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
            // code here
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            // code here
        }

        private void SetPageContentStackPanelWidth()
        {
            PageContentStackPanel.Width = ActualWidth -
                PageContentScrollViewer.Margin.Left -
                PageContentScrollViewer.Padding.Right;
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }

        private async void ReadMoreAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                var uri = new Uri("ms-appx:///Assets/Hunting prime numbers with TimoEratosthenes algorithm.pdf");
                var pdfFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
                await Launcher.LaunchFileAsync(pdfFile);
            }
            catch (Exception ex)
            {
                mainPage.NotifyUser($"Couldn't open .pdf-file. {ex.Message}", NotifyType.ErrorMessage);
            }
        }
        #endregion MenuAppBarButton
    }
}
