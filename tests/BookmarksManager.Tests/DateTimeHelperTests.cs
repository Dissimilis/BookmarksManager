using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BookmarksManager;

namespace BookmarksManager.Tests
{
    [TestClass]
    public class DateTimeHelperTests
    {
        [TestMethod]
        public void Truncates13DigitLongTimestamp()
        {
            long timestampMs = 1640995200123;
            var expected = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = DateTimeHelper.FromUnixTimeStamp(timestampMs);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Truncates13DigitStringTimestamp()
        {
            string timestampMs = "1640995200123";
            var expected = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = DateTimeHelper.FromUnixTimeStamp(timestampMs);
            Assert.AreEqual(expected, result);
        }
    }
}
