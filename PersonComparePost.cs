using System;
using System.Collections.Generic;
using System.Text;

namespace Shackmojis
{
    class PersonComparePost : IComparer<Person>
    {
        int IComparer<Person>.Compare(Person px, Person py)
        {

            if (px.PostCount > py.PostCount)
            {
                return -1;
            }
            else if (px.PostCount < py.PostCount)
            {
                return 1;
            }
            else if (px.ReplyCount > py.ReplyCount)
            {
                return -1;
            }
            else if (px.ReplyCount < py.ReplyCount)
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
