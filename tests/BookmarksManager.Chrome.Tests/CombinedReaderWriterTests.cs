using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BookmarksManager;
using BookmarksManager.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BookmarksManager.Chrome.Tests
{
    [TestClass]
    public class CombinedReaderWriterTests
    {
        [TestMethod]
        public void EmptyContainer()
        {
            var emptyContainer = new BookmarkFolder();
            var writer = new NetscapeBookmarksWriter(emptyContainer);
            var reader = new NetscapeBookmarksReader();
            var write1 = writer.ToString();
            var read = reader.Read(write1);
            writer = new NetscapeBookmarksWriter(read);
            var write2 = writer.ToString();
            Assert.AreEqual(write1, write2, true);
        }

        [TestMethod]
        public void CustomAttributes()
        {
            var container = new BookmarkFolder
            {
                new BookmarkLink("a", "b") {Attributes = new Dictionary<string, string> {{"custom", "1"}}},
                new BookmarkFolder("folder") {Attributes = new Dictionary<string, string> {{"custom", "2"}, {"add_date", "ę"}}}
            };
            var writer = new NetscapeBookmarksWriter(container);
            var reader = new NetscapeBookmarksReader();
            var write1 = writer.ToString();
            var read = reader.Read(write1);
            Assert.AreEqual("1", read.AllLinks.First().Attributes["custom"]);
            Assert.AreEqual("2", read.GetAllItems<BookmarkFolder>().First().Attributes["custom"]);
            Assert.IsFalse(read.GetAllItems<BookmarkFolder>().First().Attributes.ContainsKey("add_date"), "add_date is ignored attribute, it must not be written");
        }

        [TestMethod]
        public void SimpleStructure()
        {
            var container = Helpers.GetSimpleStructure();
            container.Add(new BookmarkLink("test", "test123") {Description = "<br>"});
            var writer = new NetscapeBookmarksWriter(container);
            var reader = new NetscapeBookmarksReader();
            var write1 = writer.ToString();
            var read = reader.Read(write1);
            writer = new NetscapeBookmarksWriter(read);
            var write2 = writer.ToString();
            read = reader.Read(write2);
            Assert.AreEqual(write1, write2, true);
            Assert.IsNotNull(read.AllLinks.FirstOrDefault(l => l.Title == "test123" && l.Description == "<br>"), "Description must be preserved between reads and writes");
        }


        [TestMethod]
        public void StreamUnicode()
        {
            var container = Helpers.GetSimpleStructure();
            container.Add(new BookmarkLink("test", "ƒ"));
            var ms = new MemoryStream();
            var writer = new NetscapeBookmarksWriter(container) { OutputEncoding = Encoding.Unicode };
            writer.Write(ms);
            ms = new MemoryStream(ms.GetBuffer());
            var reader = new NetscapeBookmarksReader { AutoDetectEncoding = true };
            var read = reader.Read(ms);
            Assert.AreEqual(container.AllItems.Last().Title, read.AllItems.Last().Title);
        }
    }
}
