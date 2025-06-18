namespace BookmarksManager.Firefox
{
    public class FirefoxBookmarkFolder : BookmarkFolder, IFirefoxBookmarkItem
    {
        /// <summary>
        /// Internal Firefox bookmark id
        /// </summary>
        public long Id { get; set; }

        //Folder description
        public string Description { get; set; }

        /// <summary>
        /// Indicates if this folder is displayed in Firefox bookmarks toolbar
        /// </summary>
        public bool IsBookmarksToolbar { get; set; }

        /// <summary>
        /// Internal Firefox bookmarks type (menu,tags,unfiled,toolbar)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Indicates if this is internal Firefox bookmark not created by user (i.e. Most visited, History, etc)
        /// Tags, Unsorted bookmarks, Bookmarks toolbar and Bookmarks menu are not considered internal
        /// </summary>
        public bool Internal { get; set; }

        /// <summary>
        /// Use this property to determine witch bookmarks should be exportable. It is set based on internal FF attribute [places/excludeFromBackup]
        /// </summary>
        public bool ExcludeFromBackup { get; set; }

    }

    public interface IFirefoxBookmarkItem : IBookmarkItem
    {
        long Id { get; set; }
        string Description { get; set; }
        bool Internal { get; set; }
        bool ExcludeFromBackup { get; set; }
    }
}