using System.Linq;
using System.Text;

namespace BookmarksManager
{
    internal static class TextHelpers
    {
        /// <summary>
        ///     Poor man's solution of detecting text encoding
        /// </summary>
        /// <param name="b"></param>
        /// <returns>Encoding detected from BOM, UTF8 if no BOM is present</returns>
        public static Encoding GetEncoding(this byte[] b)
        {
            if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF) // UTF-32, big-endian 
            {
                return Encoding.GetEncoding("utf-32BE");
            }
            if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00) // UTF-32, little-endian
            {
                return Encoding.GetEncoding("utf-32");
            }
            if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) // UTF-16, big-endian
            {
                return Encoding.BigEndianUnicode;
            }
            if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) // UTF-16, little-endian
            {
                return Encoding.Unicode;
            }
            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) // UTF-8
            {
                return Encoding.UTF8;
            }
            if (b.Length >= 3 && b[0] == 0x2b && b[1] == 0x2f && b[2] == 0x76) // UTF-7
            {
                return Encoding.GetEncoding("utf-7");
            }

            //if number of zero bytes exeeds threshold, asume it's utf32 or utf16 content
            //this does not check for BigEndian
            var zeroBytesCnt = b.Count(x => x == 0);
            if (zeroBytesCnt > b.Length*0.5)
                return Encoding.GetEncoding("utf-32");
            if (zeroBytesCnt > b.Length*0.2)
                return Encoding.Unicode;

            return Encoding.UTF8;
        }
    }
}