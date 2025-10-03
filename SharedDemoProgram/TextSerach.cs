using System;
using System.Collections.Generic;
using System.Text;
using Foo = FooProject.Collection;

namespace SharedDemoProgram
{
    sealed class TextSearch
    {
        char[] pattern;
        int patternLength;
        Dictionary<char, long> qsTable = new Dictionary<char, long>();
        bool caseInsenstive;
        public TextSearch(string pattern, bool ci = false)
        {
            this.patternLength = pattern.Length;
            this.caseInsenstive = ci;
            if (ci)
            {
                this.CreateQSTable(pattern.ToLower());
                this.CreateQSTable(pattern.ToUpper());
                this.pattern = new char[pattern.Length];
                for (int i = 0; i < pattern.Length; i++)
                    this.pattern[i] = CharTool.ToUpperFastIf(pattern[i]);
            }
            else
            {
                this.CreateQSTable(pattern);
                this.pattern = pattern.ToCharArray();
            }
        }
        void CreateQSTable(string pattern)
        {
            int len = pattern.Length;
            for (int i = 0; i < len; i++)
            {
                if (!this.qsTable.ContainsKey(pattern[i]))
                    this.qsTable.Add(pattern[i], len - i);
                else
                    this.qsTable[pattern[i]] = len - i;
            }
        }
        public long IndexOf(Foo.BigList<char> buf, long start, long end)
        {
            //QuickSearch法
            long buflen = buf.LongCount - 1;
            long plen = this.patternLength;
            long i = start;
            long search_end = end - plen;
            //最適化のためわざとコピペした
            if (this.caseInsenstive)
            {
                while (i <= search_end)
                {
                    long j = 0;
                    while (j < plen)
                    {
                        if (CharTool.ToUpperFastIf(buf.Get(i + j)) != this.pattern[j])
                            break;
                        j++;
                    }
                    if (j == plen)
                    {
                        return i;
                    }
                    else
                    {
                        long k = i + plen;
                        if (k <= buflen)	//buffer以降にアクセスする可能性がある
                        {
                            long moveDelta;
                            if (this.qsTable.TryGetValue(buf.Get(k), out moveDelta))
                                i += moveDelta;
                            else
                                i += plen;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

            }
            else
            {
                while (i <= search_end)
                {
                    int j = 0;
                    while (j < plen)
                    {
                        if (buf.Get(i + j) != this.pattern[j])
                            break;
                        j++;
                    }
                    if (j == plen)
                    {
                        return i;
                    }
                    else
                    {
                        long k = i + plen;
                        if (k <= buflen)	//buffer以降にアクセスする可能性がある
                        {
                            long moveDelta;
                            if (this.qsTable.TryGetValue(buf.Get(k), out moveDelta))
                                i += moveDelta;
                            else
                                i += plen;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return -1;
        }
    }
    static class CharTool
    {
        /// <summary>
        /// Converts characters to lowercase.
        /// </summary>
        const string _lookupStringL =
        "---------------------------------!-#$%&-()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[-]^_`abcdefghijklmnopqrstuvwxyz{|}~-";

        /// <summary>
        /// Converts characters to uppercase.
        /// </summary>
        const string _lookupStringU =
        "---------------------------------!-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";

        /// <summary>
        /// Get lowercase version of this ASCII character.
        /// </summary>
        public static char ToLower(char c)
        {
            return _lookupStringL[c];
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        public static char ToUpper(char c)
        {
            return _lookupStringU[c];
        }

        /// <summary>
        /// Translate uppercase ASCII characters to lowercase.
        /// </summary>
        public static char ToLowerFastIf(char c)
        {
            if (c >= 'A' && c <= 'Z')
            {
                return (char)(c + 32);
            }
            else
            {
                return c;
            }
        }

        /// <summary>
        /// Translate lowercase ASCII characters to uppercase.
        /// </summary>
        public static char ToUpperFastIf(char c)
        {
            if (c >= 'a' && c <= 'z')
            {
                return (char)(c - 32);
            }
            else
            {
                return c;
            }
        }
    }
}
