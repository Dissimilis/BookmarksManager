using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BookmarksManager.Icebergs
{

    /// <summary>
    /// Class reponsible for reading JSON exported from icebergs.com
    /// </summary>
    /// <returns>BookmarkFolder with icebergs and icedrops. Empty folder if file is unknown</returns>
    public class IcebergsBookmarksReader : BookmarksReaderBase<BookmarkFolder>
    {
        public override BookmarkFolder Read(string inputString)
        {
            if (inputString == null)
                throw new ArgumentNullException("inputString");

            if (IsBookmarksFile(inputString)) //icebergs exported JSON file
            {
                var json = JArray.Parse(inputString);
                var icebergs = json.ToObject<IEnumerable<IcebergModel>>();
                return TransformModel(icebergs);
            }
            else return new BookmarkFolder();
        }

        protected virtual BookmarkFolder TransformModel(IEnumerable<IcebergModel> model)
        {
            var root = new BookmarkFolder();
            foreach (var iceberg in model)
            {
                var folder = new BookmarkFolder(iceberg.name);
                if (iceberg.icebergImage != null)
                    folder.Attributes.Add("icebergImage", iceberg.icebergImage);

                if (iceberg.icedrops != null)
                {
                    foreach (var icedrop in iceberg.icedrops)
                    {
                        if (icedrop.type == null)
                            continue;
                        IcedropItem item;
                        switch (icedrop.type.ToLower())
                        {
                            case "bookmarkletthumbnail":
                                item = new IcedropLink();
                                break;
                            case "bookmarkletimage":
                                item = new IcedropImage()
                                {
                                    Referrer = icedrop.origin,
                                    Url = icedrop.url
                                };
                                break;
                            case "bookmarkletvideo":
                                item = new IcedropVideo()
                                {
                                    VideoId = icedrop.videoId,
                                    VideoSource = icedrop.videoSource
                                };
                                break;
                            case "bookmarklettext":
                                item = new IcedropText()
                                {
                                    Text = icedrop.textContent,
                                    Description = icedrop.textContent
                                };
                                break;
                            default: //skip unknown item
                                continue;
                        }
                        AddCommonProperties(item, icedrop);
                        folder.Add(item);
                    }
                }
                root.Add(folder);
            }
            return root;
        }

        private void AddCommonProperties(IcedropItem item, IcedropModel model)
        {
            item.Added = DateTimeHelper.FromUnixTimeStamp(model.creationTime);
            item.Title = model.title;
            item.Size = model.size;
            item.Initial = model.isDefault ?? false;
            item.Public = model.@public ?? false;
            if (item.Url == null)
                item.Url = model.origin;
            //item.IconUrl = model.url;
            item.AuthorName = model.authorName;
            item.AuthorId = model.authorId;
            item.Id = model.id;
            if (item.ThumbnailUrl == null)
                item.ThumbnailUrl = model.url;

            if (model.comments != null && model.comments.Any())
            {
                item.Comments = model.comments.Select(comment => new IcedropComment()
                {
                    AuthorId = comment.authorId, AuthorName = comment.authorFullName, Comment = comment.comment, Created = DateTimeHelper.FromUnixTimeStamp(comment.time),
                });
            }

            if (model.belongsToGroup != null)
                item.Attributes.Add("belongsToGroup", model.belongsToGroup);

        }

        /// <summary>
        /// Returns true if JSON content is icebergs file
        /// </summary>
        protected virtual bool IsBookmarksFile(string json)
        {
            return Regex.IsMatch(json, @"\W+?""icebergImage""", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

    }
}
