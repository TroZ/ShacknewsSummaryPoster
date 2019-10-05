using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PersonCompare : IComparer<Person>
    {
        int IComparer<Person>.Compare(Person px, Person py)
        {

            if (px.EmojiCount > py.EmojiCount)
            {
                return -1;
            }
            else if (px.EmojiCount < py.EmojiCount)
            {
                return 1;
            }
            else 
            {
                return StringComparer.InvariantCultureIgnoreCase.Compare(px.Name, py.Name);
            }
        }
    }
}
