using System;
using System.Threading.Tasks;
using TimoPrimesNow.Helpers;
using TimoPrimesNow.Models;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TimoPrimesNow.Views
{
    public sealed partial class DatabaseInfoPage : Page
    {
        private readonly MainPage mainPage;

        public DatabaseInfoPage()
        {
            InitializeComponent();

            SizeChanged += DatabaseInfoPage_SizeChanged;

            Loaded += DatabaseInfoPage_Loaded;

            mainPage = MainPage.CurrentMainPage;
        }

        private void DatabaseInfoPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        private void DatabaseInfoPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            #region SQLite database TimoPrimesNow.db
            CountOfPrimenumbersTextBlock.Text = $"{await App.SqlPrimeRepo.GetPrimesCountAsync()}";
            BiggestPrimenumberTextBlock.Text = $"{await App.SqlPrimeRepo.GetBiggestPrimenumberAsStringAsync()}";

            StorageFile fileTimoPrimesNow = await StorageFile.GetFileFromPathAsync(App.DatabasePath);
            if (fileTimoPrimesNow != null)
            {
                BasicProperties basicPropertiesTimoPrimesNow = await fileTimoPrimesNow.GetBasicPropertiesAsync();
                TimoPrimesNowFileSize.Text = $"File TimoPrimesNow.db's size is {HelpToFileSize.ToFileSize(basicPropertiesTimoPrimesNow.Size)}.";
                TimoPrimesNowFilePath.Text = @fileTimoPrimesNow.Path;
            }
            else
            {
                TimoPrimesNowFileSize.Text = "File TimoPrimesNow.db is missing.";
            }
            #endregion SQLite database TimoPrimesNow.db
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
        #endregion MenuAppBarButton

        private async void DeleteDatabaseTableAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            #region ask for permission to delete database table
            bool bryt = false;
            MessageDialog messageDialog = new MessageDialog("Do you want to delete database table with primenumbers?\nPrimenumbers will be deleted.", "Delete database table and all records in it.");

            messageDialog.Commands.Add(new UICommand("Delete", (command) =>
            {
            }));
            messageDialog.Commands.Add(new UICommand("Cancel", (command) =>
            {
                bryt = true;
            }));
            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 1;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            await messageDialog.ShowAsync();

            if (bryt)
            {
                mainPage.NotifyUser($"You canceled the operation 'Delete table'.", NotifyType.ErrorMessage);

                return;
            }

            DeleteDatabaseTableProgressRing.IsActive = true;
            DeleteDatabaseTableProgressRing.IsEnabled = true;
            #endregion ask for permission to delete database table

            #region delete database table
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    mainPage.NotifyUser($"Please wait a few seconds.", NotifyType.StatusMessage);

                    await App.SqlPrimeRepo.DeleteDatabaseTableAsync();
                    await Task.Delay(1000);

                    await App.SqlPrimeRepo.CreateTableAsync();
                    await Task.Delay(1000);

                    await App.SqlPrimeRepo.UpsertPrimeAsync(new Prime { Primenumber = 2.ToString() });
                    await App.SqlPrimeRepo.UpsertPrimeAsync(new Prime { Primenumber = 3.ToString() });
                    await App.SqlPrimeRepo.UpsertPrimeAsync(new Prime { Primenumber = 5.ToString() });

                    mainPage.NotifyUser($"Please wait a few more seconds.", NotifyType.StatusMessage);

                    CountOfPrimenumbersTextBlock.Text = $"Count of primenumbers is {await App.SqlPrimeRepo.GetPrimesCountAsync()}.";
                    BiggestPrimenumberTextBlock.Text = $"Biggest primenumber is {await App.SqlPrimeRepo.GetBiggestPrimenumberAsStringAsync()}.";

                    mainPage.NotifyUser($"Thank you for waiting a few seconds.", NotifyType.StatusMessage);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mainPage.NotifyUser($"{ex.Message}", NotifyType.ErrorMessage);
                });
            }
            finally
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    DeleteDatabaseTableProgressRing.IsActive = false;
                    DeleteDatabaseTableProgressRing.IsEnabled = false;
                    mainPage.NotifyUser($"Database table with primenumbers was deleted.", NotifyType.StatusMessage);
                });
            }
            #endregion delete database table     
        }

        private async void OpenDatabaseFolderButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                StorageFolder localApplicationDataFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                if (localApplicationDataFolder != null)
                {
                    await Launcher.LaunchFolderAsync(localApplicationDataFolder);
                }
            }
            catch (Exception ex)
            {
                mainPage.NotifyUser($"File explorer couldn't open database folder. {ex.Message}", NotifyType.ErrorMessage);
            }

        }
    }
}
