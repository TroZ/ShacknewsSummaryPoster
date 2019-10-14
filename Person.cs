using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class Person
    {
        private string name;
        private int emojiCount;
        private int uniqueEmoji;
        private int postCount = 0;
        private int replyCount = 0;
        private int charCount = 0;

        private string emojis;

        public string Name { get => name; set => name = value; }
        public int EmojiCount { get => emojiCount; set => emojiCount = value; }
        public string Emojis { get => emojis; set => emojis = value; }
        public int UniqueEmoji { get => uniqueEmoji; set => uniqueEmoji = value; }
        public int PostCount { get => postCount; set => postCount = value; }
        public int ReplyCount { get => replyCount; set => replyCount = value; }
        public int CharCount { get => charCount; set => charCount = value; }
    }
}
