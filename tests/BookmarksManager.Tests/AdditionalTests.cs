using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using BookmarksManager;
namespace BookmarksManager.Tests
{
    [TestClass]
    public class AdditionalTests
    {
        [TestMethod]
        public void BookmarkLinkUriConstructor()
        {
            var uri = new Uri("https://example.com/path");
            var link = new BookmarkLink(uri, "Example");
            Assert.AreEqual(uri.AbsoluteUri, link.Url);
            Assert.AreEqual("Example", link.Title);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BookmarkLinkUriConstructorNullCheck()
        {
            _ = new BookmarkLink((Uri)null, "Title");
        }

        [TestMethod]
        public void BookmarkLinkToString()
        {
            var link = new BookmarkLink("https://example.com", "Title");
            Assert.AreEqual("Title (https://example.com)", link.ToString());
        }

        [TestMethod]
        public void BookmarkFolderToString()
        {
            var folder = new BookmarkFolder("MyFolder");
            Assert.AreEqual(">>> MyFolder <<<", folder.ToString());
        }

        [TestMethod]
        public void GetBookmarksBarFindsToolbar()
        {
            var root = new BookmarkFolder
            {
                new BookmarkFolder("Folder1"),
                new BookmarkFolder("Toolbar")
                {
                    Attributes = { ["personal_toolbar_folder"] = "true" }
                }
            };
            var bar = root.GetBookmarksBar();
            Assert.IsNotNull(bar);
            Assert.AreEqual("Toolbar", bar.Title);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetBookmarksBarNullCheck()
        {
            BookmarkFolder root = null;
            _ = root.GetBookmarksBar();
        }

        [TestMethod]
        public void ToUnixTimestampRoundtrip()
        {
            var now = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long ts = now.ToUnixTimestamp();
            var restored = DateTimeHelper.FromUnixTimeStamp(ts);
            Assert.AreEqual(now, restored);
        }
    }
}
