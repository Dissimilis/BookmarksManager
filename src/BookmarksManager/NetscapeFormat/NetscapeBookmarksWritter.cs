using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace BookmarksManager
{
    /// <summary>
    ///     This class is used for bookmarks container serialization to Netscape bookmarks format
    ///     Netscape bookmarks format is de facto standard for importing/exporting bookmarks from browsers
    ///     Format is described here: http://msdn.microsoft.com/en-us/library/aa753582%28v=vs.85%29.aspx
    /// </summary>
    public class NetscapeBookmarksWriter : BookmarksWriterBase<BookmarkFolder>
    {
        protected string NetscapeBookmarksFileHead = @"<!DOCTYPE NETSCAPE-Bookmark-file-1>
    <!--This is an automatically generated file.
    It will be read and overwritten.
    Do Not Edit! -->
    <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset={0}"">
    <Title>Bookmarks</Title>
    <H1>Bookmarks</H1>";

        public const string Indentation = "    ";
        public static readonly string[] IgnoredAttributes = {"last_modified", "icon", "icon_uri", "href", "last_visit", "add_date", "feedurl"};


        public NetscapeBookmarksWriter(BookmarkFolder bookmarksContainer)
            : base(bookmarksContainer)
        {
        }

        protected override void Write(TextWriter outputTextWriter)
        {
            if (outputTextWriter == null)
                throw new ArgumentNullException(nameof(outputTextWriter));
            outputTextWriter.Write(NetscapeBookmarksFileHead, OutputEncoding.WebName);
            outputTextWriter.WriteLine();
            using (var writer = XmlWriter.Create(outputTextWriter, new XmlWriterSettings {ConformanceLevel = ConformanceLevel.Fragment, Indent = false, Encoding = OutputEncoding}))
            {
                WriteFolderItems(BookmarksContainer, outputTextWriter, writer, 0);
            }
        }

        protected virtual void WriteFolderItems(IEnumerable<IBookmarkItem> folder, TextWriter writer, XmlWriter xmlWriter, int iteration)
        {
            WriteIndentation(iteration, writer);
            writer.WriteLine("<DL><p>");
            foreach (var item in folder)
            {
                BookmarkFolder innerFolder;
                BookmarkLink innerLink;
                if ((innerFolder = item as BookmarkFolder) != null)
                {
                    WriteIndentation(iteration, writer);
                    WriteFolderLine(innerFolder, writer, xmlWriter);
                    WriteFolderItems(innerFolder, writer, xmlWriter, iteration + 1);
                }
                else if ((innerLink = item as BookmarkLink) != null)
                {
                    WriteIndentation(iteration, writer);
                    WriteLinkLine(innerLink, writer, xmlWriter);
                }
            }
            WriteIndentation(iteration, writer);
            writer.WriteLine("</DL><p>");
        }

        protected virtual void WriteLinkLine(BookmarkLink link, TextWriter writer, XmlWriter xmlWriter)
        {
            writer.Write("<DT>");
            xmlWriter.WriteStartElement("A");
            if (link.LastModified.HasValue)
                xmlWriter.WriteAttributeString("LAST_MODIFIED", link.LastModified.Value.ToUnixTimestamp().ToString());
            if (link.LastVisit.HasValue)
                xmlWriter.WriteAttributeString("LAST_VISIT", link.LastVisit.Value.ToUnixTimestamp().ToString());
            if (link.Added.HasValue)
                xmlWriter.WriteAttributeString("ADD_DATE", link.Added.Value.ToUnixTimestamp().ToString());
            if (!string.IsNullOrEmpty(link.IconUrl))
                xmlWriter.WriteAttributeString("ICON_URI", link.IconUrl);
            if (!string.IsNullOrEmpty(link.IconContentType) && link.IconData != null)
                WriteEmbeddedIcon(link, xmlWriter);
            if (!string.IsNullOrEmpty(link.FeedUrl))
            {
                xmlWriter.WriteAttributeString("FEED", "true");
                xmlWriter.WriteAttributeString("FEEDURL", link.FeedUrl);
            }
            xmlWriter.WriteAttributeString("HREF", link.Url);
            if (link.Attributes != null && link.Attributes.Any())
                WriteCustomAttributes(link.Attributes, xmlWriter);

            xmlWriter.WriteString(link.Title);
            xmlWriter.WriteEndElement();
            xmlWriter.Flush();
            if (!string.IsNullOrEmpty(link.Description))
            {
                writer.WriteLine();
                writer.Write("<DD>");
                xmlWriter.WriteString(link.Description);
                xmlWriter.Flush();
            }
            writer.WriteLine();
        }

        protected virtual void WriteEmbeddedIcon(BookmarkLink link, XmlWriter xmlWriter)
        {
            const string template = "data:{0};base64,{1}";
            var base64Content = Convert.ToBase64String(link.IconData);
            xmlWriter.WriteAttributeString("ICON", string.Format(template, link.IconContentType, base64Content));
        }

        protected virtual void WriteFolderLine(BookmarkFolder folder, TextWriter writer, XmlWriter xmlWriter)
        {
            writer.Write("<DT>");
            xmlWriter.WriteStartElement("H3");
            if (folder.LastModified.HasValue)
                xmlWriter.WriteAttributeString("LAST_MODIFIED", folder.LastModified.Value.ToUnixTimestamp().ToString());
            if (folder.Added.HasValue)
                xmlWriter.WriteAttributeString("ADD_DATE", folder.Added.Value.ToUnixTimestamp().ToString());
            if (folder.Attributes != null && folder.Attributes.Any())
                WriteCustomAttributes(folder.Attributes, xmlWriter);
            xmlWriter.WriteString(folder.Title);
            xmlWriter.WriteEndElement();
            xmlWriter.Flush();
            writer.WriteLine();
        }

        protected virtual void WriteCustomAttributes(IDictionary<string, string> attributes, XmlWriter xmlWriter)
        {
            foreach (var attr in attributes.Keys.Except(IgnoredAttributes, StringComparer.OrdinalIgnoreCase))
            {
                xmlWriter.WriteAttributeString(attr.ToUpper(), attributes[attr]);
            }
        }

        protected virtual void WriteIndentation(int iteration, TextWriter writer)
        {
            for (var i = 0; i < iteration; i++)
                writer.Write(Indentation);
        }
    }
}