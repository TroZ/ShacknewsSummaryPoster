using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostCompareChattyness : IComparer<Post>
    {
        int IComparer<Post>.Compare(Post px, Post py)
        {

            if (px.ThreadChattyness > py.ThreadChattyness)
            {
                return -1;
            }
            else if (px.ThreadChattyness < py.ThreadChattyness)
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

