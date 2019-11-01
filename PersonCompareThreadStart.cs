using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PersonCompareThreadStart : IComparer<Person>
    {
        int IComparer<Person>.Compare(Person px, Person py)
        {

            if (px.RootPostCount > py.RootPostCount)
            {
                return -1;
            }
            else if (px.RootPostCount < py.RootPostCount)
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
