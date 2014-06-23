using System;
using BookmarksManager.Firefox;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BookmarksManagerTests
{
    [TestClass]
    public class BookmarksReaderFirefoxTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var ffReader = new FirefoxBookmarksReader(@"C:\Users\Marius\AppData\Roaming\Mozilla\Firefox\Profiles\58g7m5fg.default\places.sqlite");
            ffReader.Read();
        }
    }
}
