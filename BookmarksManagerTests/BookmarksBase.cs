using System;
using System.Linq;
using BookmarksManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BookmarksManagerTests
{
    [TestClass]
    public class BookmarksBase
    {

        [TestMethod]
        public void FilteringFolders()
        {
            var bookmarks = new BookmarkFolder()
            {
                new BookmarkFolder("nonemptyFolder")
                {
                    new BookmarkFolder("emptyFolder"),
                    new BookmarkLink("url", "title2"),
                },
                new BookmarkFolder("emptyFolder"),
                new BookmarkLink("url", "title")

            };
            Assert.AreEqual(3, bookmarks.AllFolders.Count());
            Assert.AreEqual(2,bookmarks.AllFolders.Count(f=>f.Title == "emptyFolder"));
            Assert.AreEqual(1, bookmarks.AllFolders.First().AllFolders.Count());
        }
        [TestMethod]
        public void FilteringLinks()
        {
            var bookmarks = new BookmarkFolder()
            {
                new BookmarkFolder("nonemptyFolder")
                {
                    new BookmarkFolder("emptyFolder"),
                    new BookmarkLink("url", "title2"),
                },
                new BookmarkFolder("emptyFolder"),
                new BookmarkLink("url", "title")

            };
            Assert.AreEqual(5, bookmarks.AllItems.Count());
            Assert.AreEqual(2, bookmarks.AllLinks.Count());
            Assert.AreEqual(1, bookmarks.AllLinks.Count(l => l.Title == "title2"));
            Assert.AreEqual(2, bookmarks.AllLinks.Count(l=>l.Url == "url"));
        }

        [TestMethod]
        public void CustomItemType()
        {
            
            var bookmarks = new BookmarkFolder()
            {
                new BookmarkFolder("nonemptyFolder")
                {
                    new BookmarkFolder("emptyFolder"),
                    new BookmarkLink("url", "title2"),
                    new CustomItem(){Title = "customTitle", Message = "msg"}

                },
                new BookmarkFolder("emptyFolder"),
                new BookmarkLink("url", "title"),
                new CustomItem(){Title = "customTitle", Message = "msg2"}

            };
            Assert.AreEqual(7, bookmarks.AllItems.ToList().Count());
            Assert.AreEqual(2, bookmarks.AllLinks.ToList().Count());
            
        }

        class CustomItem : IBookmarkItem
        {
            public string Title { get; set; }
            public string Message { get; set; }
        }

    }
}
