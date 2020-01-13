![](https://github.com/dissimilis/BookmarksManager/workflows/.NET%20Core/badge.svg)

BookmarksManager (simple .NET Standard lib for importing/exporting browser bookmarks)
=============

With this library you can:
* Write/export and read/import [Netscape bookmark file format](http://msdn.microsoft.com/en-us/library/aa753582%28v=vs.85%29.aspx) (exported from Firefox, IE, etc.)
* Read/import Firefox internal bookmarks sqlite database (places.sql)
* Read/import Chrome bookmarks from browser api (https://developer.chrome.com/extensions/bookmarks) and Chrome bookmarks file (JSON format) 

License: MIT license

NuGet packages: https://www.nuget.org/packages?q=bookmarksmanager

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
  var reader = new NetscapeBookmarksReader();
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
