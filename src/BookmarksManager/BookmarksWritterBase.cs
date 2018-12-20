using System.IO;
using System.Text;

namespace BookmarksManager
{
    public abstract class BookmarksWriterBase<T> where T : class, new()
    {
        protected T BookmarksContainer { get; set; }
        public Encoding OutputEncoding { get; set; }


        protected BookmarksWriterBase(T bookmarksContainer)
        {
            OutputEncoding = Encoding.UTF8;
            BookmarksContainer = bookmarksContainer;
        }

        /// <summary>
        ///     Writes bookmarks to provided TextWritter. BookmarksWriter output encoding is not used in this method, you must
        ///     create TextWriter with correct encoding
        /// </summary>
        protected abstract void Write(TextWriter outputTextWriter);

        /// <summary>
        ///     Writes bookmarks to specified output stream using OutputEncoding
        /// </summary>
        /// <param name="outputStream">Writable output stream; It will be automatically closed, you must override this method to prevent this</param>
        public virtual void Write(Stream outputStream)
        {
            using (var writer = new StreamWriter(outputStream, OutputEncoding))
            {
                Write(writer);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Write(writer);
                return sb.ToString();
            }
        }
    }
}
