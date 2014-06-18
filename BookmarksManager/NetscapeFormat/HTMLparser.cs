/*
  
This file incorporates work covered by the following copyright and permission notice:  

Copyright (c) Alex Chudnovsky, Majestic-12 Ltd (UK). 2005+ All rights reserved
Web:		http://www.majestic12.co.uk
E-mail:		alexc@majestic12.co.uk

Redistribution and use in source and binary forms, with or without modification, are 
permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions 
		and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
		and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of the Majestic-12 nor the names of its contributors may be used to endorse or 
		promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System.Collections.Generic;
using System.Globalization;
using System.Text;


// Description:	HTMLparser -- low level class that splits HTML into tokens 
//					such as tag, comments, text etc.
//					
//					Change source with great care - a lot of effort went into optimising it to make sure it performs
//					very well, for this reason some range checks were removed in cases when big enough buffers
//					were sufficient for 99.9999% of HTML pages out there.
//					
//					This does not mean you won't get an exception, so when you are parsing, then make sure you
//					catch exceptions from parser.
//					
// Author:			Alex Chudnovsky <alexc@majestic12.co.uk>
// 	
// History:		
//					4/02/06 v1.0.1  Added fix to raw HTML not being stored, thanks
//									for this go to Christopher Woodill <chriswoodill@yahoo.com>
//					1/10/05 v1.0.0	Public release
//					  ...			Many changes here
//					4/08/04 v0.5.0	New
//							 
// 
// 
namespace Majestic12
{
    /// <summary>
    ///     Type of parsed HTML chunk (token)
    /// </summary>
    internal enum HTMLchunkType
    {
        /// <summary>
        ///     Text data from HTML
        /// </summary>
        Text = 0,

        /// <summary>
        ///     Open tag
        /// </summary>
        OpenTag = 1,

        /// <summary>
        ///     Closed data
        /// </summary>
        CloseTag = 2,

        /// <summary>
        ///     Data between HTML comments, ie: <!-- -->
        /// </summary>
        Comment = 3,
    };

    /// <summary>
    ///     Class for fast dynamic string building - it is faster than StringBuilder
    /// </summary>
    internal class DynaString
    {
        /// <summary>
        ///     CRITICAL: that much capacity will be allocated (once) for this object -- for performance reasons
        ///     we do NOT have range checks because we make reasonably safe assumption that accumulated string will
        ///     fit into the buffer. If you have very abnormal strings then you should increase buffer accordingly.
        /// </summary>
        public static int TEXT_CAPACITY = 1024*128 - 1;

        public byte[] bBuffer;
        public int iBufPos;

        /// <summary>
        ///     Finalised text will be available in this string
        /// </summary>
        public string sText;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="sString">Initial string</param>
        public DynaString(string sString)
        {
            sText = sString;
            iBufPos = 0;
            bBuffer = new byte[TEXT_CAPACITY + 1];
        }

        /// <summary>
        ///     Converts data in buffer to string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            if (iBufPos > 0)
                Finalise();

            return sText;
        }

        /// <summary>
        ///     Lower cases data in buffer and returns as string.
        ///     WARNING: This is only to be used for ASCII lowercasing - HTML params and tags, not their values
        /// </summary>
        /// <returns>String that is now accessible via sText</returns>
        public string ToLowerString()
        {
            if (sText.Length == 0)
            {
                ToString();
                sText = sText.ToLower();
            }
            else
            {
                ToString();
                sText = sText.ToLower();
            }

            return sText;
        }

        /// <summary>
        ///     Resets object to zero length string
        /// </summary>
        public void Clear()
        {
            sText = "";
            iBufPos = 0;
        }

        /// <summary>
        ///     Appends a "char" to the buffer
        /// </summary>
        /// <param name="cChar">Appends char (byte really)</param>
        public void Append(byte cChar)
        {
            // Length++;

            if (iBufPos >= TEXT_CAPACITY)
            {
                if (sText.Length == 0)
                {
                    sText = Encoding.UTF8.GetString(bBuffer, 0, iBufPos);
                }
                else
                    //sText+=new string(bBuffer,0,iBufPos);
                    sText += Encoding.UTF8.GetString(bBuffer, 0, iBufPos);
                iBufPos = 0;
            }

            bBuffer[iBufPos++] = cChar;
        }

        /// <summary>
        ///     Internal finaliser - creates string from data accumulated in buffer
        /// </summary>
        private void Finalise()
        {
            if (iBufPos > 0)
            {
                if (sText.Length == 0)
                {
                    sText = Encoding.UTF8.GetString(bBuffer, 0, iBufPos);
                }
                else
                    //sText+=new string(bBuffer,0,iBufPos);
                    sText += Encoding.UTF8.GetString(bBuffer, 0, iBufPos);
                iBufPos = 0;
            }
        }
    }

    /// <summary>
    ///     This class will contain an HTML chunk -- parsed token that is either text, comment, open or closed tag.
    /// </summary>
    internal class HTMLchunk

    {
        /// <summary>
        ///     Maximum default capacity of buffer that will keep data
        /// </summary>
        public static int TEXT_CAPACITY = 1024*128;

        /// <summary>
        ///     Maximum number of parameters in a tag - should be high enough to fit most
        /// </summary>
        public static int MAX_PARAMS = 256;

        /// <summary>
        ///     For TAGS: it stores raw HTML that was parsed to generate thus chunk will be here UNLESS
        ///     HTMLparser was configured not to store it there as it can improve performance
        ///     For TEXT or COMMENTS: actual text or comments
        /// </summary>
        public string HTML = "";

        /// <summary>
        ///     If its open/close tag type then this is where lowercased Tag will be kept
        /// </summary>
        public string Tag = "";

        /// <summary>
        ///     Chunk type showing whether its text, open or close tag, or comments.
        /// </summary>
        public HTMLchunkType Type;

        public byte[] bBuffer = new byte[TEXT_CAPACITY + 1];

        public bool bClosure = false;
        public bool bComments = false;

        /// <summary>
        ///     True if entities were present (and transformed) in the original HTML
        /// </summary>
        public bool bEntities = false;

        /// <summary>
        ///     If true then tag params will be kept in a hash rather than in a fixed size arrays. This will be slow
        ///     down parsing, but make it easier to use
        /// </summary>
        public bool bHashMode = true;

        /// <summary>
        ///     Set to true if &lt; entity was found
        /// </summary>
        public bool bLtEntity = false;

        public int iBufPos = 0;

        public int iHTMLen = 0;

        /// <summary>
        ///     Number of parameters and values stored in sParams array.
        ///     ONLY used if bHashMode is set to FALSE.
        /// </summary>
        public int iParams = 0;

        /// <summary>
        ///     Hashtable with tag parameters: keys are param names and values are param values.
        ///     ONLY used if bHashMode is set to TRUE.
        /// </summary>
        public List<KeyValuePair<string, string>> oParams = null;

        /// <summary>
        ///     Param names will be stored here - actual number is in iParams.
        ///     ONLY used if bHashMode is set to FALSE.
        /// </summary>
        public string[] sParams = new string[MAX_PARAMS];

        /// <summary>
        ///     Param values will be stored here - actual number is in iParams.
        ///     ONLY used if bHashMode is set to FALSE.
        /// </summary>
        public string[] sValues = new string[MAX_PARAMS];

        /// <summary>
        ///     Initialises new HTMLchunk
        /// </summary>
        /// <param name="p_bHashMode">Sets <seealso cref="bHashMode" /></param>
        public HTMLchunk(bool p_bHashMode)
        {
            bHashMode = p_bHashMode;

            if (bHashMode)
                oParams = new List<KeyValuePair<string, string>>();
        }

        public bool IsOpenTag
        {
            get { return Type == HTMLchunkType.OpenTag; }
        }

        public bool IsCloseTag
        {
            get { return Type == HTMLchunkType.CloseTag; }
        }

        public bool IsText
        {
            get { return Type == HTMLchunkType.Text; }
        }

        internal int ContentPosition { get; set; }

        /// <summary>
        ///     This function will convert parameters stored in sParams/sValues arrays into oParams hash
        ///     Useful if generally parsing is done when bHashMode is FALSE. Hash operations are not the fastest, so
        ///     its best not to use this function.
        /// </summary>
        public void ConvertParamsToHash()
        {
            if (oParams != null)
                oParams.Clear();
            else
                oParams = new List<KeyValuePair<string, string>>();

            for (var i = 0; i < iParams; i++)
            {
                oParams.Add(new KeyValuePair<string, string>(sParams[i], sValues[i]));
            }
        }

        /// <summary>
        ///     Adds tag parameter to the chunk
        /// </summary>
        /// <param name="sParam">Parameter name (ie color)</param>
        /// <param name="sValue">Value of the parameter (ie white)</param>
        public void AddParam(string sParam, string sValue)
        {
            if (!bHashMode)
            {
                if (iParams < MAX_PARAMS)
                {
                    sParams[iParams] = sParam;
                    sValues[iParams] = sValue;

                    iParams++;
                }
            }
            else
            {
                oParams.Add(new KeyValuePair<string, string>(sParam, sValue));
            }
        }

        ~HTMLchunk()
        {
            Close();
        }

        /// <summary>
        ///     Closes chunk trying to reclaims memory used by buffers
        /// </summary>
        public void Close()
        {
            if (oParams != null)
                oParams = null;

            bBuffer = null;
        }

        /// <summary>
        ///     Clears chunk preparing it for
        /// </summary>
        public void Clear()
        {
            iHTMLen = iBufPos = 0;
            //oHTML=null;
            Tag = HTML = "";
            //sTag=null;
            //sTag="";
            bLtEntity = bEntities = bComments = bClosure = false;

            /*
			bComments=false;
			bEntities=false;
			bLtEntity=false;
			*/

            if (!bHashMode)
            {
                /*
				for(int i=0; i<iParams; i++)
				{
					sParams[i]=null;
					sValues[i]=null;
				}
				*/

                iParams = 0;
            }
            else
            {
                if (oParams != null)
                    oParams.Clear();
            }
            //if(oParams.Count>0)
            //	oParams=new Hashtable();
        }

        /// <summary>
        ///     Appends char to chunk
        /// </summary>
        /// <param name="cChar">Char (byte really)</param>
        public void Append(byte cChar)
        {
            // no range check here for performance reasons - this function gets called VERY often

            bBuffer[iBufPos++] = cChar;
        }

        /// <summary>
        ///     Finalises data from buffer into HTML string
        /// </summary>
        public void Finalise()
        {
            if (HTML.Length == 0)
            {
                if (iBufPos > 0)
                {
                    HTML = Encoding.UTF8.GetString(bBuffer, 0, iBufPos);
                }
            }
            else
            {
                if (iBufPos > 0)
                    //oHTML+=new string(bBuffer,0,iBufPos);
                    HTML += Encoding.UTF8.GetString(bBuffer, 0, iBufPos);
            }

            iHTMLen += iBufPos;
            iBufPos = 0;
        }
    }

    /// <summary>
    ///     HTMLparser -- class that allows to parse HTML split into tokens
    ///     It might just be Unicode compatible as well...
    /// </summary>
    internal class HTMLparser
    {
        /// <summary>
        ///     This chunk will be returned when it was parsed
        /// </summary>
        public HTMLchunk CurrentChunk { get; private set; }

        /// <summary>
        ///     Internal -- dynamic string for text accumulation
        /// </summary>
        private readonly DynaString Text = new DynaString("");

        /// <summary>
        ///     If not true then HTML entities (like nbsp) won't be decoded
        /// </summary>
        public bool DecodeEntities = false;

        // if true nested comments (<!-- <!-- --> -->) will be understood
        //bool bNestedComments=false;

        //byte[] cHTML=null;

        /// <summary>
        ///     Byte array with HTML will be kept here
        /// </summary>
        private byte[] HtmlBytes;

        /// <summary>
        ///     If true then parsed tag chunks will contain raw HTML, otherwise only comments will have it
        ///     Performance hint: keep it as false
        /// </summary>
        public bool KeepRawHTML = false;

        /// <summary>
        ///     If true the text will be returned as split words.
        ///     Performance hint: keep it as false
        /// </summary>
        public bool ReturnSplitWords = false;

        /// <summary>
        ///     If false then text will be ignored -- it will make parser run faster but in effect only HTML tags
        ///     will be returned, its in effect only useful when you just want to parse links or something like this
        /// </summary>
        public bool TextMode = true;

        /// <summary>
        ///     Support for unicode is rudimentary at best, not sure if
        ///     it actually works, this switch however will turn on and off some
        ///     known bits that can introduce Unicode characters into otherwise
        ///     virgin perfection of ASCII.
        ///     Currently this flag will only effect parsing of unicode HTML entities
        /// </summary>
        private const bool UniCodeSupport = true;

        /// <summary>
        ///     Internal - current position pointing to byte in bHTML
        /// </summary>
        private int CurPos;

        /// <summary>
        ///     Length of bHTML -- it appears to be faster to use it than bHTML.Length
        /// </summary>
        private int DataLength;

        /// <summary>
        ///     Internal heuristics for entiries: these will be set to min and max string lengths of known HTML entities
        /// </summary>
        private int MaxEntityLen;

        /// <summary>
        ///     Internal heuristics for entiries: these will be set to min and max string lengths of known HTML entities
        /// </summary>
        private int MinEntityLen;

        /// <summary>
        ///     Supported HTML entities
        /// </summary>
        private Dictionary<string, int> Entities;

        /// <summary>
        ///     Array to provide reverse lookup for entities
        /// </summary>
        private string[] EntityReverseLookup;


        public HTMLparser(byte[] html)
        {
            Init(html);
        }

        public void SetDecodeEntitiesMode(bool bMode)
        {
            DecodeEntities = bMode;
        }

        public void SetChunkHashMode(bool bHashMode)
        {
            CurrentChunk.bHashMode = bHashMode;
        }

        ~HTMLparser()
        {
            Close();
        }

        /// <summary>
        /// </summary>
        public void Close()
        {
            if (HtmlBytes != null)
                HtmlBytes = null;

            if (Entities != null)
            {
                Entities.Clear();
                Entities = null;
            }
        }

        /// <summary>
        ///     Initialises list of entities
        /// </summary>
        private void InitEntities()
        {
            Entities = new Dictionary<string, int>();

            // FIXIT: we will treat non-breakable space... as space!?!
            // perhaps it would be better to have separate return types for entities?
            Entities.Add("nbsp", 32); //oEntities.Add("nbsp",160);
            Entities.Add("iexcl", 161);
            Entities.Add("cent", 162);
            Entities.Add("pound", 163);
            Entities.Add("curren", 164);
            Entities.Add("yen", 165);
            Entities.Add("brvbar", 166);
            Entities.Add("sect", 167);
            Entities.Add("uml", 168);
            Entities.Add("copy", 169);
            Entities.Add("ordf", 170);
            Entities.Add("laquo", 171);
            Entities.Add("not", 172);
            Entities.Add("shy", 173);
            Entities.Add("reg", 174);
            Entities.Add("macr", 175);
            Entities.Add("deg", 176);
            Entities.Add("plusmn", 177);
            Entities.Add("sup2", 178);
            Entities.Add("sup3", 179);
            Entities.Add("acute", 180);
            Entities.Add("micro", 181);
            Entities.Add("para", 182);
            Entities.Add("middot", 183);
            Entities.Add("cedil", 184);
            Entities.Add("sup1", 185);
            Entities.Add("ordm", 186);
            Entities.Add("raquo", 187);
            Entities.Add("frac14", 188);
            Entities.Add("frac12", 189);
            Entities.Add("frac34", 190);
            Entities.Add("iquest", 191);
            Entities.Add("Agrave", 192);
            Entities.Add("Aacute", 193);
            Entities.Add("Acirc", 194);
            Entities.Add("Atilde", 195);
            Entities.Add("Auml", 196);
            Entities.Add("Aring", 197);
            Entities.Add("AElig", 198);
            Entities.Add("Ccedil", 199);
            Entities.Add("Egrave", 200);
            Entities.Add("Eacute", 201);
            Entities.Add("Ecirc", 202);
            Entities.Add("Euml", 203);
            Entities.Add("Igrave", 204);
            Entities.Add("Iacute", 205);
            Entities.Add("Icirc", 206);
            Entities.Add("Iuml", 207);
            Entities.Add("ETH", 208);
            Entities.Add("Ntilde", 209);
            Entities.Add("Ograve", 210);
            Entities.Add("Oacute", 211);
            Entities.Add("Ocirc", 212);
            Entities.Add("Otilde", 213);
            Entities.Add("Ouml", 214);
            Entities.Add("times", 215);
            Entities.Add("Oslash", 216);
            Entities.Add("Ugrave", 217);
            Entities.Add("Uacute", 218);
            Entities.Add("Ucirc", 219);
            Entities.Add("Uuml", 220);
            Entities.Add("Yacute", 221);
            Entities.Add("THORN", 222);
            Entities.Add("szlig", 223);
            Entities.Add("agrave", 224);
            Entities.Add("aacute", 225);
            Entities.Add("acirc", 226);
            Entities.Add("atilde", 227);
            Entities.Add("auml", 228);
            Entities.Add("aring", 229);
            Entities.Add("aelig", 230);
            Entities.Add("ccedil", 231);
            Entities.Add("egrave", 232);
            Entities.Add("eacute", 233);
            Entities.Add("ecirc", 234);
            Entities.Add("euml", 235);
            Entities.Add("igrave", 236);
            Entities.Add("iacute", 237);
            Entities.Add("icirc", 238);
            Entities.Add("iuml", 239);
            Entities.Add("eth", 240);
            Entities.Add("ntilde", 241);
            Entities.Add("ograve", 242);
            Entities.Add("oacute", 243);
            Entities.Add("ocirc", 244);
            Entities.Add("otilde", 245);
            Entities.Add("ouml", 246);
            Entities.Add("divide", 247);
            Entities.Add("oslash", 248);
            Entities.Add("ugrave", 249);
            Entities.Add("uacute", 250);
            Entities.Add("ucirc", 251);
            Entities.Add("uuml", 252);
            Entities.Add("yacute", 253);
            Entities.Add("thorn", 254);
            Entities.Add("yuml", 255);
            Entities.Add("quot", 34);
            Entities.Add("amp", 38);
            Entities.Add("lt", 60);
            Entities.Add("gt", 62);

            if (UniCodeSupport)
            {
                Entities.Add("OElig", 338);
                Entities.Add("oelig", 339);
                Entities.Add("Scaron", 352);
                Entities.Add("scaron", 353);
                Entities.Add("Yuml", 376);
                Entities.Add("circ", 710);
                Entities.Add("tilde", 732);
                Entities.Add("ensp", 8194);
                Entities.Add("emsp", 8195);
                Entities.Add("thinsp", 8201);
                Entities.Add("zwnj", 8204);
                Entities.Add("zwj", 8205);
                Entities.Add("lrm", 8206);
                Entities.Add("rlm", 8207);
                Entities.Add("ndash", 8211);
                Entities.Add("mdash", 8212);
                Entities.Add("lsquo", 8216);
                Entities.Add("rsquo", 8217);
                Entities.Add("sbquo", 8218);
                Entities.Add("ldquo", 8220);
                Entities.Add("rdquo", 8221);
                Entities.Add("bdquo", 8222);
                Entities.Add("dagger", 8224);
                Entities.Add("Dagger", 8225);
                Entities.Add("permil", 8240);
                Entities.Add("lsaquo", 8249);
                Entities.Add("rsaquo", 8250);
                Entities.Add("euro", 8364);
                Entities.Add("fnof", 402);
                Entities.Add("Alpha", 913);
                Entities.Add("Beta", 914);
                Entities.Add("Gamma", 915);
                Entities.Add("Delta", 916);
                Entities.Add("Epsilon", 917);
                Entities.Add("Zeta", 918);
                Entities.Add("Eta", 919);
                Entities.Add("Theta", 920);
                Entities.Add("Iota", 921);
                Entities.Add("Kappa", 922);
                Entities.Add("Lambda", 923);
                Entities.Add("Mu", 924);
                Entities.Add("Nu", 925);
                Entities.Add("Xi", 926);
                Entities.Add("Omicron", 927);
                Entities.Add("Pi", 928);
                Entities.Add("Rho", 929);
                Entities.Add("Sigma", 931);
                Entities.Add("Tau", 932);
                Entities.Add("Upsilon", 933);
                Entities.Add("Phi", 934);
                Entities.Add("Chi", 935);
                Entities.Add("Psi", 936);
                Entities.Add("Omega", 937);
                Entities.Add("alpha", 945);
                Entities.Add("beta", 946);
                Entities.Add("gamma", 947);
                Entities.Add("delta", 948);
                Entities.Add("epsilon", 949);
                Entities.Add("zeta", 950);
                Entities.Add("eta", 951);
                Entities.Add("theta", 952);
                Entities.Add("iota", 953);
                Entities.Add("kappa", 954);
                Entities.Add("lambda", 955);
                Entities.Add("mu", 956);
                Entities.Add("nu", 957);
                Entities.Add("xi", 958);
                Entities.Add("omicron", 959);
                Entities.Add("pi", 960);
                Entities.Add("rho", 961);
                Entities.Add("sigmaf", 962);
                Entities.Add("sigma", 963);
                Entities.Add("tau", 964);
                Entities.Add("upsilon", 965);
                Entities.Add("phi", 966);
                Entities.Add("chi", 967);
                Entities.Add("psi", 968);
                Entities.Add("omega", 969);
                Entities.Add("thetasym", 977);
                Entities.Add("upsih", 978);
                Entities.Add("piv", 982);
                Entities.Add("bull", 8226);
                Entities.Add("hellip", 8230);
                Entities.Add("prime", 8242);
                Entities.Add("Prime", 8243);
                Entities.Add("oline", 8254);
                Entities.Add("frasl", 8260);
                Entities.Add("weierp", 8472);
                Entities.Add("image", 8465);
                Entities.Add("real", 8476);
                Entities.Add("trade", 8482);
                Entities.Add("alefsym", 8501);
                Entities.Add("larr", 8592);
                Entities.Add("uarr", 8593);
                Entities.Add("rarr", 8594);
                Entities.Add("darr", 8595);
                Entities.Add("harr", 8596);
                Entities.Add("crarr", 8629);
                Entities.Add("lArr", 8656);
                Entities.Add("uArr", 8657);
                Entities.Add("rArr", 8658);
                Entities.Add("dArr", 8659);
                Entities.Add("hArr", 8660);
                Entities.Add("forall", 8704);
                Entities.Add("part", 8706);
                Entities.Add("exist", 8707);
                Entities.Add("empty", 8709);
                Entities.Add("nabla", 8711);
                Entities.Add("isin", 8712);
                Entities.Add("notin", 8713);
                Entities.Add("ni", 8715);
                Entities.Add("prod", 8719);
                Entities.Add("sum", 8721);
                Entities.Add("minus", 8722);
                Entities.Add("lowast", 8727);
                Entities.Add("radic", 8730);
                Entities.Add("prop", 8733);
                Entities.Add("infin", 8734);
                Entities.Add("ang", 8736);
                Entities.Add("and", 8743);
                Entities.Add("or", 8744);
                Entities.Add("cap", 8745);
                Entities.Add("cup", 8746);
                Entities.Add("int", 8747);
                Entities.Add("there4", 8756);
                Entities.Add("sim", 8764);
                Entities.Add("cong", 8773);
                Entities.Add("asymp", 8776);
                Entities.Add("ne", 8800);
                Entities.Add("equiv", 8801);
                Entities.Add("le", 8804);
                Entities.Add("ge", 8805);
                Entities.Add("sub", 8834);
                Entities.Add("sup", 8835);
                Entities.Add("nsub", 8836);
                Entities.Add("sube", 8838);
                Entities.Add("supe", 8839);
                Entities.Add("oplus", 8853);
                Entities.Add("otimes", 8855);
                Entities.Add("perp", 8869);
                Entities.Add("sdot", 8901);
                Entities.Add("lceil", 8968);
                Entities.Add("rceil", 8969);
                Entities.Add("lfloor", 8970);
                Entities.Add("rfloor", 8971);
                Entities.Add("lang", 9001);
                Entities.Add("rang", 9002);
                Entities.Add("loz", 9674);
                Entities.Add("spades", 9824);
                Entities.Add("clubs", 9827);
                Entities.Add("hearts", 9829);
                Entities.Add("diams", 9830);
            }

            EntityReverseLookup = new string[10000];

            // calculate min/max lenght of known entities
            foreach (var sKey in Entities.Keys)
            {
                if (sKey.Length < MinEntityLen || MinEntityLen == 0)
                    MinEntityLen = sKey.Length;

                if (sKey.Length > MaxEntityLen || MaxEntityLen == 0)
                    MaxEntityLen = sKey.Length;

                // remember key at given offset
                EntityReverseLookup[Entities[sKey]] = sKey;
            }

            // we don't want to change spaces
            EntityReverseLookup[32] = null;
        }

        /// <summary>
        ///     Parses line and changes known entiry characters into proper HTML entiries
        /// </summary>
        /// <param name="sLine">Line of text</param>
        /// <returns>Line of text with proper HTML entities</returns>
        public string ChangeToEntities(string sLine)
        {
            var oSB = new StringBuilder(sLine.Length);

            for (var i = 0; i < sLine.Length; i++)
            {
                var cChar = sLine[i];

                // yeah I know its lame but its 3:30am and I had v.long debugging session :-/
                switch ((int) cChar)
                {
                    case 39:
                    case 145:
                    case 146:
                    case 147:
                    case 148:
                        oSB.Append("&#" + ((int) cChar) + ";");
                        continue;

                    default:
                        break;
                }
                ;

                if (cChar < EntityReverseLookup.Length)
                {
                    if (EntityReverseLookup[cChar] != null)
                    {
                        oSB.Append("&");
                        oSB.Append(EntityReverseLookup[cChar]);
                        oSB.Append(";");
                        continue;
                    }
                }

                oSB.Append(cChar);
            }

            return oSB.ToString();
        }

        /// <summary>
        ///     Sets text treatment mode
        /// </summary>
        /// <param name="p_bTextMode">
        ///     If TRUE, then text will be parsed, if FALSE then it will be ignored (parsing of tags will be
        ///     faster however)
        /// </param>
        public void SetTextMode(bool p_bTextMode)
        {
            TextMode = p_bTextMode;
        }

        /// <summary>
        ///     Initialises parses with HTML to be parsed from provided data buffer
        /// </summary>
        /// <param name="p_bHTML">Data buffer with HTML in it</param>
        public void Init(byte[] p_bHTML)
        {
            CleanUp();
            HtmlBytes = p_bHTML;
            DataLength = HtmlBytes.Length;
        }

        /// <summary>
        ///     Cleans up parser in preparation for next parsing
        /// </summary>
        public void CleanUp()
        {
            if (Entities == null)
                InitEntities();

            HtmlBytes = null;
            CurrentChunk = new HTMLchunk(true);
            CurPos = 0;
            DataLength = 0;
        }

        /// <summary>
        ///     Resets current parsed data to start
        /// </summary>
        public void Reset()
        {
            CurPos = 0;
        }

        /// <summary>
        ///     Internal: parses tag that started from current position
        /// </summary>
        /// <param name="bKeepWhiteSpace">If true then whitespace will be kept, if false then it won't be (faster option)</param>
        /// <returns>HTMLchunk with tag information</returns>
        private HTMLchunk ParseTag(bool bKeepWhiteSpace)

        {
            /*
			 *  WARNING: this code was optimised for performance rather than for readability, 
			 *  so be extremely careful at changing it -- your changes could easily result in wrongly parsed HTML
			 * 
			 *  This routine takes about 60% of CPU time, in theory its the best place to gain extra speed,
			 *  but I've spent plenty of time doing it, so it won't be easy... and if is easy then please post
			 *  your changes for everyone to enjoy!
			 * 
			 * 
			 * */


            Text.Clear();

            //oChunk.Clear();

            var bWhiteSpace = false;
            var bComments = false;

            // for tracking quotes purposes
            var bQuotes = false;
            byte cQuotes = 0x20;
            //bool bParamValue=false;
            byte cChar = 0;
            byte cPeek;

            // if true it means we have parsed complete tag
            var bGotTag = false;

            var sParam = "";
            var iEqualIdx = 0;

            //bool bQuotesAllowed=false;

            //StringBuilder sText=new StringBuilder(128);
            //,HTMLchunk.TEXT_CAPACITY

            // we reach this function immediately after tag's byte (<) was
            // detected, so we need to save it in order to keep correct HTML copy
            CurrentChunk.Append((byte) '<'); // (byte)'<'

            /*
			oChunk.bBuffer[0]=60;
			oChunk.iBufPos=1;
			oChunk.iHTMLen=1;
			*/

            //while(!Eof())
            //int iTagLength=0;

            while (CurPos < DataLength)
            {
                // we will only skip whitespace OUTSIDE of quotes and comments
                bWhiteSpace = false;

                if (!bQuotes && !bComments && !bKeepWhiteSpace)
                {
                    //bWhiteSpace=SkipWhiteSpace();

                    while (CurPos < DataLength)
                    {
                        cChar = HtmlBytes[CurPos++];

                        if (cChar != ' ' && cChar != '\t' && cChar != 13 && cChar != 10)
                            //if(!char.IsWhiteSpace((char)cChar))
                        {
                            //PutChar();
                            //iCurPos--;
                            break;
                        }
                        bWhiteSpace = true;
                    }

                    if (CurPos >= DataLength)
                        cChar = 0;

                    //cChar=NextChar();

                    if (bWhiteSpace && (KeepRawHTML || !bGotTag || (bGotTag && bComments)))
                        CurrentChunk.Append((byte) ' ');
                }
                else
                {
                    //cChar=NextChar();
                    cChar = HtmlBytes[CurPos++];


                    if (cChar == ' ' || cChar == '\t' || cChar == 10 || cChar == 13)
                    {
                        bWhiteSpace = true;

                        // we don't want that nasty unnecessary 0x0D or 13 byte :-/
                        if (cChar != 13 && (bKeepWhiteSpace || bComments || bQuotes))
                        {
                            if (KeepRawHTML || !bGotTag || (bGotTag && bComments))
                                CurrentChunk.Append(cChar);

                            //sText.Append(cChar);
                            // NOTE: this is manual inlining from actual object
                            // NOTE: speculative execution that requires large enough buffer to avoid overflowing it
                            /*
							if(sText.iBufPos>=DynaString.TEXT_CAPACITY)
							{
								sText.Append(cChar);
							}
							else
							*/
                            {
                                //sText.Length++;
                                Text.bBuffer[Text.iBufPos++] = cChar;
                            }

                            continue;
                        }
                    }
                }

                //if(cChar==0)
                //	break;

                //if(cChar==13 || cChar==10)
                //	continue;

                // check if its entity
                //if(cChar=='&')
                if (cChar == 38 && !bComments)
                {
                    cChar = (byte) CheckForEntity();

                    // restore current symbol
                    if (cChar == 0)
                        cChar = 38; //(byte)'&';
                    else
                    {
                        // we have to skip now to next byte, since 
                        // some converted chars might well be control chars like >
                        CurrentChunk.bEntities = true;

                        if (cChar == '<')
                            CurrentChunk.bLtEntity = true;

                        // unless is space we will ignore it
                        // note that this won't work if &nbsp; is defined as it should
                        // byte int value of 160, rather than 32.
                        //if(cChar!=' ')
                        CurrentChunk.Append(cChar);

                        //continue;
                    }
                }

                // cPeek=Peek();

                cPeek = CurPos < DataLength ? HtmlBytes[CurPos] : (byte) 0;

                // check if we've got tag now: either whitespace before current symbol or the next one is end of string
                if (!bGotTag)
                {
                    CurrentChunk.Append(cChar);

                    if ((Text.iBufPos >= 3 && (Text.bBuffer[0] == '!' && Text.bBuffer[1] == '-' && Text.bBuffer[2] == '-')))
                        bComments = true;

                    if (bWhiteSpace || (!bWhiteSpace && cPeek == '>') || cPeek == 0 || bComments)

                        //|| sText.sText=="!--") || sText.ToString()=="!--"))	//(sText.bBuffer[0]=='!' || sText.sText=="!--") &&
                    {
                        if (cPeek == '>' && cChar != '/')
                            Text.Append(cChar);

                        if (bComments)
                        {
                            CurrentChunk.Tag = "!--";
                            CurrentChunk.Type = HTMLchunkType.Comment;
                            CurrentChunk.bComments = true;
                        }
                        else
                        {
                            CurrentChunk.Tag = Text.ToLowerString();
                        }

                        bGotTag = true;

                        //sText.Remove(0,sText.Length);
                        Text.Clear();
                    }
                }
                else
                {
                    if (KeepRawHTML || bComments)
                        CurrentChunk.Append(cChar);

                    // ought to be parameter
                    if (Text.iBufPos != 0 && !CurrentChunk.bComments)
                    {
                        if ((bWhiteSpace && !bQuotes) || (cPeek == 0 || cPeek == '>'))
                        {
                            if (cPeek == '>' && cChar != '/' && cQuotes != cChar)
                                Text.Append(cChar);

                            // some params consist of key=value combination
                            sParam = Text.ToString();

                            iEqualIdx = sParam.IndexOf('=');

                            //bQuotesAllowed=false;

                            if (iEqualIdx <= 0)
                            {
                                CurrentChunk.AddParam(sParam.ToLower(), "");
                            }
                            else
                            {
                                CurrentChunk.AddParam(sParam.Substring(0, iEqualIdx).ToLower(), sParam.Substring(iEqualIdx + 1, sParam.Length - iEqualIdx - 1));
                                //bQuotesAllowed=true;
                            }


                            //sText.Remove(0,sText.Length);
                            Text.Clear();

                            sParam = null;
                        }
                    }
                }

                switch (cChar)
                {
                    case 0:
                        goto GetOut;

                        //case (byte)'>':
                    case 62:

                        //bQuotesAllowed=false;
                        // if we are in comments mode then we will be waiting for -->
                        if (bComments)
                        {
                            if (LookBack(2) == '-' && LookBack(3) == '-')
                            {
                                bComments = false;
                                return CurrentChunk;
                            }
                        }
                        else
                        {
                            if (!bQuotes)
                            {
                                if (CurrentChunk.bComments)
                                    CurrentChunk.Type = HTMLchunkType.Comment;
                                else
                                {
                                    CurrentChunk.Type = CurrentChunk.bClosure ? HTMLchunkType.CloseTag : HTMLchunkType.OpenTag;
                                }
                                return CurrentChunk;
                            }
                        }

                        break;

                        //case (byte)'"':
                    case 34:
                        //case (byte)'\'':
                    case 39:

                        if (bQuotes)
                        {
                            if (cQuotes == cChar) // && bQuotesAllowed)
                            {
                                bQuotes = false;
                                //bQuotesAllowed=false;
                            }
                            else
                                goto AddSymbol;
                        }
                        else
                        {
                            //if(bQuotesAllowed)
                            {
                                bQuotes = true;
                                cQuotes = cChar;
                            }
                        }

                        break;

                        //case (byte)'/':
                    case 47:

                        if (!bQuotes && !bGotTag)
                            CurrentChunk.bClosure = true;
                        else
                            goto AddSymbol;

                        break;

                    default:

                        AddSymbol:

                        //if(bWhiteSpace && bQuotes)
                        //	sText.Append(0x20);

                        //sText.Append(cChar);
                        // NOTE: this is manual inlining from actual object

                        // NOTE: we go here for speculative insertion that expectes that we won't run out of
                        // buffer, which should be big enough to hold most of HTML data.
                        /*
							if(sText.iBufPos>=DynaString.TEXT_CAPACITY)
							{
								sText.Append(cChar);
							}
							else
						*/
                    {
                        //sText.Length++;
                        Text.bBuffer[Text.iBufPos++] = cChar;
                    }


                        break;
                }
                ;
            }

            GetOut:

            if (CurrentChunk.bComments)
                CurrentChunk.Type = HTMLchunkType.Comment;
            else
            {
                CurrentChunk.Type = CurrentChunk.bClosure ? HTMLchunkType.CloseTag : HTMLchunkType.OpenTag;
            }

            return CurrentChunk;
        }

        /// <summary>
        ///     Looks back X bytes and returns char that was there, or 0 if its start
        /// </summary>
        /// <returns>Previous byte, or 0 if we will reached start position</returns>
        private byte LookBack(int iHowFar)
        {
            if (CurPos >= iHowFar)
                return GetChar(CurPos - iHowFar);

            return 0;
        }

        /// <summary>
        ///     Returns byte at specified position
        /// </summary>
        /// <param name="iPos">Position (WARNING: no range checks here for speed)</param>
        /// <returns>Byte at that position</returns>
        private byte GetChar(int iPos)
        {
            //return bCharConv ? (byte)oCharset.ConvertByte(bHTML[iPos]) : (byte)bHTML[iPos];
            return HtmlBytes[iPos];
        }

        /// <summary>
        ///     Puts back specified number of chars (bytes really)
        /// </summary>
        /// <param name="iChars">Number of chars</param>
        private void PutChars(int iChars)
        {
            if ((CurPos - iChars) >= 0)
                CurPos -= iChars;
        }

        /// <summary>
        ///     Returns next char in data and increments points
        /// </summary>
        /// <returns>Next char or 0 to indicate end of data</returns>
        private byte NextChar()
        {
            //if(Eof())
            if (CurPos >= DataLength)
                return 0;

            //iCurPos++;

            //return bCharConv ? (byte)oCharset.ConvertByte(bHTML[iCurPos-1]) : (byte)bHTML[iCurPos-1];

            return HtmlBytes[CurPos++];
        }

        public HTMLchunk PeakNext()
        {
            var currPos = CurPos;
            var currChunk = CurrentChunk;
            CurrentChunk = new HTMLchunk(true);
            var result = ParseNext();
            CurrentChunk = currChunk;
            CurPos = currPos;
            return result;
        }

        public void StepBack(HTMLchunk chunk)
        {
            if (chunk == null)
                return;
            CurPos = chunk.ContentPosition;
            CurrentChunk = chunk;
        }

        /// <summary>
        ///     Parses next chunk and returns it (whitespace is NOT kept in this version)
        /// </summary>
        /// <returns>HTMLchunk or null if end of data reached</returns>
        public HTMLchunk ParseNext()
        {
            return ParseNext(false);
        }

        /// <summary>
        ///     Parses next chunk and returns it with
        /// </summary>
        /// <param name="bKeepWhiteSpace">If true then whitespace will be preserved (slower)</param>
        /// <returns>HTMLchunk or null if end of data reached</returns>
        public HTMLchunk ParseNext(bool bKeepWhiteSpace)
        {
            CurrentChunk.Clear();
            CurrentChunk.Type = HTMLchunkType.Text;
            CurrentChunk.ContentPosition = CurPos;

            var bWhiteSpace = false;
            byte cChar = 0x00;

            while (true)
            {
                if (!bKeepWhiteSpace)
                {
                    //bWhiteSpace=SkipWhiteSpace();

                    bWhiteSpace = false;

                    while (CurPos < DataLength)
                    {
                        cChar = HtmlBytes[CurPos++];

                        if (cChar != ' ' && cChar != '\t' && cChar != 13 && cChar != 10)
                        {
                            // we don't do anything because we found char that can be used down the pipeline
                            // without need to look it up again
                            //PutChar();
                            //iCurPos--;
                            goto WhiteSpaceDone;
                        }
                        bWhiteSpace = true;
                    }

                    break;
                }
                cChar = NextChar();

                // we are definately done
                if (cChar == 0)
                    break;

                WhiteSpaceDone:

                switch (cChar)
                {
                        //case '<':
                    case 60:


                        // we may have found text bit before getting to the tag
                        // in which case we need to put back tag byte and return
                        // found text first, the tag will be parsed next time
                        if (CurrentChunk.iBufPos > 0 || bWhiteSpace)
                        {
                            // we will add 1 white space chars to compensate for 
                            // loss of space before tag since this space often serves as a delimiter between words
                            if (bWhiteSpace)
                                CurrentChunk.Append(0x20);

                            //PutChar();
                            CurPos--;

                            // finalise chunk if text mode is not false
                            if (TextMode)
                                CurrentChunk.Finalise();

                            return CurrentChunk;
                        }

                        if (!KeepRawHTML)
                            return ParseTag(bKeepWhiteSpace);
                        CurrentChunk = ParseTag(bKeepWhiteSpace);

                        CurrentChunk.Finalise();

                        return CurrentChunk;

                        /*
						 * case 179:
							Console.WriteLine("Found: {0} in {1}!",(char)cChar,oChunk.oHTML.ToString());
							break;
							*/

                    case 13:
                        break;

                    case 10:
                        if (bKeepWhiteSpace)
                        {
                            /*
							if(oChunk==null)
							{
								oChunk=new HTMLchunk(false);
								oChunk.oType=HTMLchunkType.Text;
							}
							*/

                            CurrentChunk.Append(cChar);
                        }
                        break;

                    default:

                        /*
						if(oChunk==null)
						{
							oChunk=new HTMLchunk(false);
							oChunk.oType=HTMLchunkType.Text;
						}
						*/
                        if (TextMode)
                        {
                            // check if its entity
                            if (cChar == '&')
                            {
                                cChar = (byte) CheckForEntity();

                                // restore current symbol
                                if (cChar == 0)
                                    cChar = (byte) '&';
                                else
                                {
                                    CurrentChunk.bEntities = true;

                                    if (cChar == '<')
                                        CurrentChunk.bLtEntity = true;
                                }
                            }

                            if (ReturnSplitWords)
                            {
                                if (bWhiteSpace)
                                {
                                    if (CurrentChunk.iBufPos > 0)
                                    {
                                        //PutChar();
                                        CurPos--;

                                        CurrentChunk.Finalise();
                                        return CurrentChunk;
                                    }
                                }
                                else
                                {
                                    if (char.IsPunctuation((char) cChar))
                                    {
                                        if (CurrentChunk.iBufPos > 0)
                                        {
                                            //PutChar();
                                            CurrentChunk.Finalise();
                                            return CurrentChunk;
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (bWhiteSpace && TextMode)
                                    CurrentChunk.Append((byte) ' ');
                            }

                            CurrentChunk.Append(cChar);
                        }

                        break;
                }
                ;
            }

            if (CurrentChunk.iBufPos == 0)
                return null;

            // it will be null if we have not found any data

            if (TextMode)
                CurrentChunk.Finalise();

            return CurrentChunk;
        }


        /// <summary>
        ///     This function will be called when & is found, and it will
        ///     peek forward to check if its entity, should there be a success
        ///     indicated by non-zero returned, the pointer will be left at the new byte
        ///     after entity
        /// </summary>
        /// <returns>Char (not byte) that corresponds to the entity or 0 if it was not entity</returns>
        private char CheckForEntity()

        {
            if (!DecodeEntities)
                return (char) 0;

            var iChars = 0;
            byte cChar;
            //string sEntity="";

            // if true it means we are getting hex or decimal value of the byte
            var bCharCode = false;
            var bCharCodeHex = false;

            var iEntLen = 0;

            var iFrom = CurPos;

            string sEntity;


            /*
				while(!Eof())
				{
					cChar=NextChar();
				*/
            while (CurPos < DataLength)
            {
                cChar = HtmlBytes[CurPos++];

                iChars++;

                // we are definately done
                if (cChar == 0)
                    break;

                // the first byte for numbers should be #
                if (iChars == 1 && cChar == '#')
                {
                    iFrom++;
                    bCharCode = true;
                    continue;
                }

                if (bCharCode && iChars == 2 && cChar == 'x')
                {
                    iFrom++;
                    iEntLen--;
                    bCharCodeHex = true;
                }

                //Console.WriteLine("Got entity end: {0}",sEntity);
                // Break on:
                // 1) ; - proper end of entity
                // 2) number 10-based entity but current byte is not a number
                if (cChar == ';' || (bCharCode && !bCharCodeHex && !char.IsNumber((char) cChar)))
                {


                    {
                        sEntity = Encoding.UTF8.GetString(HtmlBytes, iFrom, iEntLen);


                        if (bCharCode)
                        {
                            // NOTE: this may fail due to wrong data format,
                            // in which case we will return 0, and entity will be
                            // ignored
                            if (iEntLen > 0)
                            {
                                var iChar = 0;

                                iChar = !bCharCodeHex ? int.Parse(sEntity) : int.Parse(sEntity, NumberStyles.HexNumber);

                                return (char) iChar;
                            }
                        }

                        if (iEntLen >= MinEntityLen && iEntLen <= MaxEntityLen)
                        {
                            int charIndex;
                            if (Entities.TryGetValue(sEntity, out charIndex))
                                return (char) charIndex;
                        }
                    }

                    break;
                }

                // as soon as entity length exceed max length of entity known to us
                // we break up the loop and return nothing found

                if (iEntLen > MaxEntityLen)
                    break;

                //sEntity+=(char)cChar;
                iEntLen++;
            }


            // if we have not found squat, then we will need to put point back
            // to where it was before this function was called
            if (iChars > 0)
                PutChars(iChars);

            return (char) (0);
        }
    }
}