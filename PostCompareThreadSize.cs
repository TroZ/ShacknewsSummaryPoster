using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostCompareThreadSize : IComparer<Post>
    {
        int IComparer<Post>.Compare(Post px, Post py)
        {

            if (px.ThreadSize > py.ThreadSize)
            {
                return -1;
            }
            else if (px.ThreadSize < py.ThreadSize)
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
