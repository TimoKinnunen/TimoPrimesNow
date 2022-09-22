using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TimoPrimesNow.Helpers;
using TimoPrimesNow.Models;
using Windows.Storage;
using Windows.Storage.Provider;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TimoPrimesNow.Views
{
    public sealed partial class HomePage : Page
    {
        private readonly MainPage mainPage;

        private readonly ObservableCollection<Prime> primesObservableCollection;

        public IEnumerable<Prime> primesList { get; private set; }
        public int numberOfPrimes { get; private set; }


        public ObservableCollection<int> NumberOfRecordsInOneSetList { get; private set; }
        public int CurrentNumberOfRecordsInOneSet { get; private set; }

        public ObservableCollection<int> SetNumberList { get; private set; }
        public int CurrentSetNumber { get; private set; }

        public HomePage()
        {
            InitializeComponent();

            primesObservableCollection = new ObservableCollection<Prime>();

            mainPage = MainPage.CurrentMainPage;

            NumberOfRecordsInOneSetList = new ObservableCollection<int> { 20, 100, 100000 };

            NumberOfRecordsInOneSetComboBox.ItemsSource = NumberOfRecordsInOneSetList;

            SetNumberList = new ObservableCollection<int> { 1, 2, 3 };

            SetNumberComboBox.ItemsSource = SetNumberList;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            try
            {
                numberOfPrimes = await App.SqlPrimeRepo.GetPrimesCountAsync();

                CountOfPrimenumbersTextBlock.Text = $"{numberOfPrimes}";
                BiggestPrimenumberTextBlock.Text = $"{await App.SqlPrimeRepo.GetBiggestPrimenumberAsStringAsync()}";

                #region comboboxes
                CurrentNumberOfRecordsInOneSet = NumberOfRecordsInOneSetList.FirstOrDefault();
                CurrentNumberOfRecordsInOneSet = HelpLocalSettings.RetrieveCurrentNumberOfRecordsInOneSet(CurrentNumberOfRecordsInOneSet);
                NumberOfRecordsInOneSetComboBox.SelectedItem = NumberOfRecordsInOneSetList.Contains(CurrentNumberOfRecordsInOneSet) ? CurrentNumberOfRecordsInOneSet : 1;

                RecalculateSetList();
                #endregion comboboxes

                await FetchData();

                NumberOfRecordsInOneSetComboBox.SelectionChanged += NumberOfRecordsInOneSetComboBox_SelectionChanged;

                SetNumberComboBox.SelectionChanged += SetNumberComboBox_SelectionChanged;

                NumberOfRecordsInOneSetComboBox.IsEnabled = true;
                SetNumberComboBox.IsEnabled= true;
                SearchPrimenumberTextBox.IsEnabled = true;
            }
            catch (OutOfMemoryException ex)
            {
                mainPage.NotifyUser($"OutOfMemoryException: {ex.Message}", NotifyType.ErrorMessage);
            }
            catch (Exception ex)
            {
                mainPage.NotifyUser($"{ex.Message}", NotifyType.ErrorMessage);
            }
            // code here
        }

        private void RecalculateSetList()
        {
            var maxSetNumber = Math.Max(1, numberOfPrimes / CurrentNumberOfRecordsInOneSet);

            SetNumberList = new ObservableCollection<int>();
            for (int i = 0; i < maxSetNumber; i++)
            {
                SetNumberList.Add(i + 1);
            }

            if (numberOfPrimes > maxSetNumber * CurrentNumberOfRecordsInOneSet)
            {
                int lastSetNumber = SetNumberList.LastOrDefault();
                SetNumberList.Add(lastSetNumber + 1);
            }

            SetNumberComboBox.ItemsSource = SetNumberList;

            CurrentSetNumber = SetNumberList.FirstOrDefault();
            SetNumberComboBox.SelectedItem = CurrentSetNumber;
        }

        private async Task FetchData()
        {
            int counter = 0;
            primesList = await App.SqlPrimeRepo.GetAllPrimesAsync((CurrentSetNumber - 1) * CurrentNumberOfRecordsInOneSet, CurrentNumberOfRecordsInOneSet);
            if (primesList.Count() > 0)
            {
                primesObservableCollection.Clear();
                foreach (Prime prime in primesList)
                {
                    primesObservableCollection.Add(prime);
                    counter++;
                }
                PrimesListView.ItemsSource = primesObservableCollection;
            }
            mainPage.NotifyUser($"Fetched {counter} records.", NotifyType.StatusMessage);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            if (NumberOfRecordsInOneSetComboBox != null && NumberOfRecordsInOneSetComboBox.SelectedValue != null)
            {
                HelpLocalSettings.SaveCurrentNumberOfRecordsInOneSet(NumberOfRecordsInOneSetComboBox.SelectedValue);
            }

            NumberOfRecordsInOneSetComboBox.SelectionChanged -= NumberOfRecordsInOneSetComboBox_SelectionChanged;

            SetNumberComboBox.SelectionChanged -= SetNumberComboBox_SelectionChanged;
            // code here
        }

        private async void NumberOfRecordsInOneSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null && comboBox.SelectedValue != null)
            {
                CurrentNumberOfRecordsInOneSet = int.Parse(comboBox.SelectedValue.ToString());
            }
            await FetchData();

            RecalculateSetList();
        }

        private async void SetNumberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null && comboBox.SelectedValue != null)
            {
                CurrentSetNumber = int.Parse(comboBox.SelectedValue.ToString());
            }
            await FetchData();
        }

        private void SearchPrimenumberTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string trimmedPrimenumber = SearchPrimenumberTextBox.Text.Trim();
                if (BigInteger.TryParse(trimmedPrimenumber, out BigInteger primeNumberAsBigInteger))
                {
                    Prime prime = primesObservableCollection.FirstOrDefault(p => BigInteger.Parse(p.Primenumber) >= primeNumberAsBigInteger);
                    if (prime != null)
                    {
                        if (prime.Primenumber == trimmedPrimenumber)
                        {
                            PrimesListView.SelectedItem = prime;
                        }
                        PrimesListView.ScrollIntoView(prime, ScrollIntoViewAlignment.Leading);
                    }
                }
            }
            catch (OutOfMemoryException ex)
            {
                mainPage.NotifyUser($"OutOfMemoryException: {ex.Message}", NotifyType.ErrorMessage);
            }
            catch (Exception ex)
            {
                mainPage.NotifyUser($"{ex.Message}", NotifyType.ErrorMessage);
            }
        }

        private async void ExportSetDataAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                StorageFile file = await HelpFileSavePicker.GetStorageFileForJsonAsync($"TimoPrimesNow_{CurrentSetNumber}_");
                if (file == null)
                {
                    mainPage.NotifyUser($"You canceled the operation 'Export data'.", NotifyType.ErrorMessage);
                    return;
                }

                mainPage.NotifyUser($"Exporting data. Please wait.", NotifyType.StatusMessage);

                ExportSetDataAppBarButton.IsEnabled = false;
                ExportDataProgressRing.IsEnabled = true;
                ExportDataProgressRing.IsActive = true;
                ExportDataProgressRing.Visibility = Visibility.Visible;

                string jsonData = await Task.Run(() => JsonConvert.SerializeObject(primesList)).ConfigureAwait(false);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    // write to file
                    await FileIO.WriteTextAsync(file, jsonData);
                    // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                    // Completing updates may require Windows to ask for user input.
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status == FileUpdateStatus.Complete)
                    {
                        mainPage.NotifyUser($"File {file.Name} was saved.", NotifyType.StatusMessage);
                    }
                    else
                    {
                        mainPage.NotifyUser($"File {file.Name} couldn't be saved.", NotifyType.StatusMessage);
                    }

                    ExportSetDataAppBarButton.IsEnabled = true;
                    ExportDataProgressRing.IsEnabled = false;
                    ExportDataProgressRing.IsActive = false;
                    ExportDataProgressRing.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                mainPage.NotifyUser($"{ex.Message}", NotifyType.ErrorMessage);
            }
        }
    }
}
