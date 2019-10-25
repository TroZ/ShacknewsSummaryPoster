using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PersonCompareTag : IComparer<Person>
    {
        public const int TAG_LOL = PostCompareTag.TAG_LOL;
        public const int TAG_INF = PostCompareTag.TAG_INF;
        public const int TAG_UNF = PostCompareTag.TAG_UNF;
        public const int TAG_TAG = PostCompareTag.TAG_TAG;
        public const int TAG_WOW = PostCompareTag.TAG_WOW;
        public const int TAG_AWW = PostCompareTag.TAG_AWW;
        public const int TAG_WTF = PostCompareTag.TAG_WTF;
        public const int TAG_MAX = PostCompareTag.TAG_MAX;

        readonly int tag = 0;

        public PersonCompareTag(int fortag)
        {
            tag = fortag;
        }

        int IComparer<Person>.Compare(Person px, Person py)
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
            else 
            {
                return StringComparer.CurrentCultureIgnoreCase.Compare(px.Name,py.Name);
            }
            
        }

        public static int GetTagCount(Person p, int tag)
        {
            switch (tag)
            {
                case TAG_INF:
                    return p.Tag_inf_recv_count;
                case TAG_UNF:
                    return p.Tag_unf_recv_count;
                case TAG_TAG:
                    return p.Tag_tag_recv_count;
                case TAG_WOW:
                    return p.Tag_wow_recv_count;
                case TAG_AWW:
                    return p.Tag_aww_recv_count;
                case TAG_WTF:
                    return p.Tag_wtf_recv_count;
                default:
                    return p.Tag_lol_recv_count;
            }
        }

        public static string GetTagName(int tag)
        {
            return PostCompareTag.GetTagName(tag);
        }
    }
}
