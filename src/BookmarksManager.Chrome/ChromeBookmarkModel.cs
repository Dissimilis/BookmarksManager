using System.Collections.Generic;

namespace BookmarksManager.Chrome
{
    public class ChromeBookmarkModel
    {
        //common model
        public string id { get; set; }
        public string url { get; set; }
        
        //API model
        public string parentid { get; set; }
        public int? index { get; set; }
        public string title { get; set; }
        public long? dateadded { get; set; }
        public long? dateGroupModified { get; set; }

        //Bookmarks file model
        public string type { get; set; }
        public string name { get; set; }
        public long? date_added { get; set; }
        public long? date_modified { get; set; }

        public IEnumerable<ChromeBookmarkModel> children { get; set; }
    }
}