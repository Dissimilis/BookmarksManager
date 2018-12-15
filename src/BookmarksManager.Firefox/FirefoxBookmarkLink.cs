namespace BookmarksManager.Firefox
{
    public class FirefoxBookmarkLink : BookmarkLink, IFirefoxBookmarkItem
    {
        /// <summary>
        /// Internal Firefox bookmark id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// How many times user visided this site
        /// </summary>
        public int? VisitCount { get; set; }

        /// <summary>
        /// Indicates if this is internal Firefox bookmark not created by user (i.e. Most visited, History, etc)
        /// </summary>
        public bool Internal { get; set; }

        /// <summary>
        /// Use this property to determine witch bookmarks should be exportable. It is set based on internal FF attribute [places/excludeFromBackup]
        /// </summary>
        public bool ExcludeFromBackup { get; set; }
    }
}