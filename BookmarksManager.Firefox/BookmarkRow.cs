namespace BookmarksManager.Firefox
{
    public class BookmarkRow
    {
        public long Id { get; set; }
        public long Parent { get; set; }
        public long Type { get; set; }
        public long? Position { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public long? LastModified { get; set; }
        public long? DateAdded { get; set; }
        public long? LastVisit { get; set; }
        public long? VisitCount { get; set; }
        public string FaviconUrl { get; set; }
        public byte[] FaviconData { get; set; }
        public string FaviconContentType { get; set; }
    }
}