using System;
using System.Text;
using BookmarksManager;

namespace BookmarksManager.Tests
{
    public static class Helpers
    {
        public static BookmarkFolder GetSimpleStructure()
        {
            var bookmarks = new BookmarkFolder
            {
                new BookmarkFolder("Title1"),
                new BookmarkFolder("Title2"),
                new BookmarkLink("http://example.com", "Example"),
                new BookmarkFolder("Nested")
                {
                    new BookmarkLink("http://example.com", "Example2") {LastModified = new DateTime(2000, 01, 01, 12, 12, 12)},
                    new BookmarkFolder("Inner folder") {new BookmarkLink("http://localhost?foo=bar", "Local example") {Added = new DateTime(2000, 01, 01, 12, 12, 12)}}
                }
            };
            return bookmarks;
        }

        public static string RandomUnicodeString(int length)
        {
            var rnd = new Random();
            var str = new byte[length*2];
            for (var i = 0; i < length*2; i += 2)
            {
                int chr;
                switch (i%4)
                {
                    case 0:
                        chr = rnd.Next(9398, 11097);
                        break;
                    case 1:
                        chr = rnd.Next(255, 700);
                        break;
                    case 2:
                        chr = rnd.Next(255, 0x1447);
                        break;
                    default:
                        chr = rnd.Next(0x1447, 0xD7FF);
                        break;
                }
                str[i + 1] = (byte) ((chr & 0xFF00) >> 8);
                str[i] = (byte) (chr & 0xFF);
            }
            return Encoding.Unicode.GetString(str);
        }
    }
}
