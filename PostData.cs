using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PostData
    {
        public int id = 0;
        public DateTime postDate = new DateTime();
        public bool summary;

        public PostData(int postId, DateTime postTime)
        {
            id = postId;
            postDate = postTime;
        }
    }
}
