using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BookmarksManager.Chrome
{

    /// <summary>
    /// Class reponsible for reading Chrome bookmarks
    /// It accepts JSON from Chrome API (as per https://developer.chrome.com/extensions/bookmarks#type-BookmarkTreeNode)
    /// As well as JSON from Chrome bookmarks file (%AppData%\Local\Google\Chrome\User Data\Default\bookmarks)
    /// </summary>
    public class ChromeBookmarksReader : BookmarksReaderBase<BookmarkFolder>
    {
        public override BookmarkFolder Read(string inputString)
        {
            if (inputString == null)
                throw new ArgumentNullException("inputString");

            if (IsBookmarksFile(inputString)) //chrome bookmarks file
            {
                var json = JObject.Parse(inputString);
                
                var rootFolders = json["roots"].Select(root => root.First());
                var transformed = rootFolders.Where(rf => rf.Type == JTokenType.Object).Select(rf => rf.ToObject<ChromeBookmarkModel>());
                return TransformModel(transformed);

            }
            else //chrome bookmarks from browser API
            {
                var bookmarks = JsonConvert.DeserializeObject<IEnumerable<ChromeBookmarkModel>>(inputString);
                return TransformModel(bookmarks);
            }
        }

        protected virtual BookmarkFolder TransformModel(IEnumerable<ChromeBookmarkModel> model)
        {
            var root = new BookmarkFolder();
            foreach (var m in model)
            {
                root.Add(ReadItems(m));
            }
            return root;
        }

        /// <summary>
        /// Returns true if JSON content is from chrome bookmakrs file
        /// </summary>
        protected virtual bool IsBookmarksFile(string json)
        {
            return Regex.IsMatch(json, @"\W+?""roots""", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        private IBookmarkItem ReadItems(ChromeBookmarkModel model)
        {
            if (!string.IsNullOrEmpty(model.url) && model.children == null)
            {
                var item = new BookmarkLink(model.url, model.title??model.name);
                AddAttributes(item.Attributes, model);
                item.Added = parseTimeStamp(model.dateadded ?? model.date_added);
                item.LastModified = parseTimeStamp(model.dateGroupModified ?? model.date_modified);
                return item;
            }
            else
            {
                var folder = new BookmarkFolder(model.title ?? model.name);
                AddAttributes(folder.Attributes, model);
                folder.Added = parseTimeStamp(model.dateadded??model.date_added);
                folder.LastModified = parseTimeStamp(model.dateGroupModified??model.date_modified);
                if (model.children != null)
                {
                    foreach (var inner in model.children.OrderBy(x => x.index ?? 0))
                    {
                        folder.Add(ReadItems(inner));
                    }
                }
                return folder;
            }
        }

        private void AddAttributes(IDictionary<string, string> attributes, ChromeBookmarkModel model)
        {
            if (model.type != null)
                attributes.Add("type", model.type);
            if (model.id != null)
                attributes.Add("id", model.id);
            if (model.parentid != null)
                attributes.Add("parentid", model.parentid);
            var title = model.title ?? model.name;
            if (title != null && string.Equals("bookmarks bar", title, StringComparison.OrdinalIgnoreCase) && model.url == null)
                attributes.Add("personal_toolbar_folder", "true");
        }

        private DateTime? parseTimeStamp(long? timeStamp)
        {
            //http://fileformats.archiveteam.org/wiki/Chrome_bookmarks
            if (timeStamp == null)
                return null;
            return DateTime.FromFileTime(timeStamp.Value*10);
        }

    }
}
