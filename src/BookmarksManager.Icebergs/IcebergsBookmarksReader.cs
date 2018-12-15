using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
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
        private Func<string, string> _htmlDecoder;

        /// <summary>
        /// Html decoder to use on text fields. You can pass only HttpUtility.HtmlDecode, o combine it with some HTML sanitizer
        /// By default no decoder is used
        /// </summary>
        public Func<string, string> HtmlDecoder {
            get { return _htmlDecoder ?? (_htmlDecoder = (html) => html); }
            set { _htmlDecoder = value; }
        }

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
                                item = new IcedropLink(){ThumbnailUrl = icedrop.url};
                                break;
                            case "bookmarkletimage":
                                item = new IcedropImage()
                                {
                                    Referrer = icedrop.origin,
                                    Url = icedrop.url,
                                    ThumbnailUrl = icedrop.url
                                };
                                break;
                            case "bookmarkletvideo":
                                item = new IcedropVideo()
                                {
                                    VideoId = icedrop.videoId,
                                    VideoSource = icedrop.videoSource,
                                    ThumbnailUrl = icedrop.url,
                                };
                                break;
                            case "bookmarklettext":
                                item = new IcedropText()
                                {
                                    Text = HtmlDecoder(icedrop.textContent ?? string.Empty),
                                };
                                break;
                            case "userfile":
                                item = new IcedropUserFile()
                                {
                                    Url = icedrop.url
                                };
                                break;
                            case "note":
                                item = new IcedropNote()
                                {
                                    Text = HtmlDecoder(icedrop.textContent ?? string.Empty),
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
            item.Title = HtmlDecoder(model.title ?? string.Empty);
            item.Size = model.size;
            item.Initial = model.isDefault ?? false;
            item.Public = model.@public ?? false;
            if (item.Url == null)
                item.Url = model.origin;
            //item.IconUrl = model.url;
            if (item.AuthorName != null)
                item.AuthorName = HtmlDecoder(model.authorName);
            item.AuthorId = model.authorId;
            item.Id = model.id;
            if (model.comments != null && model.comments.Any())
            {
                item.Comments = model.comments.Select(comment => new IcedropComment()
                {
                    AuthorId = comment.authorId, AuthorName = comment.authorFullName, Comment = HtmlDecoder(comment.comment??string.Empty), Created = DateTimeHelper.FromUnixTimeStamp(comment.time),
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
