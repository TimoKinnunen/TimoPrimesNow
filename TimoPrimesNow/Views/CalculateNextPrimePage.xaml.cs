using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TimoPrimesNow.Views
{
    public sealed partial class CalculateNextPrimePage : Page
    {
        CancellationTokenSource nextPrimeCancellationTokenSource { get; set; }

        readonly MainPage mainPage;

        BigInteger enteredNumber { get; set; }

        IList<BigInteger> primenumberCandidates { get; set; }

        ParallelOptions parallelOptions { get; set; }

        public CalculateNextPrimePage()
        {
            InitializeComponent();

            mainPage = MainPage.CurrentMainPage;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            // code here
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            if (nextPrimeCancellationTokenSource != null)
            {
                nextPrimeCancellationTokenSource.Cancel();
                nextPrimeCancellationTokenSource.Dispose();
                nextPrimeCancellationTokenSource = null;
            }

            // code here
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }
        #endregion MenuAppBarButton

        private void EnteredNumberTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            FoundPrimenumberTextBlock.Text = $"Next primenumber found is shown here.";
            TimeElapsedTextBlock.Text = $"{string.Empty}";
            string enteredNumberAsString = EnteredNumberTextBox.Text.Trim();
            if (BigInteger.TryParse(enteredNumberAsString, out BigInteger numberAsBigInteger))
            {
                if (numberAsBigInteger.IsEven)
                {
                    numberAsBigInteger--;
                }
                if (numberAsBigInteger > 2)
                {
                    enteredNumber = numberAsBigInteger;
                    mainPage.NotifyUser($"{string.Empty}", NotifyType.StatusMessage);
                }
                else
                {
                    enteredNumber = 0;
                    mainPage.NotifyUser($"Enter number greater than 2, please.", NotifyType.ErrorMessage);
                }
            }
            else
            {
                enteredNumber = 0;
                mainPage.NotifyUser($"Please enter number greater than 2.", NotifyType.StatusMessage);
            }
        }

        private async void CalculateDataButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CalculateDataButton.Content.ToString() == "Cancel")
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    nextPrimeCancellationTokenSource.Cancel();
                }
                CalculateDataButton.Content = "Calculate next primenumber";
                CalculateDataButton.IsEnabled = false;
                return;
            }

            if (CalculateDataButton.Content.ToString() == "Calculate next primenumber")
            {
                if (enteredNumber < 3 || enteredNumber.IsEven)
                {
                    mainPage.NotifyUser($"Please enter number greater than 2.", NotifyType.ErrorMessage);
                    return;
                }

                CalculateDataButton.Content = "Cancel";
                ToolTipService.SetToolTip(CalculateDataButton, "Cancel.");
                EnteredNumberTextBox.IsEnabled = false;
                mainPage.NotifyUser($"Please wait or cancel.", NotifyType.StatusMessage);

                StartCalculateDataProgressRing();

                try
                {
                    nextPrimeCancellationTokenSource = new CancellationTokenSource();

                    // Use ParallelOptions instance to store the CancellationToken
                    parallelOptions = new ParallelOptions();
                    parallelOptions.CancellationToken = nextPrimeCancellationTokenSource.Token;
                    parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

                    await Task.Run(async () => await CalculateDataAsync()).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"You canceled the operation 'Calculate next primenumber'.", NotifyType.ErrorMessage);
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
                        CalculateDataButton.Content = "Calculate next primenumber";
                        ToolTipService.SetToolTip(CalculateDataButton, "Calculate next primenumber.");
                        CalculateDataButton.IsEnabled = true;
                        EnteredNumberTextBox.IsEnabled = true;
                        if (nextPrimeCancellationTokenSource != null)
                        {
                            nextPrimeCancellationTokenSource.Dispose();
                            nextPrimeCancellationTokenSource = null;
                        }
                    });
                }
            }
        }

        private async Task CalculateDataAsync()
        {
            BigInteger primenumberCandidate = enteredNumber;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!parallelOptions.CancellationToken.IsCancellationRequested)
            {
                primenumberCandidates = new List<BigInteger>();
                for (int i = 0; i < 1000; i++)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    primenumberCandidates.Add(primenumberCandidate += 2);
                }

                bool foundPrimenumber = false;
                IList<BigInteger> primeNumbersFromParallelForeach = GetPrimeListWithParallel(primenumberCandidates);

                foreach (BigInteger primenumber in primeNumbersFromParallelForeach.OrderBy(p => p))
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        FoundPrimenumberTextBlock.Text = primenumber.ToString();
                        mainPage.NotifyUser($"Next primenumber was found.", NotifyType.StatusMessage);

                        stopwatch.Stop();

                        // Get the elapsed time as a TimeSpan value.
                        TimeSpan timeSpan = stopwatch.Elapsed;

                        TimeElapsedTextBlock.Text = $"Elapsed time is {timeSpan.Hours:00} h {timeSpan.Minutes:00} min {timeSpan.Seconds:00} sec {timeSpan.Milliseconds:000} msec.";
                    });

                    foundPrimenumber = true;

                    break;
                }

                if (foundPrimenumber)
                {
                    break;
                }
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