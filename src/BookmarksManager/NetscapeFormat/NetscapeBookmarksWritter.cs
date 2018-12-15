using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace BookmarksManager
{
    /// <summary>
    ///     This class is used for bookmarks container serialization to Netscape bookmarks fomat
    ///     Netscape bookmarks format is defacto standard for importing/exporting bookmarks from browsers
    ///     Format is described here: http://msdn.microsoft.com/en-us/library/aa753582%28v=vs.85%29.aspx
    /// </summary>
    public class NetscapeBookmarksWritter : BookmarksWritterBase<BookmarkFolder>
    {
        protected string NetscapeBookmarksFileHead = @"<!DOCTYPE NETSCAPE-Bookmark-file-1>
    <!--This is an automatically generated file.
    It will be read and overwritten.
    Do Not Edit! -->
    <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset={0}"">
    <Title>Bookmarks</Title>
    <H1>Bookmarks</H1>";

        public const string Identation = "    ";
        public static readonly string[] IgnoredAttributes = {"last_modified", "icon", "icon_uri", "href", "last_visit", "add_date", "feedurl"};


        public NetscapeBookmarksWritter(BookmarkFolder bookmarksContainer)
            : base(bookmarksContainer)
        {
        }

        protected override void Write(TextWriter outputTextWritter)
        {
            if (outputTextWritter == null)
                throw new ArgumentNullException("outputTextWritter");
            outputTextWritter.Write(NetscapeBookmarksFileHead, OutputEncoding.WebName);
            outputTextWritter.WriteLine();
            using (var writter = XmlWriter.Create(outputTextWritter, new XmlWriterSettings {ConformanceLevel = ConformanceLevel.Fragment, Indent = false, Encoding = OutputEncoding}))
            {
                WriteFolderItems(BookmarksContainer, outputTextWritter, writter, 0);
            }
        }

        protected virtual void WriteFolderItems(IEnumerable<IBookmarkItem> folder, TextWriter writter, XmlWriter xmlWritter, int iteration)
        {
            WriteIdentation(iteration, writter);
            writter.WriteLine("<DL><p>");
            foreach (var item in folder)
            {
                BookmarkFolder innerFolder;
                BookmarkLink innerLink;
                if ((innerFolder = item as BookmarkFolder) != null)
                {
                    WriteIdentation(iteration, writter);
                    WriteFolderLine(innerFolder, writter, xmlWritter);
                    WriteFolderItems(innerFolder, writter, xmlWritter, iteration + 1);
                }
                else if ((innerLink = item as BookmarkLink) != null)
                {
                    WriteIdentation(iteration, writter);
                    WriteLinkLine(innerLink, writter, xmlWritter);
                }
            }
            WriteIdentation(iteration, writter);
            writter.WriteLine("</DL><p>");
        }

        protected virtual void WriteLinkLine(BookmarkLink link, TextWriter writter, XmlWriter xmlWritter)
        {
            writter.Write("<DT>");
            xmlWritter.WriteStartElement("A");
            if (link.LastModified.HasValue)
                xmlWritter.WriteAttributeString("LAST_MODIFIED", link.LastModified.Value.ToUnixTimestamp().ToString());
            if (link.LastVisit.HasValue)
                xmlWritter.WriteAttributeString("LAST_VISIT", link.LastVisit.Value.ToUnixTimestamp().ToString());
            if (link.Added.HasValue)
                xmlWritter.WriteAttributeString("ADD_DATE", link.Added.Value.ToUnixTimestamp().ToString());
            if (!string.IsNullOrEmpty(link.IconUrl))
                xmlWritter.WriteAttributeString("ICON_URI", link.IconUrl);
            if (!string.IsNullOrEmpty(link.IconContentType) && link.IconData != null)
                WriteEmbededIcon(link, xmlWritter);
            if (!string.IsNullOrEmpty(link.FeedUrl))
            {
                xmlWritter.WriteAttributeString("FEED", "true");
                xmlWritter.WriteAttributeString("FEEDURL", link.FeedUrl);
            }
            xmlWritter.WriteAttributeString("HREF", link.Url);
            if (link.Attributes != null && link.Attributes.Any())
                WriteCustomAttributes(link.Attributes, xmlWritter);

            xmlWritter.WriteString(link.Title);
            xmlWritter.WriteEndElement();
            xmlWritter.Flush();
            if (!string.IsNullOrEmpty(link.Description))
            {
                writter.WriteLine();
                writter.Write("<DD>");
                xmlWritter.WriteString(link.Description);
                xmlWritter.Flush();
            }
            writter.WriteLine();
        }

        protected virtual void WriteEmbededIcon(BookmarkLink link, XmlWriter xmlWritter)
        {
            const string template = "data:{0};base64,{1}";
            var base64Content = Convert.ToBase64String(link.IconData);
            xmlWritter.WriteAttributeString("ICON", string.Format(template, link.IconContentType, base64Content));
        }

        protected virtual void WriteFolderLine(BookmarkFolder folder, TextWriter writter, XmlWriter xmlWritter)
        {
            writter.Write("<DT>");
            xmlWritter.WriteStartElement("H3");
            if (folder.LastModified.HasValue)
                xmlWritter.WriteAttributeString("LAST_MODIFIED", folder.LastModified.Value.ToUnixTimestamp().ToString());
            if (folder.Added.HasValue)
                xmlWritter.WriteAttributeString("ADD_DATE", folder.Added.Value.ToUnixTimestamp().ToString());
            if (folder.Attributes != null && folder.Attributes.Any())
                WriteCustomAttributes(folder.Attributes, xmlWritter);
            xmlWritter.WriteString(folder.Title);
            xmlWritter.WriteEndElement();
            xmlWritter.Flush();
            writter.WriteLine();
        }

        protected virtual void WriteCustomAttributes(IDictionary<string, string> attributes, XmlWriter xmlWritter)
        {
            foreach (var attr in attributes.Keys.Except(IgnoredAttributes, StringComparer.OrdinalIgnoreCase))
            {
                xmlWritter.WriteAttributeString(attr.ToUpper(), attributes[attr]);
            }
        }

        protected virtual void WriteIdentation(int iteration, TextWriter writter)
        {
            for (var i = 0; i < iteration; i++)
                writter.Write(Identation);
        }
    }
}