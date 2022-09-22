using Windows.Storage;

namespace TimoPrimesNow.Helpers
{
    internal class HelpLocalSettings
    {
        private static ApplicationDataContainer localSettings { get; set; } = ApplicationData.Current.LocalSettings;

        internal static void SaveCurrentNumberOfRecordsInOneSet(object selectedValue)
        {
            localSettings.Values["CurrentNumberOfRecordsInOneSet"] = selectedValue;
        }

        internal static int RetrieveCurrentNumberOfRecordsInOneSet(int currentNumberOfRecords)
        {
            var value = localSettings.Values["CurrentNumberOfRecordsInOneSet"];
            if (value != null)
            {
                currentNumberOfRecords = int.Parse(value.ToString());
            }
            return currentNumberOfRecords;
        }
    }
}
