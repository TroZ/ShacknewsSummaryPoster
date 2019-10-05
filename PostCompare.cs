using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostCompare : IComparer<Post>
    {
        int IComparer<Post>.Compare(Post px, Post py)
        {
            
            if (px.NumEmoji > py.NumEmoji)
            {
                return -1;
            }
            else if (px.NumEmoji < py.NumEmoji)
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
