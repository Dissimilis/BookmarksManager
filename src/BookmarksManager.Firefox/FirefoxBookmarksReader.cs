using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

namespace BookmarksManager.Firefox
{
    public class FirefoxBookmarksReader
    {

        /// <summary>
        /// If true, includes non user created bookmarks; Default is false
        /// </summary>
        public bool IncludeInternal { get; set; }
        public string FilePath { get; private set; }


        private const string TableInfoSelectTemplate = @"PRAGMA table_info(`{0}`)";
        private const string BookmarksRootsSelectStatement = @"SELECT title, id FROM moz_bookmarks where parent = 1 order by position";
        private const string BookmarksSelectStatementTemplate = @"select {0}
            from moz_bookmarks b 
            left join moz_places p on b.fk = p.id
            where b.id > 0 and b.type > 0 and b.parent is not null
            order by parent,position";

        private const string AttributesSelectStatement = @"select a.id, a.item_id, a.anno_attribute_id,a.content,aa.name
            from moz_items_annos a
            left join moz_anno_attributes aa on aa.id = a.anno_attribute_id";

        private const string ColumnsToSelect = "b.id,b.parent,b.type,b.position,b.title,b.dateadded,b.lastmodified,p.url,p.visit_count,p.hidden";
        private const string ConnectionStringTemplate = "Data source={0};";
        private readonly IDictionary<string, HashSet<string>> _dbColumnInfo = new Dictionary<string, HashSet<string>>();
        private readonly IDictionary<string, int> _roots = new Dictionary<string, int>();

        
        /// <summary></summary>
        /// <param name="filePath">Path to Firefox places database (places.sqlite)</param>
        public FirefoxBookmarksReader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new InvalidOperationException("File does not exist ["+filePath+"]");
            FilePath = filePath;
            IncludeInternal = false;
        }

        /// <summary>
        /// Reads from Firefox bookmarks database 
        /// </summary>
        /// <returns>Bookmarks container filled with bookmarks from Firefox db</returns>
        public virtual FirefoxBookmarkFolder Read()
        {
            using (var connection = new SQLiteConnection(string.Format(ConnectionStringTemplate, FilePath)))
            {
                connection.Open();
                FillBookmarksRoots(connection);

                var rows = GetBookmarksRows(connection);
                AssignAttributesToRows(GetBookmarksAttributes(connection), rows);

                return ConvertRowsToBookmarksContainer(rows);
            }
            
        }

        private void AssignAttributesToRows(IEnumerable<FirefoxBookmarkAttribute> attributes, ICollection<FirefoxBookmarkRow> rows)
        {
            foreach (var attr in attributes.GroupBy(a=>a.BookmarkId))
            {
                var bookmark = rows.FirstOrDefault(r => r.Id == attr.Key);
                if (bookmark != null)
                {
                    bookmark.Attributes = attr.ToList();
                }
            }
        }

        protected virtual IEnumerable<FirefoxBookmarkAttribute> GetBookmarksAttributes(SQLiteConnection connection)
        {
            var cmd = new SQLiteCommand(AttributesSelectStatement, connection);
            dynamic reader = new DynamicReader(cmd.ExecuteReader());
            var attrs = new List<FirefoxBookmarkAttribute>();
            while (reader.Read())
            {
                attrs.Add(new FirefoxBookmarkAttribute()
                {
                    BookmarkId = reader.item_id,
                    AttributeName = reader.name,
                    AttributeValue = reader.content
                });
            }
            return attrs;
        }

        protected virtual List<FirefoxBookmarkRow> GetBookmarksRows(SQLiteConnection connection)
        {
            var hasLastVisitedColumn = HasColumn(connection, "moz_bookmarks", "last_visit_date");
            var columnsToSelect = hasLastVisitedColumn ? ColumnsToSelect + ", p.last_visit_date" : ColumnsToSelect;

            var cmd = new SQLiteCommand(string.Format(BookmarksSelectStatementTemplate, columnsToSelect), connection);
            dynamic reader = new DynamicReader(cmd.ExecuteReader());
            var rows = new List<FirefoxBookmarkRow>();
            while (reader.Read())
            {
                rows.Add(new FirefoxBookmarkRow()
                {
                    Id = reader.id,
                    Parent = reader.parent,
                    Type = reader.type,
                    Position = reader.position,
                    Title = reader.title,
                    Url = reader.url,
                    LastModified = reader.lastmodified,
                    DateAdded = reader.dateadded,
                    LastVisit = reader.last_visit_date,
                    VisitCount = reader.visit_count,
                    Hidden = reader.hidden > 0,
                });
            }
            return rows;
        }

        /// <summary>
        /// Creates bookmarks container from DB rows
        /// </summary>
        /// <returns></returns>
        protected virtual FirefoxBookmarkFolder ConvertRowsToBookmarksContainer(IList<FirefoxBookmarkRow> rows)
        {
            var bookmarks = new FirefoxBookmarkFolder();
            int? bookmarksToolbarId = GetFolderIdByType("toolbar");
            //gets firefox root bookmarks folder id from moz_bookmarks_roots table. If that doesn't exist, finds most root folder
            int rootFolderId = GetFolderIdByType("places") ?? (int)rows.OrderBy(r => r.Parent).First().Id;

            var folderIndexer = new Dictionary<long, FirefoxBookmarkFolder> { { rootFolderId, bookmarks } };
            foreach (var row in rows) //folders
            {
                if (folderIndexer.TryGetValue(row.Parent, out var parent))
                {
                    //firefox treats livemarks (RSS bookmarks) as folders, we want them to be links
                    if (row.Attributes != null && row.Attributes.Any(r => r.AttributeName.Contains("livemark")))
                        row.Type = 1;
                    if (row.Type == 2) //folder
                    {
                        var folder = RowToFolder(row,bookmarksToolbarId);
                        if ((!folder.Internal || IncludeInternal) && !row.Hidden)
                            bookmarks.Add(folder);
                        folderIndexer.Add(row.Id, folder);
                    }
                    else if (row.Type == 1) //link
                    {
                        var link = RowToLink(row);
                        if ((!link.Internal || IncludeInternal) && (!row.Hidden || link.Internal))   
                            parent.Add(link);
                    }
                }
            }
            return bookmarks;
        }

        private int? GetFolderIdByType(string type)
        {
            if (_roots.TryGetValue(type,out var folderId))
            {
                return folderId;
            }
            return null;
        }


        private void FillBookmarksRoots(SQLiteConnection connection)
        {
            if (!_roots.Any())
            {
                var cmd = new SQLiteCommand(BookmarksRootsSelectStatement, connection);
                dynamic reader = new DynamicReader(cmd.ExecuteReader());
                try
                {
                    while (reader.Read())
                    {
                        string rootName = reader.title.ToLower();
                        if (!_roots.ContainsKey(rootName))
                        {
                            _roots.Add(rootName, (int)reader.id);
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
        }
        
        private bool HasColumn(SQLiteConnection connection, string tableName, string columnName)
        {
            if (!_dbColumnInfo.ContainsKey(tableName))
            {
                FillDbColumns(connection, tableName);
            }
            return _dbColumnInfo[tableName].Contains(columnName);
        }

        private void FillDbColumns(SQLiteConnection connection, string tableName)
        {
            var cmd = new SQLiteCommand(string.Format(TableInfoSelectTemplate, tableName), connection);
            var columns = new HashSet<string>();
            _dbColumnInfo[tableName] = columns;
            dynamic reader = new DynamicReader(cmd.ExecuteReader());
            try
            {
                while (reader.Read())
                {
                    columns.Add(reader.name);
                }
            }
            finally
            {
                reader.Close();
            }
        }

        private FirefoxBookmarkFolder RowToFolder(FirefoxBookmarkRow row, int? bookmarksToolbarId)
        {
            var folder = new FirefoxBookmarkFolder()
            {
                Title = row.Title,
                LastModified = DateTimeHelper.FromUnixTimeStamp(row.LastModified),
                Added = DateTimeHelper.FromUnixTimeStamp(row.DateAdded),
                Id = row.Id,
            };
            if (row.Id == bookmarksToolbarId)
            {
                folder.IsBookmarksToolbar = true;
                folder.Attributes.Add("personal_toolbar_folder", "true");
            }
            if (row.Attributes != null)
            {
                foreach (var attr in row.Attributes)
                {
                    var attrName = attr.AttributeName.ToLower();
                    switch (attrName)
                    {
                        case "bookmarkproperties/description":
                            folder.Description = attr.AttributeValue;
                            break;
                        case "places/excludefrombackup":
                            folder.ExcludeFromBackup = attr.AttributeValue == "1";
                            break;
                    }
                    if (attrName.StartsWith("places", StringComparison.CurrentCultureIgnoreCase))
                    {
                        folder.Internal = true;
                    }
                }
            }
            return folder;
        }

        private FirefoxBookmarkLink RowToLink(FirefoxBookmarkRow row)
        {
            var link = new FirefoxBookmarkLink()
            {
                Id = row.Id,
                Title = row.Title,
                LastModified = DateTimeHelper.FromUnixTimeStamp(row.LastModified),
                Added = DateTimeHelper.FromUnixTimeStamp(row.DateAdded),
                Url = row.Url??string.Empty,
                LastVisit = DateTimeHelper.FromUnixTimeStamp(row.LastVisit),
                VisitCount = (int?)row.VisitCount,
            };
            if (link.Url.StartsWith("places:", StringComparison.CurrentCultureIgnoreCase))
                link.Internal = true;
            if (row.Attributes != null)
            {
                foreach (var attr in row.Attributes)
                {
                    var attrName = attr.AttributeName.ToLower();
                    switch (attrName)
                    {
                        case "livemark/feeduri":
                            link.FeedUrl = attr.AttributeValue;
                            break;
                        case "livemark/siteuri":
                            link.Url = attr.AttributeValue;
                            break;
                        case "bookmarkproperties/description":
                            link.Description = attr.AttributeValue;
                            break;
                        case "places/excludefrombackup":
                            link.ExcludeFromBackup = attr.AttributeValue == "1";
                            break;
                    }
                    if (attrName.StartsWith("places/", StringComparison.CurrentCultureIgnoreCase))
                        link.Internal = true;
                }
            }

            return link;
        }



    }
}
