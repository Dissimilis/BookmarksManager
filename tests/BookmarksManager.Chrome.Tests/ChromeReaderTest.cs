using System;
using System.IO;
using System.Linq;
using System.Text;
using BookmarksManager;
using BookmarksManager.Chrome;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BookmarksManager.Chrome.Tests
{
    [TestClass]
    public class ChromeReaderTest
    {
        private readonly ChromeBookmarksReader _reader;

        private string _basicJson;
        
        public ChromeReaderTest()
        {
            _basicJson = @"Ww0KICB7DQogICAgImNoaWxkcmVuIjogWw0KICAgICAgew0KICAgICAgICAiY2hpbGRyZW4iOiBbDQogICAgICAgICAgew0KICAgICAgICAgICAgImRhdGVBZGRlZCI6IDEzNzYzMzE0MjEyODAsDQogICAgICAgICAgICAiaWQiOiAiMTc2MSIsDQogICAgICAgICAgICAiaW5kZXgiOiAwLA0KICAgICAgICAgICAgInBhcmVudElkIjogIjE3NjQiLA0KICAgICAgICAgICAgInRpdGxlIjogIk9uZSBiaWxsaW9uIGRvbGxhciBleHRlbnNpb24hIiwNCiAgICAgICAgICAgICJ1cmwiOiAiaHR0cHM6Ly9leGFtcGxlLmNvbSINCiAgICAgICAgICB9DQogICAgICAgIF0sDQogICAgICAgICJkYXRlQWRkZWQiOiAxMzc2OTg1MDc2MTQ0LA0KICAgICAgICAiZGF0ZUdyb3VwTW9kaWZpZWQiOiAxMzk5OTc5MzY2NDQ2LA0KICAgICAgICAiaWQiOiAiMTc2NCIsDQogICAgICAgICJpbmRleCI6IDEsDQogICAgICAgICJwYXJlbnRJZCI6ICIxIiwNCiAgICAgICAgInRpdGxlIjogIkFERE9uIGJ1Z2FpIg0KICAgICAgfSwNCiAgICAgIHsNCiAgICAgICAgImRhdGVBZGRlZCI6IDEzNTQ1NzQxMzUyNjUsDQogICAgICAgICJkYXRlR3JvdXBNb2RpZmllZCI6IDEzOTQxNzM1MTMzOTQsDQogICAgICAgICJpZCI6ICI5NDMiLA0KICAgICAgICAiaW5kZXgiOiAyLA0KICAgICAgICAicGFyZW50SWQiOiAiMSIsDQogICAgICAgICJ0aXRsZSI6ICItLU9OIEFJUi0tIg0KICAgICAgfQ0KICAgIF0sDQogICAgImRhdGVBZGRlZCI6IDEzNTE5Njk3NDk2MDAsDQogICAgImRhdGVHcm91cE1vZGlmaWVkIjogMTQwNTk1MTE0OTE2MCwNCiAgICAiaWQiOiAiMSIsDQogICAgImluZGV4IjogMCwNCiAgICAicGFyZW50SWQiOiAiMCIsDQogICAgInRpdGxlIjogIkJvb2ttYXJrcyBiYXIiDQogIH0sDQogIHsNCiAgICAiY2hpbGRyZW4iOiBbDQogICAgICB7DQogICAgICAgICJjaGlsZHJlbiI6IFsNCiAgICAgICAgICB7DQogICAgICAgICAgICAiZGF0ZUFkZGVkIjogMTM1MTk3MjExMzg4MywNCiAgICAgICAgICAgICJpZCI6ICI5MDQiLA0KICAgICAgICAgICAgImluZGV4IjogMCwNCiAgICAgICAgICAgICJwYXJlbnRJZCI6ICI5MDMiLA0KICAgICAgICAgICAgInRpdGxlIjogIkluY3JlZGlibGUgU3RhcnRQYWdlIFNldHRpbmdzIiwNCiAgICAgICAgICAgICJ1cmwiOiAiY2hyb21lLWV4dGVuc2lvbjovL3p6ei9vcHRpb25zLmh0bWwiDQogICAgICAgICAgfQ0KICAgICAgICBdLA0KICAgICAgICAiZGF0ZUFkZGVkIjogMTM1MTk3MjExMzg4MywNCiAgICAgICAgImRhdGVHcm91cE1vZGlmaWVkIjogMTM2MTUxNzE4NTg4NywNCiAgICAgICAgImlkIjogIjkwMyIsDQogICAgICAgICJpbmRleCI6IDAsDQogICAgICAgICJwYXJlbnRJZCI6ICI5MDIiLA0KICAgICAgICAidGl0bGUiOiAiRXh0ZW5zaW9uIFNldHRpbmdzIg0KICAgICAgfQ0KICAgIF0sDQogICAgImRhdGVBZGRlZCI6IDEzNTE5Njk3NDk2MDAsDQogICAgImRhdGVHcm91cE1vZGlmaWVkIjogMTM2MTUxNzE4NTg4NiwNCiAgICAiaWQiOiAiOTAyIiwNCiAgICAiaW5kZXgiOiAxLA0KICAgICJwYXJlbnRJZCI6ICIwIiwNCiAgICAidGl0bGUiOiAiT3RoZXIgYm9va21hcmtzIg0KICB9LA0KICB7DQogICAgImNoaWxkcmVuIjogWw0KICAgICAgew0KICAgICAgICAiZGF0ZUFkZGVkIjogMTM5NDg3NjE0MDUzMywNCiAgICAgICAgImlkIjogIjI0OTgiLA0KICAgICAgICAiaW5kZXgiOiAwLA0KICAgICAgICAicGFyZW50SWQiOiAiOTA1IiwNCiAgICAgICAgInRpdGxlIjogImV4YW1wbGUubmV0IiwNCiAgICAgICAgInVybCI6ICJodHRwOi8vZXhhbXBsZS5udW1iZXIudHdvIg0KICAgICAgfQ0KICAgIF0sDQogICAgImRhdGVBZGRlZCI6IDEzNTE5Njk3NDk2MDAsDQogICAgImRhdGVHcm91cE1vZGlmaWVkIjogMTM5NDg3NjE0MDUzMywNCiAgICAiaWQiOiAiOTA1IiwNCiAgICAiaW5kZXgiOiAyLA0KICAgICJwYXJlbnRJZCI6ICIwIiwNCiAgICAidGl0bGUiOiAiTW9iaWxlIGJvb2ttYXJrcyINCiAgfQ0KXQ==";
            _basicJson = Encoding.UTF8.GetString(Convert.FromBase64String(_basicJson));
            _reader = new ChromeBookmarksReader();
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
        public void ComplicatedContainer()
        {
            var container = _reader.Read(_basicJson);
            Assert.AreEqual(3, container.Count);
            Assert.AreEqual(9, container.AllItems.Count());
            Assert.AreEqual(1, container.Count(f => string.Equals(f.Title, "Mobile bookmarks",StringComparison.InvariantCultureIgnoreCase)));
        }

        [TestMethod]
        public void ChromeBookmarksFile()
        {
            var bookmarksFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome\\User Data\\Default\\Bookmarks");
            using (var file = File.OpenRead(bookmarksFilePath))
            {
                var container = _reader.Read(file);
                Assert.AreEqual(3, container.Count);
                Assert.IsTrue(container.AllLinks.Count() > 3, "Chrome must have more than 3 bookmarks");
                Assert.IsTrue(container.AllFolders.Count() >= 3, "Chrome must have at least 3 bookmarks folders");
                Assert.AreEqual(container.AllItems.Count(), container.AllFolders.Count()+container.AllLinks.Count());

                Assert.IsTrue(container.AllLinks.Any(l => l.Added.HasValue && l.Added.Value < DateTime.Now), "Chrome bookmarks must contain at least one bookmark which is added before DateTime.Now");
            }
        }
    }
}
