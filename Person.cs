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

        private string emojis;

        public string Name { get => name; set => name = value; }
        public int EmojiCount { get => emojiCount; set => emojiCount = value; }
        public string Emojis { get => emojis; set => emojis = value; }
        public int UniqueEmoji { get => uniqueEmoji; set => uniqueEmoji = value; }
    }
}
