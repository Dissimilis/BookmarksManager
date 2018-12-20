using System;
using System.Collections.Generic;


namespace BookmarksManager
{
    public class BookmarkLink : IBookmarkLink
    {
        public string Title { get; set; }
        public string Url { get; set; }

        /// <summary>
        /// URL of RSS feed
        /// </summary>
        public string FeedUrl { get; set; }

        /// <summary>
        ///     favicon URL
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        ///     favicon content if it's embedded icon
        /// </summary>
        public byte[] IconData { get; set; }

        /// <summary>
        ///     favicon content type if it's embedded icon
        /// </summary>
        public string IconContentType { get; set; }


        public DateTime? LastVisit { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? Added { get; set; }
        public string Description { get; set; }

        public IDictionary<string, string> Attributes = new Dictionary<string, string>();

        public BookmarkLink(string url = null, string title = null)
        {
            Url = url;
            Title = title;
        }

        public BookmarkLink(Uri url, string title = null)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            Url = url.AbsoluteUri;
            Title = title;
        }

        public override string ToString()
        {
            return $"{Title} ({Url})";
        }
    }
}