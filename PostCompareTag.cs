using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostCompareTag : IComparer<Post>
    {
        public const int TAG_LOL = 0;
        public const int TAG_INF = 1;
        public const int TAG_UNF = 2;
        public const int TAG_TAG = 3;
        public const int TAG_WOW = 4;
        public const int TAG_AWW = 5;
        public const int TAG_WTF = 6;
        public const int TAG_MAX = 7;

        readonly int tag = 0;

        public PostCompareTag(int fortag)
        {
            tag = fortag;
        }

        int IComparer<Post>.Compare(Post px, Post py)
        {
            int xval = GetTagCount(px, tag);
            int yval = GetTagCount(py, tag);
            if (xval > yval)
            {
                return -1;
            }
            else if (xval < yval)
            {
                return 1;
            }
            else if (px.Id > py.Id)
            {
                return 1;
            }
            else if (px.Id < py.Id)
            {
                return -1;
            }
            return 0;
        }

        public static int GetTagCount(Post p,int tag)
        {
            switch (tag)
            {
                case TAG_INF:
                    return p.Tag_inf;
                case TAG_UNF:
                    return p.Tag_unf;
                case TAG_TAG:
                    return p.Tag_tag;
                case TAG_WOW:
                    return p.Tag_wow;
                case TAG_AWW:
                    return p.Tag_aww;
                case TAG_WTF:
                    return p.Tag_wtf;
                case TAG_MAX:
                    return p.Tag_lol+ p.Tag_inf+ p.Tag_unf+ p.Tag_tag + p.Tag_wow+ p.Tag_aww+p.Tag_wtf;
                default:
                    return p.Tag_lol;
            }
        }

        public static string GetTagName( int tag)
        {
            switch (tag)
            {
                case TAG_INF:
                    return "b{INF}b";
                case TAG_UNF:
                    return "r{UNF}r";
                case TAG_TAG:
                    return "g{TAG}g";
                case TAG_WOW:
                    return "p[WOW]p";
                case TAG_AWW:
                    return "l[/[AWW]/]l";
                case TAG_WTF:
                    return "e[WTF]e";
                default:
                    return "n[LOL]n";
            }
        }
    }
}
