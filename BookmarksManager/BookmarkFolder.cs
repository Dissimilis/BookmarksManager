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
            get { return GetAllItems<BookmarkLink>(this); }
        }

        /// <summary>
        /// All items (links and folders) in flat structure
        /// </summary>
        public IEnumerable<IBookmarkItem> AllItems
        {
            get { return GetAllItems<IBookmarkItem>(this); }
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

        /// <summary>
        /// Returns all items of <typeparamref name="T"/> in flat structure 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAllItems<T>() where T : IBookmarkItem
        {
            return GetAllItems<T>(this);
        }

        private IEnumerable<T> GetAllItems<T>(IEnumerable<IBookmarkItem> folder) where T : IBookmarkItem
        {
            foreach (var item in folder)
            {
                if(typeof(T) == item.GetType() || typeof(T) == typeof(IBookmarkItem))
                {
                    yield return (T)item;
                }
                var innerFolder = item as IEnumerable<IBookmarkItem>;
                if (innerFolder != null)
                {
                    foreach (var innerItem in GetAllItems<T>(innerFolder))
                    {
                        yield return innerItem;
                    }
                }
            }
        }

        public override string ToString()
        {
            return string.Format(">>> {0} <<<", Title);
        }
    }
}