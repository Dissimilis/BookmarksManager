#region

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BookmarksManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace BookmarksManager.Tests
{
    [TestClass]
    public class NetscapeWritterTests
    {
        private readonly NetscapeBookmarksWriter _writter;

        private readonly Regex _headerRegex = new Regex(@"<!DOCTYPE\s+?NETSCAPE-BOOKMARK-FILE-\d+>.+?<TITLE.+?</TITLE>.+?<H1.+?</H1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly Regex _folderTagsRegex = new Regex("dl><p", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly Regex _folderHeaderRegex = new Regex("<DT><H3>(.*?)</H3>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly Regex _linkRegex = new Regex("<DT><H3>(.*?)</H3>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly Regex _h3Regex = new Regex("<H3>(.*?)</H3>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly Regex _charsetRegex = new Regex(@"charset\s*=\s*([\w-]+)", RegexOptions.IgnoreCase);

        public NetscapeWritterTests()
        {
            var emptyContainer = new BookmarkFolder();
            _writter = new NetscapeBookmarksWriter(emptyContainer);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [TestMethod]
        public void ValidHeader()
        {
            var result = _writter.ToString();
            Assert.IsTrue(_headerRegex.IsMatch(result));
        }

        [TestMethod]
        public void EmptyContainer()
        {
            var result = _writter.ToString();
            var folderTagCnt = _folderTagsRegex.Matches(result).Count;
            var itemTags = _linkRegex.IsMatch(result);
            Assert.AreEqual(2, folderTagCnt);
            Assert.IsFalse(itemTags);
        }

        [TestMethod]
        public void CharsetHeader()
        {
            _writter.OutputEncoding = Encoding.UTF32;
            var result = _writter.ToString();
            var charsetMatch = _charsetRegex.Match(result);
            Assert.IsTrue(charsetMatch.Success);
            Assert.AreEqual(charsetMatch.Groups[1].Value, "utf-32", true);
        }

        [TestMethod]
        public void SimpleStructureBasic()
        {
            var bookmarks = Helpers.GetSimpleStructure();
            var writter = new NetscapeBookmarksWriter(bookmarks);
            var result = writter.ToString();
            var folderTagCnt = _folderTagsRegex.Matches(result).Count;
            var itemTagsCnt = _linkRegex.Matches(result).Count;
            var h3Cnt = _h3Regex.Matches(result).Count;
            var folderHeaderCnt = _folderHeaderRegex.Matches(result).Count;
            Assert.AreEqual(10, folderTagCnt);
            Assert.AreEqual(4, itemTagsCnt);
            Assert.AreEqual(4, h3Cnt);
            Assert.AreEqual(4, folderHeaderCnt);
        }

        [TestMethod]
        public void StreamWritting()
        {
            var encoding = Encoding.UTF32;
            _writter.OutputEncoding = encoding;
            using (var stream = new MemoryStream())
            {
                _writter.Write(stream);
                var content = encoding.GetString(stream.ToArray());
                Assert.IsTrue(_headerRegex.IsMatch(content));
            }
        }

        [TestMethod]
        public void OutputEncoding1()
        {
            var encoding = Encoding.BigEndianUnicode;
            var bookmarks = Helpers.GetSimpleStructure();
            var unicodeStr = Helpers.RandomUnicodeString(10240);
            bookmarks.Add(new BookmarkLink("http://example.com", "Unicode title test: <" + unicodeStr));
            var writter = new NetscapeBookmarksWriter(bookmarks) {OutputEncoding = encoding};
            using (var stream = new MemoryStream())
            {
                writter.Write(stream);
                var content = encoding.GetString(stream.ToArray());
                Assert.IsTrue(content.Contains("&lt;" + unicodeStr));
            }
        }

        [TestMethod]
        public void OutputEncoding2()
        {
            var encoding = Encoding.GetEncoding(1257);
            var bookmarks = Helpers.GetSimpleStructure();
            bookmarks.Add(new BookmarkLink("http://example.com", "ASCII title test: ƒ ąčęėįšųūĄŪ"));
            var writter = new NetscapeBookmarksWriter(bookmarks) {OutputEncoding = encoding};
            using (var stream = new MemoryStream())
            {
                writter.Write(stream);
                var content = encoding.GetString(stream.ToArray());
                Assert.IsTrue(content.Contains("? ąčęėįšųūĄŪ"));
                content = Encoding.UTF8.GetString(stream.ToArray());
                Assert.IsFalse(content.Contains("ƒ"));
            }
        }

        [TestMethod]
        public void EmbededIconTest()
        {
            var bookmarks = Helpers.GetSimpleStructure();
            var randomBytes = Encoding.UTF8.GetBytes(Helpers.RandomUnicodeString(4096));
            bookmarks.Add(new BookmarkLink("http://example.com", "<\">") {IconContentType = "image/png", IconData = randomBytes});
            var writter = new NetscapeBookmarksWriter(bookmarks);
            using (var stream = new MemoryStream())
            {
                writter.Write(stream);
                var content = writter.OutputEncoding.GetString(stream.ToArray());
                Assert.IsTrue(content.Contains("data:image/png;base64,"));
                Assert.IsTrue(content.Contains(Convert.ToBase64String(randomBytes)));
            }
        }

        [TestMethod]
        public void DateTest()
        {
            var bookmarks = Helpers.GetSimpleStructure();
            var testDate = new DateTime(2039, 01, 01, 12, 12, 12);
            var unixTimeString = ((DateTimeOffset)testDate).ToUnixTimeSeconds().ToString();
            bookmarks.Add(new BookmarkLink("http://example.com", "DateTest") {Added = testDate});
            var writter = new NetscapeBookmarksWriter(bookmarks);
            using (var stream = new MemoryStream())
            {
                writter.Write(stream);
                var content = writter.OutputEncoding.GetString(stream.ToArray());
                Assert.IsTrue(content.Contains($"ADD_DATE=\"{unixTimeString}\""));
            }
        }
    }
}
