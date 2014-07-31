using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using BookmarksManager;
using BookmarksManager.Icebergs;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace BookmarksManagerTests
{
    [TestClass]
    public class IcebergsReaderTest
    {
        private readonly IcebergsBookmarksReader _reader;

        
        public IcebergsReaderTest()
        {
            _reader = new IcebergsBookmarksReader()
            {
                HtmlDecoder = HttpUtility.HtmlDecode
            };
        }
        

        [TestMethod]
        public void EmptyContainer()
        {
            var container = _reader.Read("[]");
            Assert.IsNotNull(container);
            Assert.IsNull(container.Title);
            Assert.IsNotNull(container.Attributes);
            Assert.IsFalse(container.Any());
            Assert.IsFalse(container.AllItems.Any());
        }

        [TestMethod]
        public void IcebergsFile()
        {
            var bookmarksFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "misc\\icebergs.json");
            using (var file = File.OpenRead(bookmarksFilePath))
            {
                var container = _reader.Read(file);
                Assert.AreEqual(2, container.Count, "Must have exactly 2 folders (icebergs)");
                Assert.IsTrue(container.GetAllItems<IcedropItem>().Count(i => i.Initial) > 40, "Must have more than 40 system created items");
                Assert.AreEqual(1, container.GetAllItems<IcedropItem>().Count(i => i.Public && i.Initial), "Must have exatly one system created public item");
                Assert.AreEqual(1, container.Count(c=>c.Title == "test"), "Must have folder (iceberg) named test");

                Assert.AreEqual(container.Count, container.AllFolders.Count(), "Must not be more than 2 levels (root > iceberg > icedrop)");
                Assert.IsTrue(container.GetAllItems<IcedropVideo>().Count(v=>v.VideoId != null && v.VideoSource != null) > 2, "Must have more than 2 video bookmarks");
                Assert.IsTrue(container.GetAllItems<IcedropImage>().Count(i=>!string.IsNullOrEmpty(i.Referrer)) > 2, "Must have more than 2 image bookmarks");
                Assert.IsTrue(container.GetAllItems<IcedropText>().Any(t=>!string.IsNullOrEmpty(t.Text)), "Must have at least one text bookmark");
                Assert.IsTrue(container.GetAllItems<IcedropUserFile>().Any(t => t.Size.HasValue), "Must have at least one user file");
                Assert.IsTrue(container.GetAllItems<IcedropNote>().Any(t => !string.IsNullOrEmpty(t.Text)), "Must have at least one note");
                Assert.IsTrue(container.AllLinks.Any(l=>l.Added.HasValue), "Must have link with date set");
                Assert.IsTrue(container.AllLinks.Any(l=>l.Added > new DateTime(2014,01,01)), "Must link not older than 2014");

                Assert.IsTrue(container.GetAllItems<IcedropItem>().Any(l => l.Comments != null && l.Comments.Any()), "Must have item with comments");

                Assert.IsTrue(container.GetAllItems<IcedropNote>().Any(l => l.Text.Contains("<div ")), "Must have note with properly decoded html content");

            }
        }
    }
}
