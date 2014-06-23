using System;
using System.Collections.Generic;

namespace BookmarksManager
{
    public interface IBookmarkItem
    {
        string Title { get; set; }
    }

    public interface IBookmarkLink : IBookmarkItem
    {
        string Url { get; set; }
    }

    public interface IBookmarkFolder : IBookmarkItem,IEnumerable<IBookmarkItem>
    {

        IEnumerable<IBookmarkItem> AllItems { get; }
        IEnumerable<IBookmarkFolder> AllFolders { get; }
        void Add(IBookmarkItem item);

        IEnumerable<T> GetAllItems<T>() where T : IBookmarkItem;

    }
}