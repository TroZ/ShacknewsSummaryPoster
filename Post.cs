using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class Post
    {

        private int id = 0;
        private string author = "";
        private DateTime postDate;
        private int parentId = 0;
        private int numEmoji = 0;
        private int uniqueEmoji = 0;
        private string modCategory = "";
        private int replyCount = 0;
        private int threadSize = 0;
        private int shacktags = 0;

        private string text = "";

        private int tag_lol = 0;
        private int tag_inf = 0;
        private int tag_unf = 0;
        private int tag_tag = 0;
        private int tag_aww = 0;
        private int tag_wow = 0;
        private int tag_wtf = 0;

        private string emojis = "";

        private int wordCount = 0;
        private double threadChattyness = 0.0;

        public int Id { get => id; set => id = value; }
        public string Author { get => author; set => author = value; }
        public DateTime PostDate { get => postDate; set => postDate = value; }
        public int NumEmoji { get => numEmoji; set => numEmoji = value; }
        public string Emojis { get => emojis; set => emojis = value; }
        public int UniqueEmoji { get => uniqueEmoji; set => uniqueEmoji = value; }
        public int Tag_lol { get => tag_lol; set => tag_lol = value; }
        public int Tag_inf { get => tag_inf; set => tag_inf = value; }
        public int Tag_unf { get => tag_unf; set => tag_unf = value; }
        public int Tag_tag { get => tag_tag; set => tag_tag = value; }
        public int Tag_aww { get => tag_aww; set => tag_aww = value; }
        public int Tag_wow { get => tag_wow; set => tag_wow = value; }
        public int Tag_wtf { get => tag_wtf; set => tag_wtf = value; }
        public string Text { get => text; set => text = value; }
        public bool Nws { get => modCategory == "nws";  }
        public string ModCategory { get => modCategory; set => modCategory = value; }
        public int ReplyCount { get => replyCount; set => replyCount = value; }
        public int ParentId { get => parentId; set => parentId = value; }
        public int WordCount { get => wordCount; set => wordCount = value; }
        public double ThreadChattyness { get => threadChattyness; set => threadChattyness = value; }
        public int ThreadSize { get => threadSize; set => threadSize = value; }
        public int Shacktags { get => shacktags; set => shacktags = value; }
    }
}
