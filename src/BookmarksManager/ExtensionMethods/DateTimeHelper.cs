using System;

namespace BookmarksManager
{
    public static class DateTimeHelper
    {
        public static long ToUnixTimestamp(this DateTime time)
        {
            return (long)((DateTimeOffset)time).ToUnixTimeSeconds();
        }

        public static DateTime? FromUnixTimeStamp(long? unixTimeStamp)
        {
            if (!unixTimeStamp.HasValue || unixTimeStamp < 1)
                return null;
            unixTimeStamp = takeNDigits(unixTimeStamp.Value, 10);
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)unixTimeStamp);
        }

        public static DateTime? FromUnixTimeStamp(string unixTimeStamp)
        {
            if (!string.IsNullOrEmpty(unixTimeStamp) && long.TryParse(unixTimeStamp, out long unixTime))
            {
                return FromUnixTimeStamp(unixTime);
            }
            return null;
        }
        private static long takeNDigits(long number, int n)
        {
            number = Math.Abs(number);
            if (number == 0)
                return number;
            int numberOfDigits = (int)Math.Floor(Math.Log10(number) + 1);
            if (numberOfDigits >= n)
                return (int)Math.Truncate((number / Math.Pow(10, numberOfDigits - n)));
            else
                return number;
        }
    }
}