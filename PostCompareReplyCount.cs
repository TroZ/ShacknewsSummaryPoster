using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostCompareReplyCount : IComparer<Post>
    {
        int IComparer<Post>.Compare(Post px, Post py)
        {

            if (px.ReplyCount > py.ReplyCount)
            {
                return -1;
            }
            else if (px.ReplyCount < py.ReplyCount)
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
    }
}
