using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class Word
    {
        String word;
        int count = 0;

        public Word(string word)
        {
            this.word = word;
        }

        public string WordString { get => word; }
        public int Count { get => count; set => count = value; }
        public int Length
        {
            get => word.Length;
        }
    }
}
