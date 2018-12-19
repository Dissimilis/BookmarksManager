using System;

namespace BookmarksManager
{
    public static class DateTimeHelper
    {
        public static int ToUnixTimestamp(this DateTime time)
        {
            //todo: use DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds;
        }

        public static DateTime? FromUnixTimeStamp(long? unixTimeStamp)
        {
            if (!unixTimeStamp.HasValue || unixTimeStamp < 1)
                return null;
            if (unixTimeStamp > 99999999999)
            {
                if (unixTimeStamp > 99999999999999) //microseconds
                    unixTimeStamp = unixTimeStamp/10000;
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((double)unixTimeStamp);
            }
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
    }
}