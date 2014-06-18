using System.IO;
using System.Text;

namespace BookmarksManager
{
    public abstract class BookmarksWritterBase<T> where T : class, new()
    {
        protected T BookmarksContainer { get; set; }
        public Encoding OutputEncoding { get; set; }


        protected BookmarksWritterBase(T bookmarksContainer)
        {
            OutputEncoding = Encoding.UTF8;
            BookmarksContainer = bookmarksContainer;
        }

        /// <summary>
        ///     Writes bookmarks to provided TextWritter. BookmarksWritter output encoding is not used in this method, you must
        ///     create TextWritter with correct encoding
        /// </summary>
        protected abstract void Write(TextWriter outputTextWritter);

        /// <summary>
        ///     Writes bookmarks to specified output stream using OutputEncoding
        /// </summary>
        /// <param name="outputStream">Writeable output stream</param>
        /// <param name="leaveOpen">Leave stream open after writting</param>
        public virtual void Write(Stream outputStream, bool leaveOpen = false)
        {
            using (var writer = new StreamWriter(outputStream, OutputEncoding, 4096, leaveOpen))
            {
                Write(writer);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            using (var writter = new StringWriter(sb))
            {
                Write(writter);
                return sb.ToString();
            }
        }
    }
}
