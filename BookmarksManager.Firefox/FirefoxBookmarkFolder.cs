namespace BookmarksManager.Firefox
{
    public class FirefoxBookmarkFolder : BookmarkFolder
    {
        /// <summary>
        /// Internal Firefox bookmark id
        /// </summary>
        public long Id { get; set; }
        
        /// <summary>
        /// Indicates if this folder is displayed in Firefox bookmarks toolbar 
        /// </summary>
        public bool IsBoomarksToolbar { get; set; }

        /// <summary>
        /// Internal Firefox bookmarks type (menu,tags,unfiled,toolbar)
        /// </summary>
        public string Type { get; set; }
    }
}