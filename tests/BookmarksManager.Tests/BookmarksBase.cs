using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BookmarksManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BookmarksManager.Tests
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

        [TestMethod]
        public void CustomFolderType()
        {
            var bookmarks = new BookmarkFolder()
            {
                new BookmarkFolder("nonemptyFolder")
                {
                    new BookmarkFolder("emptyFolder"),
                    new BookmarkLink("url", "title2"),
                    new CustomItem(){Title = "customTitle", Message = "msg"},
                    new CustomFolder("customFolder","customProperty")
                    {
                        new BookmarkLink("url","title3"),
                        new BookmarkFolder("emptyFolder"),
                        new CustomItem(){Title = "customTitle", Message = "msg3"}
                    },
                    new CustomFolder2(){CustomTitle = "custom"}

                },
                new BookmarkFolder("emptyFolder"),
                new BookmarkLink("url", "title"),
                
            };
            bookmarks.Add(new CustomItem() {Title = "customTitle", Message = "msg2"});
            Assert.AreEqual(12, bookmarks.AllItems.ToList().Count());
            
            Assert.AreEqual(3, bookmarks.AllLinks.ToList().Count());
            Assert.AreEqual(3, bookmarks.AllFolders.Count(f => f.Title == "emptyFolder"));
            var customFolders = bookmarks.GetAllItems<CustomFolder>();
            Assert.IsNotNull(customFolders);
            Assert.IsTrue(customFolders.Any(c=>c.Count == 3));
            Assert.AreEqual(1,bookmarks.GetAllItems<CustomFolder2>().Count(c => c.CustomTitle == "custom"));
        }


        class CustomItem : IBookmarkItem
        {
            public string Title { get; set; }
            public string Message { get; set; }
        }
        class CustomFolder : List<IBookmarkItem>, IBookmarkFolder
        {
            public string Title { get; set; }
            public string CustomProperty { get; set; }

            public CustomFolder(string title, string s)
            {
                Title = title;
                CustomProperty = s;
            }

            public IEnumerable<IBookmarkItem> AllItems { get; private set; }
            public IEnumerable<IBookmarkFolder> AllFolders { get; private set; }
            public IEnumerable<T> GetAllItems<T>() where T : class, IBookmarkItem
            {
                throw new NotImplementedException();
            }
        }

        class CustomFolder2 : BookmarkFolder
        {
            public string CustomTitle { get; set; }
            
        }

    }
}
