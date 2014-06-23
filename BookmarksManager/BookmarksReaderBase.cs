using System.IO;
using System.Text;

namespace BookmarksManager
{
    public abstract class BookmarksFileReaderBase<T> : IBookmarksReader where T : class
    {
        public Encoding InputEncoding { get; set; }

        protected BookmarksFileReaderBase()
        {
            InputEncoding = Encoding.UTF8;
        }
        
        /// <summary>
        ///     Reads bookmarks from stream using provided encoding
        /// </summary>
        /// <param name="inputStream">Input stream containing bookmarks</param>
        /// <returns>Bookmarks container</returns>
        public virtual T Read(Stream inputStream)
        {
            using (var reader = new StreamReader(inputStream, InputEncoding))
            {
                return Read(reader.ReadToEnd());
            }
        }

        /// <summary>
        ///     Creates bookmarks container from string
        /// </summary>
        /// <returns>Bookmarks container</returns>
        public abstract T Read(string inputString);

        public IBookmarkFolder Read()
        {
            throw new System.NotImplementedException();
        }
    }

    public interface IBookmarksReader
    {
        IBookmarkFolder Read();
    }
    public interface IBookmarksWritter
    {
        void Write(IBookmarkFolder bookmarksContainer);
    }
}