using System;
using System.Collections.Generic;
using System.Linq;

namespace BookmarksManager
{
    public class BookmarkFolder : List<IBookmarkItem>, IBookmarkItem
    {

        public string Title { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? Added { get; set; }

        public IDictionary<string, string> Attributes = new Dictionary<string, string>();

        /// <summary>
        /// All links from all folders in flat structure
        /// </summary>
        public IEnumerable<BookmarkLink> AllLinks
        {
            get { return this.GetAllItems<BookmarkLink>(); }
        }

        /// <summary>
        /// All items (links, folders and custom IBookmarkItem objects) in flat structure
        /// </summary>
        public IEnumerable<IBookmarkItem> AllItems
        {
            get { return this.GetAllItems<IBookmarkItem>(); }
        }

        /// <summary>
        /// All folders in flat structure
        /// </summary>
        public IEnumerable<BookmarkFolder> AllFolders
        {
            get { return this.GetAllItems<BookmarkFolder>(); }
        }

        public BookmarkFolder()
        {
        }

        public BookmarkFolder(string title) : this()
        {
            Title = title;
        }


        public override string ToString()
        {
            return string.Format(">>> {0} <<<", Title);
        }
    }
}