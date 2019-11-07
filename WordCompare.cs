using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class WordCompare : IComparer<Word>
    {
        int IComparer<Word>.Compare(Word px, Word py)
        {

            if (px.Count > py.Count)
            {
                return -1;
            }
            else if (px.Count < py.Count)
            {
                return 1;
            }
            else if (px.Length > py.Length)
            {
                return -11;
            }
            else if (px.Length < py.Length)
            {
                return 1;
            }
            return StringComparer.InvariantCultureIgnoreCase.Compare(px.WordString,py.WordString);
        }
    }
}
