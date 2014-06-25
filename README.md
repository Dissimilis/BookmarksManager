BookmarksManager
=============

Create and read Netscape bookmark file format (Firefox, IE, etc.)

Read Firefox internal bookmarks database (places.sql)

License: MIT license

**Usage examples:**
```csharp
//Read bookmarks from string
var reader = new NetscapeBookmarksReader();
var bookmarks = reader.Read(bookmarksString);
foreach (var b in bookmarks.AllLinks)
{
  Console.WriteLine("Url: {0}; Title: {1}", b.Url, b.Title);
}

//Read bookmarks from file
using (var file = File.OpenRead("path_to_file"))
{
  var reader = new NetscapeBookmarksReader;
  //supports encoding detection when reading from stream
  var bookmarks = reader.Read(file);
  foreach (var b in bookmarks.AllLinks.Where(l=>l.LastVisit < DateTime.Today))
  {
    Console.WriteLine("Type {0}, Title: {1}", b.GetType().Name, b.Title);
  }
}


//Write bookmarks
var bookmarks = new BookmarkFolder()
{
    new BookmarkLink("http://example.com", "Example")
};
var writter = new NetscapeBookmarksWritter(bookmarks);

Console.WriteLine(writter.ToString());

//supports writting to stream with custom encoding
writter.OutputEncoding = Encoding.GetEncoding(1257);
using (var file = File.OpenWrite("path_to_file"))
{
    writter.Write(file);
}
```
