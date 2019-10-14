using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostCompareDate : IComparer<Post>
    {
        int IComparer<Post>.Compare(Post px, Post py)
        {

            if (px.PostDate > py.PostDate)
            {
                return 1;
            }
            else if (px.PostDate < py.PostDate)
            {
                return -1;
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