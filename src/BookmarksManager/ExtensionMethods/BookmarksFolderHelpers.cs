using System;
using System.Collections.Generic;
using System.Linq;

namespace BookmarksManager
{
    public static class BookmarksHelpers
    {
        /// <summary>
        ///     Finds firefox/chrome bookmarks bar in bookmarks container
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static IBookmarkFolder GetBookmarksBar(this BookmarkFolder root)
        {
            //PERSONAL_TOOLBAR_FOLDER
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            var bar = root.GetAllItems<BookmarkFolder>().FirstOrDefault(f => f.Attributes != null && f.Attributes.ContainsKey("personal_toolbar_folder"));
            return bar;
        }



    }
}
