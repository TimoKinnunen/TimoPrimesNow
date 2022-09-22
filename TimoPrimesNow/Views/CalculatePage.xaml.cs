using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TimoPrimesNow.Models;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TimoPrimesNow.Views
{
    public sealed partial class CalculatePage : Page
    {
        CancellationTokenSource calculateCancellationTokenSource { get; set; }

        readonly MainPage mainPage;

        DispatcherTimer dispatcherTimer { get; set; }

        int totalCountOfPrimenumbers { get; set; }
        int sessionCountOfPrimenumbers { get; set; }

        int countOfMinutes { get; set; }

        BigInteger BiggestPrimenumber { get; set; }

        IList<BigInteger> primenumberCandidates { get; set; }
        ParallelOptions parallelOptions { get; set; }

        public CalculatePage()
        {
            InitializeComponent();

            mainPage = MainPage.CurrentMainPage;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            countOfMinutes++;
            CalculationSpeedTextBlock.Text = $"Calculation speed is {sessionCountOfPrimenumbers / countOfMinutes} primenumbers/minute ({sessionCountOfPrimenumbers}/{countOfMinutes}).";
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            totalCountOfPrimenumbers = await App.SqlPrimeRepo.GetPrimesCountAsync();
            BiggestPrimenumber = BigInteger.Parse(await App.SqlPrimeRepo.GetBiggestPrimenumberAsStringAsync());

            CountOfPrimenumbersTextBlock.Text = $"{totalCountOfPrimenumbers}";
            BiggestPrimenumberTextBlock.Text = $"{BiggestPrimenumber}";

            CalculateDataButton.IsEnabled = true;

            // code here
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            if (calculateCancellationTokenSource != null)
            {
                calculateCancellationTokenSource.Cancel();
                calculateCancellationTokenSource.Dispose();
                calculateCancellationTokenSource = null;
            }
            // code here
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }
        #endregion MenuAppBarButton

        private async void CalculateDataButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CalculateDataButton.Content.ToString() == "Cancel")
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    calculateCancellationTokenSource.Cancel();
                }
                CalculateDataButton.Content = "Calculate new primenumbers";
                return;
            }

            if (CalculateDataButton.Content.ToString() == "Calculate new primenumbers")
            {
                if (totalCountOfPrimenumbers == 0)
                {
                    mainPage.NotifyUser($"Database table does not contain primenumbers. Delete database table and try again.", NotifyType.ErrorMessage);
                    return;
                }

                CalculateDataButton.Content = "Cancel";
                ToolTipService.SetToolTip(CalculateDataButton, "Cancel.");
                mainPage.NotifyUser($"Please wait or cancel. Calculation speed is shown every minute.", NotifyType.StatusMessage);

                StartCalculateDataProgressRing();

                countOfMinutes = 0;
                sessionCountOfPrimenumbers = 0;
                dispatcherTimer.Start();

                BiggestPrimenumber = BigInteger.Parse(await App.SqlPrimeRepo.GetBiggestPrimenumberAsStringAsync());

                try
                {
                    if (BiggestPrimenumber.IsEven)
                    {
                        throw new Exception("Database table contains an even primenumber. Only odd primenumbers are accepted.");
                    }

                    calculateCancellationTokenSource = new CancellationTokenSource();

                    // Use ParallelOptions instance to store the CancellationToken
                    parallelOptions = new ParallelOptions();
                    parallelOptions.CancellationToken = calculateCancellationTokenSource.Token;
                    parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

                    await Task.Run(async () => await CalculateDataAsync(BiggestPrimenumber)).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"You canceled the operation 'Calculate new primenumbers'.", NotifyType.ErrorMessage);
                    });
                }
                catch (OutOfMemoryException ex)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"OutOfMemoryException: {ex.Message}", NotifyType.ErrorMessage);
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
                        StopCalculateDataProgressRing();
                        CalculateDataButton.Content = "Calculate new primenumbers";
                        ToolTipService.SetToolTip(CalculateDataButton, "Calculate new primenumbers.");

                        dispatcherTimer.Stop();
                        CalculationSpeedTextBlock.Text = string.Empty;

                        if (calculateCancellationTokenSource != null)
                        {
                            calculateCancellationTokenSource.Dispose();
                            calculateCancellationTokenSource = null;
                        }
                    });
                }
            }
        }

        private async Task CalculateDataAsync(BigInteger biggestPrimenumber)
        {
            BigInteger primenumberCandidate = biggestPrimenumber;

            while (!parallelOptions.CancellationToken.IsCancellationRequested)
            {
                int batchCountOfPrimenumbers = 0;

                primenumberCandidates = new List<BigInteger>();
                for (int i = 0; i < 10000; i++)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    primenumberCandidates.Add(primenumberCandidate += 2);
                }

                IList<BigInteger> primeNumbersFromParallelForeach = GetPrimeListWithParallel(primenumberCandidates);

                foreach (BigInteger primenumber in primeNumbersFromParallelForeach.OrderBy(p => p)) //important to save in database table in correct order!!
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    int newPrimeId = await App.SqlPrimeRepo.UpsertPrimeAsync(new Prime { Primenumber = primenumber.ToString() });
                    totalCountOfPrimenumbers++;
                    sessionCountOfPrimenumbers++;
                    batchCountOfPrimenumbers++;
                    biggestPrimenumber = primenumber;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CountOfPrimenumbersTextBlock.Text = $"{totalCountOfPrimenumbers}";
                    BiggestPrimenumberTextBlock.Text = $"{biggestPrimenumber}";
                    mainPage.NotifyUser($"Added {batchCountOfPrimenumbers} primenumbers to database table.", NotifyType.StatusMessage);
                });
            }
        }

        /// <summary>
        /// GetPrimeListWithParallel returns Prime numbers by using Parallel.ForEach
        /// </summary>
        /// <param name="primenumberCandidates"></param>
        /// <returns></returns>
        private IList<BigInteger> GetPrimeListWithParallel(IList<BigInteger> primenumberCandidates)
        {
            ConcurrentBag<BigInteger> primeNumbers = new ConcurrentBag<BigInteger>();

            Parallel.ForEach(primenumberCandidates, primenumberCandidate =>
            {
                if (IsPrime(primenumberCandidate))
                {
                    primeNumbers.Add(primenumberCandidate);
                }
            });

            return primeNumbers.ToList();
        }

        /// <summary>
        /// IsPrime returns true if number is Prime, else false.(https://en.wikipedia.org/wiki/Prime_number)
        /// </summary>
        /// <param name="primenumberCandidate"></param>
        /// <returns></returns>
        private bool IsPrime(BigInteger primenumberCandidate)
        {
            try
            {
                for (BigInteger divisor = 3; divisor <= (BigInteger)Math.Sqrt((double)primenumberCandidate); divisor += 2)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (primenumberCandidate % divisor == 0)
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                for (BigInteger divisor = 3; divisor <= (primenumberCandidate + 1) / 2; divisor += 2)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (primenumberCandidate % divisor == 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void StartCalculateDataProgressRing()
        {
            CalculateDataProgressRing.IsActive = true;
            CalculateDataProgressRing.Visibility = Visibility.Visible;
        }

        private void StopCalculateDataProgressRing()
        {
            CalculateDataProgressRing.IsActive = false;
            CalculateDataProgressRing.Visibility = Visibility.Collapsed;
        }
    }
}