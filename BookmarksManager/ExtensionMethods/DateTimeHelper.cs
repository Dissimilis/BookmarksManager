using System;

namespace BookmarksManager
{
    public static class DateTimeHelper
    {
        public static int ToUnixTimestamp(this DateTime time)
        {
            return (Int32) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static DateTime? FromUnixTimeStamp(long? unixTimeStamp)
        {
            if (!unixTimeStamp.HasValue || unixTimeStamp < 1)
                return null;
            if (unixTimeStamp > 99999999999)
            {
                if (unixTimeStamp > 99999999999999) //microseconds
                    unixTimeStamp = unixTimeStamp/10000;
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((double)unixTimeStamp).ToLocalTime();
            }
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)unixTimeStamp).ToLocalTime();
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