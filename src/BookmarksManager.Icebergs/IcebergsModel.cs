using System.Collections.Generic;

namespace BookmarksManager.Icebergs
{
    public class IcedropModel
    {
                //"origin" : "http://www.bbc.co.uk/",
                //"id" : "1398243385283141",
                //"authorId" : "p33577eb1063a8",
                //"authorName" : "none",
                //"title" : "BBC \u017dinios - World news",
                //"creationTime" : "1398243185",
                //"size" : 19914,
                //"type" : "bookmarkletImage",
                //"url" : "http://icebergs.com/icedrop/download/?expiresAt=1564471590&ownerId=p53577eb1069a8&icebergId=53577eff6c119&icedropId=1398245685283141&userId=p89777eb1069a8&hash=512f9757201d8b16a84a09befa4e2056048f98f7",
                //"public" : false,
                //"comments" : [],
                //"commentsNumber" : 0
        public string origin { get; set; }
        public long id { get; set; }
        public string authorName { get; set; }
        public string authorId { get; set; }
        public string title { get; set; }
        public long? creationTime { get; set; }
        public long? size { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public bool? @public { get; set; }
        public bool? isDefault { get; set; }

        public string videoId { get; set; }
        public string videoSource { get; set; }

        public string textContent { get; set; }

        public string belongsToGroup { get; set; }
        public string highlighted { get; set; }

        public IEnumerable<IcedropCommentModel> comments { get; set; }
        public int? commentsNumber { get; set; }
    }

    public class IcedropCommentModel
    {
        public long id { get; set; }
        public long? time { get; set; }
        public string authorId { get; set; }
        public string authorFullName { get; set; }
        public string comment { get; set; }
    }

    public class IcebergModel
    {
        public long? icedropsNumber { get; set; }
        public string name { get; set; }
        public long? size { get; set; }
        public string icebergImage { get; set; }
        public IEnumerable<IcedropModel> icedrops { get; set; }
    }
}