using System;
using System.IO;
using System.Text;

namespace BookmarksManager
{
    public abstract class BookmarksFileWritterBase<T> : IBookmarksWritter, IDisposable  where T : class
    {
        //protected T BookmarksContainer { get; set; }
        public Encoding OutputEncoding { get; set; }
        protected TextWriter Writter { get; set; }

        protected BookmarksFileWritterBase(TextWriter writter)
        {
            OutputEncoding = Encoding.UTF8;
            this.Writter = writter;
        }

        /// <summary>
        /// Writes bookmarks to specified output
        /// </summary>
        /// <param name="bookmarksContainer">Bookmarks to write</param>
        public void Write(IBookmarkFolder bookmarksContainer)
        {
            Write(Writter, bookmarksContainer);
        }

        protected abstract void Write(TextWriter writter, IBookmarkFolder bookmarksContainer);


        public virtual void Dispose()
        {
            if (Writter != null)
                Writter.Dispose();
        }
    }
}
