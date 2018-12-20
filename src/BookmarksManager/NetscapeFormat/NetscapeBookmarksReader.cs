using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Majestic12;

namespace BookmarksManager
{
    /// <summary>
    ///     This class is used for bookmarks container deserialization from Netscape bookmarks format
    ///     Netscape bookmarks format is de facto standard for importing/exporting bookmarks from browsers
    ///     Format is described here: http://msdn.microsoft.com/en-us/library/aa753582%28v=vs.85%29.aspx
    /// </summary>
    public class NetscapeBookmarksReader : BookmarksReaderBase<BookmarkFolder>
    {
        /// <summary>
        ///     Length of the file header for encoding detection (in chars, not bytes).
        ///     0 to use full file. Default - 512 chars
        /// </summary>
        public int HeaderLength { get; set; }

        /// <summary>
        ///     Tries to detect and correct encoding based on HTML meta tag and BOM. Default is true.
        /// </summary>
        public bool AutoDetectEncoding { get; set; }


        protected Regex CharsetRegex = new Regex(@"charset\s*=\s*([\w-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected Regex ValidateRegex = new Regex(@"<DL\s*>.+?</DL\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        ///     Gets encoding from HTML meta tag
        /// </summary>
        /// <param name="content">HTML string</param>
        /// <returns>Encoding</returns>
        protected virtual Encoding GetEncoding(string content)
        {
            var m = CharsetRegex.Match(content);
            if (m.Success)
            {
                var charset = m.Groups[1].Value;
                try
                {
                    return Encoding.GetEncoding(charset);
                }
                catch (ArgumentException)
                {
                    return Encoding.UTF8;
                }
            }
            return Encoding.UTF8;
        }

        public NetscapeBookmarksReader()
        {
            HeaderLength = 512;
            AutoDetectEncoding = true;
        }

        /// <summary>
        ///     Creates bookmarks container from string
        /// </summary>
        /// <param name="inputString">String with HTML</param>
        /// <returns>Bookmarks container</returns>
        public override BookmarkFolder Read(string inputString)
        {
            var isValid = Validate(inputString);
            if (!isValid)
                throw new InvalidOperationException("Initial validation failed. Content is probably not well formed Netscape bookmark format or invalid encoding was used");
            var contentBytes = Encoding.UTF8.GetBytes(inputString);
            return Parse(contentBytes);
        }

        /// <summary>
        ///     Creates bookmarks container from stream
        ///     If AutoDetectEncoding is true, tries to use encoding from HTML headers
        /// </summary>
        /// <param name="inputStream">Stream containing valid HTML</param>
        /// <returns>Bookmarks container</returns>
        public override BookmarkFolder Read(Stream inputStream)
        {
            using (var ms = new MemoryStream())
            {
                inputStream.CopyTo(ms);
                var content = ms.ToArray();
                if (AutoDetectEncoding)
                {
                    InputEncoding = content.GetEncoding();
                    var headerLengthBytes = HeaderLength*InputEncoding.GetMaxByteCount(1);
                    var toRead = headerLengthBytes > 0 && headerLengthBytes < content.Length ? headerLengthBytes : content.Length;
                    var header = InputEncoding.GetString(content, 0, toRead);
                    InputEncoding = GetEncoding(header);
                }
                return Read(InputEncoding.GetString(content, 0, content.Length));
            }
        }

        protected virtual bool Validate(string content)
        {
            return ValidateRegex.IsMatch(content);
        }

        #region parsing

        private BookmarkFolder Parse(byte[] content)
        {
            var parser = new HTMLparser(content) {DecodeEntities = true};
            var rootFolder = ParseFolder(parser, null, true);
            return rootFolder;
        }

        private BookmarkFolder ParseFolder(HTMLparser parser, BookmarkFolder folderBase, bool root = false)
        {
            var folder = folderBase ?? new BookmarkFolder();
            folderBase = null;
            AssignFolderAttributes(folder, folder.Attributes);
            HTMLchunk chunk;
            while ((chunk = parser.ParseNext()) != null)
            {
                if (chunk.Type == HTMLchunkType.OpenTag && chunk.Tag == "dt")
                {
                    var item = ParseItem(parser);
                    if (item != null)
                    {
                        if (item is BookmarkFolder)
                        {
                            folderBase = item as BookmarkFolder;
                        }
                        else
                        {
                            folder.Add(item);
                        }
                    }
                }
                else if (chunk.IsOpenTag && chunk.Tag == "dl")
                {
                    if (root)
                    {
                        folder = ParseFolder(parser, folderBase);
                        root = false;
                    }
                    else
                    {
                        var newFolder = ParseFolder(parser, folderBase);
                        folder.Add(newFolder);
                    }
                }
                else if (chunk.IsCloseTag && chunk.Tag == "dl")
                {
                    return folder;
                }
            }
            return folder;
        }

        private IBookmarkItem ParseItem(HTMLparser parser)
        {
            BookmarkLink item = null;
            HTMLchunk chunk, prevChunk = parser.CurrentChunk;
            while ((chunk = parser.ParseNext()) != null)
            {
                if (chunk.IsOpenTag && chunk.Tag == "a")
                {
                    item = new BookmarkLink();
                    AssignLinkAttributes(item, chunk.oParams);
                    item.Title = GetTextOrDontMove(parser);
                }
                else if (chunk.IsOpenTag && chunk.Tag == "dd" && item != null)
                {
                    item.Description = ParseDescription(parser);
                }
                else if (chunk.IsOpenTag && chunk.Tag == "h3")
                {
                    var folder = new BookmarkFolder();
                    AssignFolderAttributes(folder, chunk.oParams);
                    folder.Title = GetTextOrDontMove(parser);
                    return folder;
                }
                else if ((chunk.IsOpenTag && chunk.Tag == "dt") || chunk.Tag == "dl")
                {
                    parser.StepBack(prevChunk);
                    break;
                }
                prevChunk = chunk;
            }
            return item;
        }

        private string GetTextOrDontMove(HTMLparser parser)
        {
            var textChunk = parser.ParseNext();
            if (textChunk.IsText)
                return textChunk.HTML;
            parser.StepBack(textChunk);
            return null;
        }

        private void AssignLinkAttributes(BookmarkLink link, IEnumerable<KeyValuePair<string, string>> attributes)
        {
            if (attributes == null)
                return;
            foreach (var attr in attributes)
            {
                switch (attr.Key)
                {
                    case "href":
                        link.Url = attr.Value;
                        break;
                    case "last_modified":
                        link.LastModified = DateTimeHelper.FromUnixTimeStamp(attr.Value);
                        break;
                    case "add_date":
                        link.Added = DateTimeHelper.FromUnixTimeStamp(attr.Value);
                        break;
                    case "last_visited":
                        link.LastVisit = DateTimeHelper.FromUnixTimeStamp(attr.Value);
                        break;
                    case "icon":
                        link.IconData = DecodeEmbeddedIcon(attr.Value, out var contentType);
                        link.IconContentType = contentType;
                        break;
                    case "icon_uri":
                        link.IconUrl = attr.Value;
                        break;
                    case "feedurl":
                        link.FeedUrl = attr.Value;
                        break;
                }
                if (!link.Attributes.ContainsKey(attr.Key))
                    link.Attributes.Add(attr.Key, attr.Value);
            }
        }

        private void AssignFolderAttributes(BookmarkFolder folder, IEnumerable<KeyValuePair<string, string>> attributes)
        {
            if (attributes == null)
                return;
            foreach (var attr in attributes)
            {
                switch (attr.Key)
                {
                    case "last_modified":
                        folder.LastModified = DateTimeHelper.FromUnixTimeStamp(attr.Value);
                        break;
                    case "add_date":
                        folder.Added = DateTimeHelper.FromUnixTimeStamp(attr.Value);
                        break;
                }
                if (!folder.Attributes.ContainsKey(attr.Key))
                    folder.Attributes.Add(attr.Key, attr.Value);
            }
        }

        private byte[] DecodeEmbeddedIcon(string data, out string contentType)
        {
            contentType = null;
            if (string.IsNullOrEmpty(data))
                return null;
            var parts = data.Split(',', ':', ';');
            if (parts.Length != 4)
                return null;
            contentType = parts[1];
            if ("base64".Equals(parts[2], StringComparison.CurrentCultureIgnoreCase))
            {
                return Convert.FromBase64String(parts[3]);
            }
            return null;
        }

        private string ParseDescription(HTMLparser parser)
        {
            var chunk = parser.ParseNext();
            if (chunk != null && chunk.IsText && !string.IsNullOrWhiteSpace(chunk.HTML))
                return chunk.HTML.Trim();
            return null;
        }

        #endregion
    }
}