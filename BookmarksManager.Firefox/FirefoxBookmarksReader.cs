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

        public string FilePath { get; private set; }


        private const string TableInfoSelectTemplate = @"PRAGMA table_info(`{0}`)";
        private const string BookmarksRootsSelectStatement = @"SELECT root_name, folder_id FROM moz_bookmarks_roots";
        private const string BookmarksSelectStatementTemplate = @"select {0}
            from moz_bookmarks b 
            left join moz_places p on b.fk = p.id
            left join moz_favicons f on f.id = p.favicon_id
            where Coalesce(p.hidden,0) != 1 and b.id > 0 and b.type > 0 and b.parent is not null
            order by parent,position";

        private const string ColumnsToSelect = "b.id,b.parent,b.type,b.position,b.title,b.dateadded,b.lastmodified,p.url,p.visit_count, f.url as favicon_url, f.data as favicon_data, f.mime_type as favicon_type";
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
        }

        /// <summary>
        /// Reads from Firefox bookmarks database 
        /// </summary>
        /// <returns>Boomarks container filled with bookmaks from Firefox db</returns>
        public virtual FirefoxBookmarkFolder Read()
        {
            using (var connection = new SQLiteConnection(string.Format(ConnectionStringTemplate, FilePath)))
            {
                connection.Open();
                FillBookmarksRoots(connection);
                var hasLastVisitedColumn = HasColumn(connection, "moz_bookmarks", "last_visit_date");
                var columnsToSelect = hasLastVisitedColumn ? ColumnsToSelect + ", p.last_visit_date" : ColumnsToSelect;

                var cmd = new SQLiteCommand(string.Format(BookmarksSelectStatementTemplate, columnsToSelect), connection);
                dynamic reader = new DynamicReader(cmd.ExecuteReader());
                var rows = new List<BookmarkRow>();
                while (reader.Read())
                {
                    rows.Add(new BookmarkRow()
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
                        FaviconUrl = reader.favicon_url,
                        FaviconData = reader.favicon_data,
                        FaviconContentType = reader.favicon_type,
                    });

                }
                return ConvertRowsToBookmarksContainer(rows);
            }
            
        }

        /// <summary>
        /// Creates bookmarks container from DB rows
        /// </summary>
        /// <returns></returns>
        protected virtual FirefoxBookmarkFolder ConvertRowsToBookmarksContainer(IList<BookmarkRow> rows)
        {
            var bookmarks = new FirefoxBookmarkFolder();
            int? bookmarksToolbarId = GetFolderIdByType("toolbar");
            //gets firefox root bookmarks folder id from moz_bookmarks_roots table. If that doesn't exist, finds most root folder
            int rootFolderId = GetFolderIdByType("places") ?? (int)rows.OrderBy(r => r.Parent).First().Id;

            var folderIndexer = new Dictionary<long, FirefoxBookmarkFolder> { { rootFolderId, bookmarks } };
            foreach (var row in rows) //folders
            {
                FirefoxBookmarkFolder parent;
                if (folderIndexer.TryGetValue(row.Parent, out parent))
                {
                    if (row.Type == 2) //folder
                    {
                        var folder = RowToFolder(row,bookmarksToolbarId);
                        bookmarks.Add(folder);
                        folderIndexer.Add(row.Id, folder);
                    }
                    else if (row.Type == 1) //link
                    {
                        parent.Add(RowToLink(row));
                    }
                }
            }
            return bookmarks;
        }

        private int? GetFolderIdByType(string type)
        {
            int folderId;
            if (_roots.TryGetValue(type,out folderId))
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
                        string rootName = reader.root_name.ToLower();
                        if (!_roots.ContainsKey(rootName))
                        {
                            _roots.Add(rootName, (int)reader.folder_id);
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

        private FirefoxBookmarkFolder RowToFolder(BookmarkRow row, int? bookmarksToolbarId)
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
                folder.IsBoomarksToolbar = true;
                folder.Attributes.Add("personal_toolbar_folder", "true");
            }

            return folder;
        }

        private FirefoxBookmarkLink RowToLink(BookmarkRow row)
        {
            return new FirefoxBookmarkLink()
            {
                Title = row.Title,
                LastModified = DateTimeHelper.FromUnixTimeStamp(row.LastModified),
                Added = DateTimeHelper.FromUnixTimeStamp(row.DateAdded),
                Url = row.Url,
                LastVisit = DateTimeHelper.FromUnixTimeStamp(row.LastVisit),
                VisitCount = (int?)row.VisitCount,
                IconContentType = row.FaviconContentType,
                IconData = row.FaviconData,
                IconUrl = row.FaviconUrl,
            };
        }



    }
}
