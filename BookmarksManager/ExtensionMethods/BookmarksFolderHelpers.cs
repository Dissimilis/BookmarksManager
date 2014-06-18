using System;
using System.Linq;

namespace BookmarksManager
{
    public static class BookmarksFolderHelpers
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
    }
}
