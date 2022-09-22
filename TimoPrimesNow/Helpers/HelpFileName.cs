using System;

namespace TimoPrimesNow.Helpers
{
    internal class HelpFileName
    {
        public static string GetNewFileName(string fileName)
        {
            var dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{fileName}_{dateTime}.json";
        }
    }
}
