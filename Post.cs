using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class Post
    {

        private int id;
        private string author;
        private DateTime postDate;
        private int numEmoji;
        private int uniqueEmoji;

        private bool nws=false;
        private string text;

        private int tag_lol;
        private int tag_inf;
        private int tag_unf;
        private int tag_tag;
        private int tag_aww;
        private int tag_wow;
        private int tag_wtf;

        private string emojis;

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
        public bool Nws { get => nws; set => nws = value; }
        
    }
}
