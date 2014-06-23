using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BookmarksManager
{
    public class BookmarkFolder : Collection<IBookmarkItem>, IBookmarkFolder
    {

        /// <summary>
        /// Bookmark title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Time when bookmark was edited in browser
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Time when bookmark was added to browser
        /// </summary>
        public DateTime? Added { get; set; }

        private IDictionary<string, string> _attributes;
        public IDictionary<string, string> Attributes
        {
            get { return _attributes ?? (_attributes = new Dictionary<string, string>()); }
            set { _attributes = value; }
        }

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
        public IEnumerable<IBookmarkFolder> AllFolders
        {
            get { return this.GetAllItems<IBookmarkFolder>(); }
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
        /// <typeparam name="T">Specifies what type of items to return</typeparam>
        /// <returns>Flattened list of <typeparamref name="T"/> items</returns>
        public virtual IEnumerable<T> GetAllItems<T>() where T : class,IBookmarkItem
        {
            return this.GetAllItems<T>(this);
        }


        private IEnumerable<T> GetAllItems<T>(IBookmarkFolder folder) where T : class,IBookmarkItem
        {
            foreach (var item in folder)
            {
                var returnItem = item as T;
                if (returnItem != null)
                {
                    yield return (T)item;
                }
                var innerFolder = item as IBookmarkFolder;
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