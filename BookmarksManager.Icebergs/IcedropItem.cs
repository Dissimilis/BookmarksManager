using System;
using System.Collections.Generic;

namespace BookmarksManager.Icebergs
{
    public abstract class IcedropItem : BookmarkLink
    {
        /// <summary>
        /// Internal icebergs id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Internal icebergs author id (i.e. p52577ac1069a8)
        /// </summary>
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public bool Public { get; set; }
        public long? Size { get; set; }

        //Thumbnail of item
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// If true, this item was created by icebergs system on registration (isDefault)
        /// </summary>
        public bool Initial { get; set; }

        public IEnumerable<IcedropComment> Comments { get; set; }
    }

    public class IcedropVideo : IcedropItem
    {
        public string VideoId { get; set; }
        public string VideoSource { get; set; }
    }
    public class IcedropImage : IcedropItem
    {
        public string Referrer { get; set; }
    }
    public class IcedropText : IcedropItem
    {
        public string Text { get; set; }
    }
    public class IcedropLink : IcedropItem { }

    public class IcedropComment
    {
        public string Comment { get; set; }
        public DateTime? Created { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public long Id { get; set; }
    }

}