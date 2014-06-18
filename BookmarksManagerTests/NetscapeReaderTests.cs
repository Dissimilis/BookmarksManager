using System;
using System.IO;
using System.Linq;
using System.Text;
using BookmarksManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace BookmarksManagerTests
{
    [TestClass]
    public class NetscapeReaderTests
    {
        private readonly NetscapeBookmarksReader _reader;

        private const string HtmlTemplate = @"<!DOCTYPE NETSCAPE-Bookmark-file-1>
    <!--This is an automatically generated file.
    It will be read and overwritten.
    Do Not Edit! -->
    <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset={1}"">
    <Title>Bookmarks</Title>
    <H1>Bookmarks</H1><DL><p>{0}</DL><p>";

        public NetscapeReaderTests()
        {
            _reader = new NetscapeBookmarksReader();
        }


        [TestMethod]
        public void EmptyContainer()
        {
            var html = string.Format(HtmlTemplate, string.Empty, "utf-8");
            var container = _reader.Read(html);
            Assert.IsNull(container.Title);
            Assert.IsNotNull(container.Attributes);
            Assert.IsFalse(container.Any());
            Assert.IsFalse(container.AllItems.Any());
        }

        [TestMethod]
        public void OneItemContainer()
        {
            var html = string.Format(HtmlTemplate, "<DT><A href='http://example.com' title='test' icon_uri=\"http://example.com\">Example</a>", "utf-8");
            var container = _reader.Read(html);
            Assert.AreEqual(1, container.Count);
            Assert.AreEqual(1, container.AllItems.Count());
        }

        [TestMethod]
        public void HtmlEntitiesParsing()
        {
            var html = string.Format(HtmlTemplate, "<DT><A href='&gt;12c%233'>&lt;&nbsp;&gt;</a>", "utf-8");
            var container = _reader.Read(html);
            Assert.AreEqual("12c%233", ((BookmarkLink) container[0]).Url);
            Assert.AreEqual("< >", container[0].Title);
        }

        [TestMethod]
        public void ItemAttributesContainer()
        {
            var html = string.Format(HtmlTemplate, "<DT><A href='http://example.com' title='test' icon_uri=\"http://example.com\">Example</a>", "utf-8");
            var container = _reader.Read(html);
            var item = (BookmarkLink) container.Single();
            Assert.AreEqual("Example", item.Title);
            Assert.AreEqual("test", item.Attributes["title"]);
        }

        [TestMethod]
        public void OneFolderContainer()
        {
            var html = string.Format(HtmlTemplate, "<DT><H3>Test folder</H3><DL><p></DL><p>", "utf-8");
            var container = _reader.Read(html);
            Assert.AreEqual(1, container.Count);
            Assert.AreEqual(1, container.AllFolders.Count());
            Assert.AreEqual("Test folder", container[0].Title);
        }

        [TestMethod]
        public void FolderAttributesContainer()
        {
            var html = string.Format(HtmlTemplate, "<DT><H3 ADD_DATE=1355307132 custom='1'>Test folder</H3><DL><p></DL><p>", "utf-8");
            var container = _reader.Read(html);
            var folder = (BookmarkFolder) container.Single();
            Assert.AreEqual(new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Utc), folder.Added, "Folder attributes was not readed");
            Assert.AreEqual("1", folder.Attributes["custom"], "Folder attributes was not readed");
        }

        [TestMethod]
        public void NestedFolders()
        {
            var html = string.Format(HtmlTemplate, "<DT><H3>f1</H3><DL><p><DT><H3>f2</H3><DL>  <dt><a title></a> </DL></DL><p>", "utf-8");
            var container = _reader.Read(html);
            Assert.AreEqual(1, container.Count);
            Assert.AreEqual(2, container.AllFolders.Count());
            Assert.AreEqual("f1", container[0].Title);
            Assert.IsTrue(container.AllFolders.Any(f => f.Title == "f2"));
            Assert.AreEqual(1, container.AllLinks.Count(l => l.Title == null && l.Attributes.ContainsKey("title")));
        }

        [TestMethod]
        public void EmbededIcon()
        {
            var html = string.Format(HtmlTemplate, "<DT><H3>f1</H3><DL><p><DT><H3>f2</H3> <dt><a Icon=data:image/jpeg;base64,YQ==></a> </DL></DL><p>", "utf-8");
            var container = _reader.Read(html);
            var embededIconItem = container.AllLinks.Single(l => !string.IsNullOrEmpty(l.IconContentType));
            Assert.AreEqual("image/jpeg", embededIconItem.IconContentType);
            Assert.IsNotNull(embededIconItem.IconData);
            Assert.IsTrue(embededIconItem.IconData.SequenceEqual(new byte[] {97}), "Decoded data must be [a] char");
        }

        [TestMethod]
        public void EncodingUtf32()
        {
            var unicodeStr = Helpers.RandomUnicodeString(147);
            var html = string.Format(HtmlTemplate, "<DT><a href='#'>" + unicodeStr + "</a>", "utf-32");
            var bytes = Encoding.UTF32.GetBytes(html);
            var ms = new MemoryStream(bytes);
            var reader = new NetscapeBookmarksReader {AutoDetectEncoding = false, InputEncoding = Encoding.UTF32};
            var container = reader.Read(ms);
            var link = container[0];
            Assert.AreEqual(unicodeStr, link.Title);
        }

        [TestMethod]
        public void EncodingAutodetectAscii()
        {
            var encoding = Encoding.GetEncoding(1257);
            var html = string.Format(HtmlTemplate, "<DT><a href='#;'>ĄČĘĖįšųū τ</a>", encoding.WebName);
            var bytes = encoding.GetBytes(html);
            var ms = new MemoryStream(bytes);
            var reader = new NetscapeBookmarksReader {AutoDetectEncoding = true};
            var container = reader.Read(ms);
            var link = container[0];
            Assert.AreEqual("ĄČĘĖįšųū ?", link.Title);
        }

        [TestMethod]
        public void EncodingAutodetectUtf32()
        {
            var encoding = Encoding.UTF32;
            var html = string.Format(HtmlTemplate, "<DT><a href='#;'>ĄČĘĖįšųū τ</a>", encoding.WebName);
            var bytes = encoding.GetBytes(html);
            var ms = new MemoryStream(bytes);
            var reader = new NetscapeBookmarksReader {AutoDetectEncoding = true};
            var container = reader.Read(ms);
            var link = container[0];
            Assert.AreEqual("ĄČĘĖįšųū τ", link.Title);
        }

        [TestMethod]
        public void EncodingAutodetectUtf16()
        {
            var encoding = Encoding.Unicode;
            var html = string.Format(HtmlTemplate, "<DT><a href='#;'>ĄČĘĖįšųū τ</a>", encoding.WebName);
            var bytes = encoding.GetBytes(html);
            var ms = new MemoryStream(bytes);
            var reader = new NetscapeBookmarksReader {AutoDetectEncoding = true};
            var container = reader.Read(ms);
            var link = container[0];
            Assert.AreEqual("ĄČĘĖįšųū τ", link.Title);
        }

        [TestMethod]
        public void EncodingAutodetectHeaderLength()
        {
            var encoding = Encoding.GetEncoding(1257);
            var html = string.Format(HtmlTemplate, "<DT><a href='#;'>ĄŽ</a>", encoding.WebName);
            var bytes = encoding.GetBytes(html);
            var ms = new MemoryStream(bytes);
            var reader = new NetscapeBookmarksReader {AutoDetectEncoding = true, HeaderLength = 10};
            var container = reader.Read(ms);
            Assert.AreNotEqual("ĄŽ", container[0].Title);
        }

        [TestMethod]
        public void FFToolbarFolder()
        {
            var html = string.Format(HtmlTemplate, "<DT><H3 personal_toolbar_folder='true'><DL><DT><a href='#'>test</a></DL>", string.Empty);
            var reader = new NetscapeBookmarksReader();
            var container = reader.Read(html);
            var ff = container.FirefoxBookmarksBar();
            Assert.IsNotNull(ff, "Firefox bookmarks toolbar was not found");
            Assert.AreEqual("test", ff[0].Title, "Firefox bookmarks toolbar was not found");
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException), "Reader must fail if invalid HTML is provided")]
        public void InvalidDocument()
        {
            var reader = new NetscapeBookmarksReader();
            reader.Read("<html><b>Nothing here</b></html>");
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException), "Reader must fail if invalid HTML is provided")]
        public void InvalidCharset()
        {
            var encoding = Encoding.Unicode;
            var html = @"<html><META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=FOO-32""><dl>Nothing here</dl></html>";
            var bytes = encoding.GetBytes(html);
            var ms = new MemoryStream(bytes);
            var reader = new NetscapeBookmarksReader {AutoDetectEncoding = true};
            reader.Read(ms);
            Assert.AreEqual(Encoding.UTF8, reader.InputEncoding, "Encoding must fallback to UTF8");
        }
    }
}
