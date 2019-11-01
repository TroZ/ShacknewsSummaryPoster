using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostCompareShacktags : IComparer<Post>
    {
        int IComparer<Post>.Compare(Post px, Post py)
        {

            if (px.Shacktags > py.Shacktags)
            {
                return -1;
            }
            else if (px.Shacktags < py.Shacktags)
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
