using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TimoPrimesNow.Data;
using TimoPrimesNow.Models;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TimoPrimesNow.Views
{
    public sealed partial class FactorizeNumberPage : Page
    {
        CancellationTokenSource calculateCancellationTokenSource { get; set; }

        readonly MainPage mainPage;

        BigInteger enteredNumber { get; set; }

        Dictionary<BigInteger, int> divisorsDictionary { get; set; }

        ParallelOptions parallelOptions { get; set; }

        IEnumerable<Prime> primesList { get; set; }
        BigInteger biggestCurrentDivisor { get; set; }
        int biggestCurrentId { get; set; }
        public FactorizeNumberPage()
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

        private void EnteredProductTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            EmptyTextBlocks();

            if (BigInteger.TryParse(EnteredProductTextBox.Text, out BigInteger numberAsBigInteger))
            {
                if (numberAsBigInteger > 0)
                {
                    enteredNumber = numberAsBigInteger;
                    mainPage.NotifyUser(string.Empty, NotifyType.StatusMessage);
                    CalculateDataButton.IsEnabled = true;
                }
                else
                {
                    enteredNumber = 0;
                    mainPage.NotifyUser($"Enter number greater than 0, please.", NotifyType.StatusMessage);
                    CalculateDataButton.IsEnabled = false;
                }
            }
            else
            {
                enteredNumber = 0;
                mainPage.NotifyUser($"Please enter a number greater than 0.", NotifyType.StatusMessage);
                CalculateDataButton.IsEnabled = false;
            }
        }

        private void EmptyTextBlocks()
        {
            ElapsedTimeTextBlock.Text = string.Empty;
            DivisorsTextBlock.Text = string.Empty;
            CountOfDivisorsTextBlock.Text = string.Empty;
            FactorsTextBlock.Text = string.Empty;
            CountOfFactorsTextBlock.Text = string.Empty;
        }

        private async void CalculateDataButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CalculateDataButton.Content.ToString() == "Cancel")
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    calculateCancellationTokenSource.Cancel();
                }
                CalculateDataButton.Content = "Calculate divisors";
                ToolTipService.SetToolTip(CalculateDataButton, "Calculate divisors.");
                return;
            }

            if (CalculateDataButton.Content.ToString() == "Calculate divisors")
            {
                EmptyTextBlocks();

                CalculateDataButton.Content = "Cancel";
                ToolTipService.SetToolTip(CalculateDataButton, "Cancel.");

                StartCalculateDataProgressRing();

                biggestCurrentDivisor = 0;

                biggestCurrentId = 0;

                divisorsDictionary = new Dictionary<BigInteger, int>();

                EnteredProductTextBox.IsEnabled = false;

                try
                {
                    calculateCancellationTokenSource = new CancellationTokenSource();

                    // Use ParallelOptions instance to store the CancellationToken
                    parallelOptions = new ParallelOptions();
                    parallelOptions.CancellationToken = calculateCancellationTokenSource.Token;
                    parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

                    await Task.Run(async () => await CalculateDataAsync(enteredNumber)).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"You canceled the operation 'Calculate divisors'.", NotifyType.ErrorMessage);
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
                        CalculateDataButton.Content = "Calculate divisors";
                        ToolTipService.SetToolTip(CalculateDataButton, "Calculate divisors.");

                        if (calculateCancellationTokenSource != null)
                        {
                            calculateCancellationTokenSource.Dispose();
                            calculateCancellationTokenSource = null;
                        }

                        EnteredProductTextBox.IsEnabled = true;
                    });
                }
            }
        }

        private async Task CalculateDataAsync(BigInteger enteredProduct)
        {
            if (enteredProduct == 0)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mainPage.NotifyUser($"Please enter a number greater than 0.", NotifyType.StatusMessage);
                });
                return;
            }

            if (!parallelOptions.CancellationToken.IsCancellationRequested)
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                IList<BigInteger> divisorCandidates;

                #region smallPrimes
                foreach (BigInteger smallPrime in SmallPrimes.smallPrimes.Where(s => s <= (enteredProduct + 1) / 2))
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    while (enteredProduct % smallPrime == 0)
                    {
                        enteredProduct /= smallPrime;
                        AddToDivisorDictionary(smallPrime);
                    }

                    biggestCurrentDivisor = smallPrime;
                    biggestCurrentId++; // id in SQLite database starts with 1
                }
                #endregion smallPrimes

                #region operations in parallel
                BigInteger maxEnteredProduct;
                try
                {
                    maxEnteredProduct = (BigInteger)Math.Sqrt((double)enteredProduct);
                }
                catch (Exception)
                {
                    maxEnteredProduct = (enteredProduct + 1) / 2;
                }

                while (biggestCurrentDivisor <= maxEnteredProduct)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    divisorCandidates = new List<BigInteger>();

                    primesList = await App.SqlPrimeRepo.GetAllPrimesAsync(biggestCurrentId);
                    if (primesList.Count() > 0)
                    {
                        foreach (Prime prime in primesList)
                        {
                            parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                            if (parallelOptions.CancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            biggestCurrentDivisor = BigInteger.Parse(prime.Primenumber);
                            divisorCandidates.Add(biggestCurrentDivisor);
                            biggestCurrentId = prime.Id;

                            if (biggestCurrentDivisor >= maxEnteredProduct)
                            {
                                break;
                            }
                        }

                        // Get the elapsed time as a TimeSpan value.
                        TimeSpan meanTimeSpan = stopwatch.Elapsed;

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ElapsedTimeTextBlock.Text = $"Elapsed time is {meanTimeSpan.Hours:00} h {meanTimeSpan.Minutes:00} min {meanTimeSpan.Seconds:00} sec {meanTimeSpan.Milliseconds:000} msec.";
                            mainPage.NotifyUser($"Primenumber from database. Trying to find divisor {biggestCurrentDivisor}.", NotifyType.StatusMessage);
                        });
                    }
                    else
                    {
                        for (int i = 0; i < 1000000; i++)
                        {
                            parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                            if (parallelOptions.CancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            biggestCurrentDivisor += 2;

                            divisorCandidates.Add(biggestCurrentDivisor);

                            if (biggestCurrentDivisor >= maxEnteredProduct)
                            {
                                break;
                            }
                        }

                        // Get the elapsed time as a TimeSpan value.
                        TimeSpan meanTimeSpan = stopwatch.Elapsed;

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ElapsedTimeTextBlock.Text = $"Elapsed time is {meanTimeSpan.Hours:00} h {meanTimeSpan.Minutes:00} min {meanTimeSpan.Seconds:00} sec {meanTimeSpan.Milliseconds:000} msec.";
                            mainPage.NotifyUser($"Add with 2. Trying to find divisor {biggestCurrentDivisor}.", NotifyType.StatusMessage);
                        });
                    }

                    IList<BigInteger> divisorsFromParallelForeach = GetDivisorListWithParallel(divisorCandidates, enteredProduct);

                    foreach (BigInteger divisor in divisorsFromParallelForeach)
                    {
                        parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                        if (parallelOptions.CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        AddToDivisorDictionary(divisor);
                        enteredProduct /= divisor;
                    }
                }

                if (enteredProduct > 1)
                {
                    AddToDivisorDictionary(enteredProduct);
                }
                #endregion operations in parallel

                #region divisor
                List<BigInteger> divisors = new List<BigInteger>();
                foreach (KeyValuePair<BigInteger, int> divisorsDictionaryKeyValuePair in divisorsDictionary.OrderBy(p => p.Key))
                {
                    for (int i = 0; i < divisorsDictionaryKeyValuePair.Value; i++)
                    {
                        divisors.Add(divisorsDictionaryKeyValuePair.Key);
                    }
                }
                #endregion divisor

                #region factor
                List<BigInteger> factors = new List<BigInteger>() { 1, enteredNumber };
                for (int firstDigitIndex = 0; firstDigitIndex < divisors.Count; firstDigitIndex++)
                {
                    BigInteger firstDigit = divisors[firstDigitIndex];
                    AddToFactors(factors, firstDigit);
                    AddToFactors(factors, enteredNumber / firstDigit);
                    for (int secondDigitIndex = firstDigitIndex + 1; secondDigitIndex < divisors.Count; secondDigitIndex++)
                    {
                        BigInteger secondDigit = divisors[secondDigitIndex];
                        BigInteger twoDigitProduct = firstDigit * secondDigit;
                        AddToFactors(factors, twoDigitProduct);
                        AddToFactors(factors, enteredNumber / twoDigitProduct);
                    }
                }
                #endregion factor

                stopwatch.Stop();

                // Get the elapsed time as a TimeSpan value.
                TimeSpan timeSpan = stopwatch.Elapsed;

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mainPage.NotifyUser(string.Empty, NotifyType.StatusMessage);
                    ElapsedTimeTextBlock.Text = $"Elapsed time is {timeSpan.Hours:00} h {timeSpan.Minutes:00} min {timeSpan.Seconds:00} sec {timeSpan.Milliseconds:000} msec.";
                    DivisorsTextBlock.Text = $"{string.Join(" * ", divisors.OrderBy(d => d))}";
                    CountOfDivisorsTextBlock.Text = $"{divisors.Count}.";
                    FactorsTextBlock.Text = $"{string.Join(", ", factors.OrderBy(d => d))}";
                    CountOfFactorsTextBlock.Text = $"{factors.Count}.";
                });
            }
        }

        private void AddToFactors(List<BigInteger> factors, BigInteger digit)
        {
            if (!factors.Contains(digit))
            {
                factors.Add(digit);
            }
        }
        private void AddToDivisorDictionary(BigInteger divisor)
        {
            KeyValuePair<BigInteger, int> existingKeyValuePair = divisorsDictionary.FirstOrDefault(p => p.Key == divisor);
            if (existingKeyValuePair.Key > 0)
            {
                divisorsDictionary[divisor] = existingKeyValuePair.Value + 1;
            }
            else
            {
                divisorsDictionary.Add(divisor, 1);
            }
        }

        /// <summary>
        /// GetDivisorListWithParallel returns divisors by using Parallel.ForEach
        /// </summary>
        /// <param name="divisorCandidates"></param>
        /// <param name="enteredProduct"></param>
        /// <returns></returns>
        private IList<BigInteger> GetDivisorListWithParallel(IList<BigInteger> divisorCandidates, BigInteger enteredProduct)
        {
            ConcurrentBag<BigInteger> divisors = new ConcurrentBag<BigInteger>();

            Parallel.ForEach(divisorCandidates, divisorCandidate =>
            {
                if (enteredProduct % divisorCandidate == 0)
                {
                    divisors.Add(divisorCandidate);
                }
            });

            return divisors.ToList();
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