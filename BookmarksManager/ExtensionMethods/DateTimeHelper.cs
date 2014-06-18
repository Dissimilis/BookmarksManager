using System;

namespace BookmarksManager
{
    internal static class DateTimeHelper
    {
        public static int ToUnixTimestamp(this DateTime time)
        {
            return (Int32) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static DateTime FromUnixTimeStamp(int unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
        }

        public static DateTime? FromUnixTimeStamp(string unixTimeStamp)
        {
            int unixTime;
            if (!string.IsNullOrEmpty(unixTimeStamp) && int.TryParse(unixTimeStamp, out unixTime))
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime).ToLocalTime();
            }
            return null;
        }
    }
}