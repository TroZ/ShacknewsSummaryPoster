using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PersonCompareShacktags : IComparer<Person>
    {
        int IComparer<Person>.Compare(Person px, Person py)
        {

            if (px.ShacktagCount > py.ShacktagCount)
            {
                return -1;
            }
            else if (px.ShacktagCount < py.ShacktagCount)
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
