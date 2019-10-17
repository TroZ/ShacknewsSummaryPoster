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

        private HashSet<string> emojis = new HashSet<string>();

        public string Name { get => name; set => name = value; }
        public int EmojiCount { get => emojiCount; set => emojiCount = value; }
        public string Emojis { get => String.Join("",emojis); }
        public int UniqueEmoji { get => emojis.Count; }
        public int PostCount { get => postCount; set => postCount = value; }
        public int ReplyCount { get => replyCount; set => replyCount = value; }
        public int CharCount { get => charCount; set => charCount = value; }

        public void addEmoji(HashSet<string> newEmoji)
        {
            foreach(string emoji in newEmoji)
            {
                emojis.Add(emoji);
            }
        }
    }
}
