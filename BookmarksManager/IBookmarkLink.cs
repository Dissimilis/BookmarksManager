namespace BookmarksManager
{
    public interface IBookmarkLink : IBookmarkItem
    {
        string Url { get; set; }
    }
}