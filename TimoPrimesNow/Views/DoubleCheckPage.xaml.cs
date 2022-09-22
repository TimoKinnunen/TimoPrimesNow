using System;
using System.Diagnostics;
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
    public sealed partial class DoubleCheckPage : Page
    {
        CancellationTokenSource doubleCheckCancellationTokenSource { get; set; }

        readonly MainPage mainPage;

        int numberOfprimenumbersToCheck { get; set; }
        int biggestId { get; set; }
        int countOfOddPrimenumbers { get; set; }

        ParallelOptions parallelOptions { get; set; }

        public DoubleCheckPage()
        {
            InitializeComponent();

            mainPage = MainPage.CurrentMainPage;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            Prime biggestPrime = await App.SqlPrimeRepo.GetBiggestPrimeAsync().ConfigureAwait(false);
            if (biggestPrime != null)
            {
                //database table has autoincrement flag and starts with id = 1
                biggestId = biggestPrime.Id;
                countOfOddPrimenumbers = biggestId - 1; //forget id = 1 which is primenumber = 2

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CountOfPrimenumbersTextBlock.Text = $"Count of odd primenumbers in database table is {countOfOddPrimenumbers}. Forgetting '2'.";
                    NumberOfPrimenumbersToCheckTextBlock.Text = countOfOddPrimenumbers.ToString();
                    DoubleCheckDataButton.IsEnabled = true; //enable button to double check
                    HowManyTextBox.IsEnabled = true; //enable textbox
                });
            }
            // code here
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            if (doubleCheckCancellationTokenSource != null)
            {
                doubleCheckCancellationTokenSource.Cancel();
                doubleCheckCancellationTokenSource.Dispose();
                doubleCheckCancellationTokenSource = null;
            }
            // code here
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }
        #endregion MenuAppBarButton

        private void HowManyTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            string howManyPrimenumbers = HowManyTextBox.Text.Trim();
            if (string.IsNullOrEmpty(howManyPrimenumbers))
            {
                NumberOfPrimenumbersToCheckTextBlock.Text = countOfOddPrimenumbers.ToString();
                return;
            }

            int enteredNumberOfprimenumbersToCheck;
            if (int.TryParse(howManyPrimenumbers, out int howManyAsInt))
            {
                enteredNumberOfprimenumbersToCheck = howManyAsInt;

                if (enteredNumberOfprimenumbersToCheck > 0)
                {
                    numberOfprimenumbersToCheck = Math.Min(enteredNumberOfprimenumbersToCheck, countOfOddPrimenumbers);
                    NumberOfPrimenumbersToCheckTextBlock.Text = numberOfprimenumbersToCheck.ToString();
                    NumberOfPrimenumbersCheckedTextBlock.Text = string.Empty;
                    mainPage.NotifyUser($"{ string.Empty}", NotifyType.StatusMessage);
                }
                else
                {
                    numberOfprimenumbersToCheck = 0;
                    enteredNumberOfprimenumbersToCheck = 0;
                    NumberOfPrimenumbersCheckedTextBlock.Text = string.Empty;
                    mainPage.NotifyUser($"Enter number of primenumbers to double check, please.", NotifyType.ErrorMessage);
                }
            }
            else
            {
                numberOfprimenumbersToCheck = 0;
                enteredNumberOfprimenumbersToCheck = 0;
                NumberOfPrimenumbersCheckedTextBlock.Text = string.Empty;
                mainPage.NotifyUser($"Please enter number of primenumbers to double check.", NotifyType.StatusMessage);
            }
        }

        private async void DoubleCheckDataButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            if (DoubleCheckDataButton.Content.ToString() == "Cancel")
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    doubleCheckCancellationTokenSource.Cancel();
                }
                DoubleCheckDataButton.Content = "Double check primenumbers";
                return;
            }

            if (DoubleCheckDataButton.Content.ToString() == "Double check primenumbers")
            {
                if (biggestId < 1 || numberOfprimenumbersToCheck < 1)
                {
                    mainPage.NotifyUser($"Please enter number of primenumbers to double check.", NotifyType.ErrorMessage);
                    return;
                }

                HowManyTextBox.IsEnabled = false; //disable textbox

                FinalResultTextBlock.Text = string.Empty;

                DoubleCheckDataButton.Content = "Cancel";
                ToolTipService.SetToolTip(DoubleCheckDataButton, "Cancel.");
                mainPage.NotifyUser($"Please wait or cancel.", NotifyType.StatusMessage);

                StartDoubleCheckDataProgressRing();

                try
                {
                    doubleCheckCancellationTokenSource = new CancellationTokenSource();

                    // Use ParallelOptions instance to store the CancellationToken
                    parallelOptions = new ParallelOptions();
                    parallelOptions.CancellationToken = doubleCheckCancellationTokenSource.Token;
                    parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

                    await Task.Run(async () => await DoubleCheckDataAsync()).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"You canceled the operation 'Double check primenumbers'.", NotifyType.ErrorMessage);
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
                        StopDoubleCheckDataProgressRing();
                        DoubleCheckDataButton.Content = "Double check primenumbers";
                        ToolTipService.SetToolTip(DoubleCheckDataButton, "Double check primenumbers.");
                        HowManyTextBox.IsEnabled = true; //enable textbox
                        if (doubleCheckCancellationTokenSource != null)
                        {
                            doubleCheckCancellationTokenSource.Dispose();
                            doubleCheckCancellationTokenSource = null;
                        }
                    });
                }
            }
        }

        private async Task DoubleCheckDataAsync()
        {
            while (!parallelOptions.CancellationToken.IsCancellationRequested)
            {
                int range = biggestId / numberOfprimenumbersToCheck;

                int numberOfPrimenumbersChecked = 0;

                Random random = new Random();
                int rangeStart = 2; //forget id = 1 which is primenumber = 2
                int rangeEnd = 0;

                Stopwatch stopwatch = new Stopwatch();

                bool isPrimenumber = true;
                for (int i = 1; i <= numberOfprimenumbersToCheck; i++)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    rangeEnd = rangeStart + range - 1; //exclusive

                    int randomId = random.Next(rangeStart, rangeEnd);

                    Prime randomPrime = await App.SqlPrimeRepo.GetPrimeAsync(randomId).ConfigureAwait(false);
                    if (randomPrime != null)
                    {
                        stopwatch.Restart();

                        BigInteger primenumber = BigInteger.Parse(randomPrime.Primenumber);

                        try
                        {
                            for (BigInteger divisor = 3; divisor <= (BigInteger)Math.Sqrt((double)primenumber); divisor += 2)
                            {
                                parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                                if (parallelOptions.CancellationToken.IsCancellationRequested)
                                {
                                    stopwatch.Stop();
                                    break;
                                }

                                if (primenumber % divisor == 0)
                                {
                                    stopwatch.Stop();
                                    isPrimenumber = false;
                                    break;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            for (BigInteger divisor = 3; divisor <= (primenumber + 1) / 2; divisor += 2)
                            {
                                parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                                if (parallelOptions.CancellationToken.IsCancellationRequested)
                                {
                                    stopwatch.Stop();
                                    break;
                                }

                                if (primenumber % divisor == 0)
                                {
                                    stopwatch.Stop();
                                    isPrimenumber = false;
                                    break;
                                }
                            }
                        }

                        stopwatch.Stop();

                        numberOfPrimenumbersChecked++;

                        if (isPrimenumber)
                        {
                            // Get the elapsed time as a TimeSpan value.
                            TimeSpan timeSpan = stopwatch.Elapsed;

                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                NumberOfPrimenumbersCheckedTextBlock.Text = $"{numberOfPrimenumbersChecked}";
                                PrimenumberWasCheckedTextBlock.Text = $"Double checked id {randomId} primenumber {primenumber} : elapsed time is {timeSpan.Hours:00} h {timeSpan.Minutes:00} min {timeSpan.Seconds:00} sec {timeSpan.Milliseconds:000} msec.";
                            });
                        }

                        if (!isPrimenumber)
                        {
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                PrimenumberWasCheckedTextBlock.Text = $"Checked id {randomId} primenumber {primenumber} is faulty.";
                            });
                            break;
                        }

                        rangeStart = rangeEnd + 1;
                    }
                }
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                    {
                        if (isPrimenumber)
                        {
                            FinalResultTextBlock.Text = $"No faulty primenumbers were found.";
                            mainPage.NotifyUser($"Operation was canceled.", NotifyType.ErrorMessage);
                        }
                        else
                        {
                            FinalResultTextBlock.Text = $"Faulty primenumber was found.";
                            mainPage.NotifyUser($"Faulty primenumber was found.", NotifyType.ErrorMessage);
                        }
                    }
                    else
                    {
                        if (isPrimenumber)
                        {
                            FinalResultTextBlock.Text = $"No faulty primenumbers were found.";
                            mainPage.NotifyUser($"Finished double checking.", NotifyType.StatusMessage);
                        }
                        else
                        {
                            FinalResultTextBlock.Text = $"Faulty primenumber was found.";
                            mainPage.NotifyUser($"Faulty primenumber was found.", NotifyType.ErrorMessage);
                        }
                    }
                });

                break;
            }
        }

        private void StartDoubleCheckDataProgressRing()
        {
            DoubleCheckDataProgressRing.IsActive = true;
            DoubleCheckDataProgressRing.Visibility = Visibility.Visible;
        }

        private void StopDoubleCheckDataProgressRing()
        {
            DoubleCheckDataProgressRing.IsActive = false;
            DoubleCheckDataProgressRing.Visibility = Visibility.Collapsed;
        }
    }
}
