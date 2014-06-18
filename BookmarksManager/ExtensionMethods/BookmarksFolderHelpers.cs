using System;
using System.Collections.Generic;
using System.Linq;

namespace BookmarksManager
{
    public static class BookmarksHelpers
    {
        /// <summary>
        ///     Finds forefox bookmarks bar in bookmarks container
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static BookmarkFolder FirefoxBookmarksBar(this BookmarkFolder root)
        {
            //PERSONAL_TOOLBAR_FOLDER
            if (root == null)
                throw new ArgumentNullException("root");
            var bar = root.AllFolders.FirstOrDefault(f => f.Attributes != null && f.Attributes.ContainsKey("personal_toolbar_folder"));
            return bar;
        }

        /// <summary>
        /// Returns all items of <typeparamref name="T"/> in flat structure 
        /// </summary>
        /// <typeparam name="T">Specifies what type of items to return</typeparam>
        /// <returns>Flattened list of <typeparamref name="T"/> items</returns>
        public static IEnumerable<T> GetAllItems<T>(this IEnumerable<IBookmarkItem> folder) where T : IBookmarkItem
        {
            foreach (var item in folder)
            {
                if (typeof(T) == item.GetType() || typeof(T) == typeof(IBookmarkItem))
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

    }
}
