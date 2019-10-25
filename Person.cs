using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class Person
    {
        private string name;
        private int emojiCount;
        private int postCount = 0;
        private int replyCount = 0;
        private int charCount = 0;

        private int tag_lol_recv_count;
        private int tag_inf_recv_count;
        private int tag_unf_recv_count;
        private int tag_tag_recv_count;
        private int tag_aww_recv_count;
        private int tag_wow_recv_count;
        private int tag_wtf_recv_count;

        private HashSet<string> emojis = new HashSet<string>();

        public string Name { get => name; set => name = value; }
        public int EmojiCount { get => emojiCount; set => emojiCount = value; }
        public string Emojis { get => String.Join("",emojis); }
        public int UniqueEmoji { get => emojis.Count; }
        public int PostCount { get => postCount; set => postCount = value; }
        public int ReplyCount { get => replyCount; set => replyCount = value; }
        public int CharCount { get => charCount; set => charCount = value; }
        public int Tag_lol_recv_count { get => tag_lol_recv_count; set => tag_lol_recv_count = value; }
        public int Tag_inf_recv_count { get => tag_inf_recv_count; set => tag_inf_recv_count = value; }
        public int Tag_unf_recv_count { get => tag_unf_recv_count; set => tag_unf_recv_count = value; }
        public int Tag_tag_recv_count { get => tag_tag_recv_count; set => tag_tag_recv_count = value; }
        public int Tag_aww_recv_count { get => tag_aww_recv_count; set => tag_aww_recv_count = value; }
        public int Tag_wow_recv_count { get => tag_wow_recv_count; set => tag_wow_recv_count = value; }
        public int Tag_wtf_recv_count { get => tag_wtf_recv_count; set => tag_wtf_recv_count = value; }

        public void addEmoji(HashSet<string> newEmoji)
        {
            foreach(string emoji in newEmoji)
            {
                emojis.Add(emoji);
            }
        }
    }
}
