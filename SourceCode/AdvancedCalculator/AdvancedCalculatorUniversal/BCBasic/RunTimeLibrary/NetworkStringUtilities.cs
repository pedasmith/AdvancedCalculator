using System;
using System.Collections.Generic;

// Copied from Telnet Advanced from the Gopher network library
namespace Network
{
    public static class StringUtilities
    {
        /// <summary>
        /// Guess the line ending type based on how many \n versus \r\n there are.
        /// </summary>
        public enum LineEnd { CRLF, NL, Mixed_CRLF_NL }
        public static LineEnd GuessLineEnd(this string text)
        {
            int nNL = 0; // \n is a LF or NL=NewLine character
            int nCRLF = 0; // \r\n
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n' && i > 0 && text[i - 1] == '\r') nCRLF++;
                else if (text[i] == '\n') nNL++;
            }
            if (nNL == nCRLF) return LineEnd.CRLF;
            else if (nNL > 0 && nCRLF == 0) return LineEnd.NL;
            else if (nNL == 0 && nCRLF > 0) return LineEnd.CRLF;
            else if (nNL == 0 && nCRLF == 0) return LineEnd.NL; // got to return something...

            // Something is off. Some lines end with NL and some with CRLF
            if (nNL > nCRLF) return LineEnd.Mixed_CRLF_NL;

            // Something is really off. How can there be more CRLF than NL?
            return LineEnd.Mixed_CRLF_NL;
        }

        public static IList<string> SplitGuessEnding(this string text)
        {
            var ending = text.GuessLineEnd();
            switch (ending)
            {
                case LineEnd.CRLF:
                    return SplitCRLF(text);
                case LineEnd.Mixed_CRLF_NL:
                    return SplitMixedCRLF_LF(text);
                default:
                    return text.Split(new char[] { '\n' });
            }
        }

        public static IList<string> SplitMixedCRLF_LF(this string text)
        {
            // By replacing in this order I end up with a string with just \n as the line endings.
            // \r by themselves are replaced by \n but  \r\n won't be turned into \n\n which would be bad.
            var tmp = text.Replace("\r\n", "\n");
            tmp = tmp.Replace('\r', '\n');
            var list = tmp.Split(new char[] { '\n' });
            return list;
        }

        /// <summary>
        /// Splits the incoming string by CR-LF
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IList<string> SplitCRLF(this string text)
        {
            var Retval = new List<string>();
            char lastChar = (char)0;
            int lastIndex = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (lastChar == '\r' && ch == '\n')
                {
                    var len = i - lastIndex - 1;
                    var substr = text.Substring(lastIndex, len);
                    Retval.Add(substr);
                    lastIndex = i + 1;
                }
                lastChar = ch;
            }
            if (text.Length > lastIndex)
            {
                var lastLine = text.Substring(lastIndex);
                Retval.Add(lastLine);
            }
            return Retval;
        }


        // From https://github.com/0x53A/Mvvm/blob/cb8aacb35ebc5713977190d1392c95ff89496b5e/src/Mvvm/src/Utf8Checker.cs
        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="buffer"></param> 
        /// <param name="length"></param> 
        /// <returns></returns> 
        public static bool IsUtf8(byte[] buffer, int length)
        {

           int position = 0;
            int bytes = 0;
            while (position < length)
            {
                if (!IsValid(buffer, position, length, ref bytes))
                {
                    return false;
                }
                position += bytes;
            }
            return true;
        }
        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="buffer"></param> 
        /// <param name="position"></param> 
        /// <param name="length"></param> 
        /// <param name="bytes"></param> 
        /// <returns></returns> 
        public static bool IsValid(byte[] buffer, int position, int length, ref int bytes)
        {
            if (length > buffer.Length)
            {
                throw new ArgumentException("Invalid length");
            }
            if (position > length - 1)
            {
                bytes = 0;
                return true;
            }
            byte ch = buffer[position];
            if (ch <= 0x7F)
            {
                bytes = 1;
                return true;
            }
            if (ch >= 0xc2 && ch <= 0xdf)
            {
                if (position > length - 2)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 2;
                return true;
            }
            if (ch == 0xe0)
            {
                if (position > length - 3)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 3;
                return true;
            }

            if (ch >= 0xe1 && ch <= 0xef)
            {
                if (position > length - 3)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 3;
                return true;
            }
            if (ch == 0xf0)
            {
                if (position > length - 4)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 4;
                return true;
            }
            if (ch == 0xf4)
            {
                if (position > length - 4)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 4;
                return true;
            }
            if (ch >= 0xf1 && ch <= 0xf3)
            {
                if (position > length - 4)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 4;
                return true;
            }

            return false;
        }
    }
}
