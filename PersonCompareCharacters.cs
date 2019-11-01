using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PersonCompareCharacters : IComparer<Person>
    {
        int IComparer<Person>.Compare(Person px, Person py)
        {

            if (px.CharCount > py.CharCount)
            {
                return -1;
            }
            else if (px.CharCount < py.CharCount)
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
