using System;
using System.Collections.Generic;
using System.Text;

namespace ShackDailyPostCompiler
{
    class PostDataCompare : IComparer<PostData>
    {
        int IComparer<PostData>.Compare(PostData px, PostData py)
        {
            int result = DateTime.Compare(px.postDate, py.postDate);
            if (result == 0)
            {
                if (px.id < py.id)
                {
                    return -1;
                }
                else if (px.id > py.id)
                {
                    return 1;
                }
                return 0;
            }
            return result;
            
        }
    }
}
