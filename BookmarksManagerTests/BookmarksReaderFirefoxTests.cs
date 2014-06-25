using System;
using System.IO;
using System.Linq;
using BookmarksManager.Firefox;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BookmarksManagerTests
{
    [TestClass]
    public class BookmarksReaderFirefoxTests
    {
        //C:\Users\Marius\AppData\Roaming\Mozilla\Firefox\Profiles\58g7m5fg.default\places.sqlite

        private string _ff_v30_places;
        private string _ff_v3_places;

        [TestInitialize]
        public void Setup()
        {
            _ff_v30_places = Path.GetFullPath("TestData\\ff30_places.sqlite");
            _ff_v3_places = Path.GetFullPath("TestData\\ff3_places.sqlite");
            //Assert.IsTrue(File.Exists(_ff_v30_places));
            //Assert.IsTrue(File.Exists(_ff_v3_places));
        }
        [TestMethod]
        public void RssFeed()
        {
            var ffReader = new FirefoxBookmarksReader(_ff_v3_places){IncludeInternal = false};
            var bookmarks = ffReader.Read();
            Assert.IsTrue(bookmarks.AllLinks.Any(l => string.Equals(l.Title, "Latest Headlines", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(l.FeedUrl)));
        }
        [TestMethod]
        public void InternalBookmarks()
        {
            var ffReader = new FirefoxBookmarksReader(_ff_v3_places) { IncludeInternal = true };
            var bookmarks = ffReader.Read();
            Assert.AreEqual(1, bookmarks.GetAllItems<FirefoxBookmarkLink>().Count(f => f.Title == "Most Visited" && f.Internal));
            Assert.AreEqual(2, bookmarks.GetAllItems<FirefoxBookmarkFolder>().Count(f=> f.Internal));
            Assert.AreEqual(3, bookmarks.GetAllItems<FirefoxBookmarkLink>().Count(f => f.Internal));
            Assert.AreEqual(1, bookmarks.AllItems.Count(f => f.Title == "All Bookmarks"));
        }
        [TestMethod]
        public void NoInternalBookmarks()
        {
            var ffReader = new FirefoxBookmarksReader(_ff_v30_places) { IncludeInternal = false };
            var bookmarks = ffReader.Read();
            Assert.AreEqual(0, bookmarks.AllItems.Count(f => f.Title == "Most Visited"));
            Assert.AreEqual(0, bookmarks.GetAllItems<FirefoxBookmarkFolder>().Count(f => f.Internal));
            Assert.AreEqual(0, bookmarks.GetAllItems<FirefoxBookmarkLink>().Count(f => f.Internal));
            Assert.AreEqual(0, bookmarks.AllItems.Count(f => f.Title == "All Bookmarks"));
            Assert.AreEqual(3, bookmarks.AllItems.Count(f => f.Title == "Unsorted Bookmarks" || f.Title == "Tags" || f.Title == "Bookmarks Toolbar"));
        }
        [TestMethod]
        public void SpecificUserBookmark()
        {
            var ffReader = new FirefoxBookmarksReader(_ff_v30_places) { IncludeInternal = false };
            var bookmarks = ffReader.Read();
            var dragdis = bookmarks.GetAllItems<FirefoxBookmarkLink>().First(l=>l.Title.StartsWith("Dragdis"));
            Assert.AreEqual("https://dragdis.com/", dragdis.Url);
            Assert.AreEqual("image/png", dragdis.IconContentType);
            Assert.IsTrue(dragdis.IconData != null);
        }
        [TestMethod]
        public void SpecificUserFolder()
        {
            var ffReader = new FirefoxBookmarksReader(_ff_v30_places) { IncludeInternal = false };
            var bookmarks = ffReader.Read();
            var folder = bookmarks.AllFolders.First(l => l.Title == "innerFolder");
            Assert.AreEqual(1, folder.AllItems.Count());
            Assert.AreEqual(0, folder.AllFolders.Count());
            Assert.IsTrue(folder.First().Title.StartsWith("Dragdis"));
        }
        [TestMethod]
        public void ExcludedFromBackupBookmarks()
        {
            var ffReader = new FirefoxBookmarksReader(_ff_v30_places) { IncludeInternal = true };
            var bookmarks = ffReader.Read();
            Assert.AreEqual(8,bookmarks.GetAllItems<IFirefoxBookmarkItem>().Count(l => l.ExcludeFromBackup));
            Assert.AreEqual(8, bookmarks.GetAllItems<IFirefoxBookmarkItem>().Count(l => l.ExcludeFromBackup && l.Internal));
            var internalNotExcluded = bookmarks.GetAllItems<IFirefoxBookmarkItem>().Where(l => l.Internal && !l.ExcludeFromBackup).ToList();
            Assert.AreEqual(3, internalNotExcluded.Count(),"Most visited, Recentry Bookmarked and Recent tags must not be excluded");
            Assert.IsInstanceOfType(internalNotExcluded.Single(l => l.Title == "Most Visited"), typeof(FirefoxBookmarkLink), "Most visited must be present");
        }

    }
}
