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
            get { return GetAllIems<BookmarkLink>(this); }
        }

        /// <summary>
        /// All items (links and folders) in flat structure
        /// </summary>
        public IEnumerable<IBookmarkItem> AllItems
        {
            get { return GetAllIems<IBookmarkItem>(this); }
        }

        /// <summary>
        /// All folders in flat structure
        /// </summary>
        public IEnumerable<BookmarkFolder> AllFolders
        {
            get { return AllItems.OfType<BookmarkFolder>(); }
        }

        public BookmarkFolder()
        {
        }

        public BookmarkFolder(string title) : this()
        {
            Title = title;
        }

        private IEnumerable<T> GetAllIems<T>(IEnumerable<IBookmarkItem> folder) where T : IBookmarkItem
        {
            foreach (var item in folder)
            {
                if (item is BookmarkFolder)
                {
                    if (typeof(T) == typeof(BookmarkFolder) || typeof(T) == typeof(IBookmarkItem))
                        yield return (T)item;
                    foreach (var innerItem in GetAllIems<T>(item as BookmarkFolder))
                    {
                        yield return innerItem;
                    }
                }
                else
                {
                    yield return (T)item;
                }
            }
        }



        public override string ToString()
        {
            return string.Format(">>> {0} <<<", Title);
        }
    }
}