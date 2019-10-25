using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

/*
MIT License

Copyright (c) 2019 Brian Risinger

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
*/

namespace Shackmojis
{
    class ShackPostReport
    {
        const string SUMMARY_THREAD_START = "Shack Post Report for ";
        readonly Hashtable emojis = new Hashtable();
        readonly SortedList<Post, Post> posts = new SortedList<Post, Post>(new PostCompare());
        readonly SortedList<Person, Person> posters = new SortedList<Person, Person>(new PersonCompare());
        readonly Dictionary<string, Person> posterList = new Dictionary<string, Person>();


        DateTime startTime = DateTime.Now;
        DateTime minPostDate = DateTime.Now;
        DateTime maxPostDate = new DateTime();

        readonly SortedList<Post, Post> postsLol = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_LOL));
        readonly SortedList<Post, Post> postsInf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_INF));
        readonly SortedList<Post, Post> postsUnf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_UNF));
        readonly SortedList<Post, Post> postsTag = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_TAG));
        readonly SortedList<Post, Post> postsWow = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WOW));
        readonly SortedList<Post, Post> postsAww = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_AWW));
        readonly SortedList<Post, Post> postsWtf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WTF));

        readonly SortedList<Post, Post> postsModInfo = new SortedList<Post, Post>(new PostCompareDate());
        readonly SortedList<Post, Post> postsModTang = new SortedList<Post, Post>(new PostCompareDate());
        readonly SortedList<Post, Post> postsModStpd = new SortedList<Post, Post>(new PostCompareDate());
        readonly SortedList<Post, Post> postsModPol = new SortedList<Post, Post>(new PostCompareDate());
        readonly SortedList<Post, Post> postsModNws = new SortedList<Post, Post>(new PostCompareDate());
        readonly String[] ModTypes = { "Interesting", "Tangent", "Stupid", "Political", "NWS" };

        readonly SortedList<Post, Post> threadChattyness = new SortedList<Post, Post>(new PostCompareChattyness());

        readonly Dictionary<string, int> replyChart = new Dictionary<string, int>();

        Post biggestThread;
        int biggestThreadCount=0;
        Post mostReplied;
        Post mostTags;
        int mostTagsCount;
        readonly Dictionary<int, Post> currentThread = new Dictionary<int, Post>();

        //static Regex rx = new Regex(@"&#x?[a-fA-F0-9]{2,6};", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        readonly static Regex rxTag = new Regex(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        readonly static Regex rxLink = new Regex(@"</?a[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        readonly static Regex rxFullLink = new Regex(@"<a.*?/a>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        int rootCount = 0;
        int replyCount = 0;

        int ontopicCount = 0;
        int nwsCount = 0;
        int stupidCount = 0;
        int politicalCount = 0;
        int tangentCount = 0;
        int interestingCount = 0; 

        int lolCount = 0;
        int infCount = 0;
        int unfCount = 0;
        int tagCount = 0;
        int wowCount = 0;
        int awwCount = 0;
        int wtfCount = 0;


        static int maxHours = 24 + 20; //normally one day of post then extra 18 hours for a root post at the end to time out 
        int[,] postTime = new int[2, maxHours];

        public ShackPostReport()
        {
            DateTime now = DateTime.Now;
            DateTime yesterday = now.AddDays(-1);

            //clear postTime
            for(int x = 0; x < 2; x++)
            {
                for(int y = 0; y < maxHours; y++)
                {
                    postTime[x, y] = 0;
                }
            }

            //string testPost = "Test Post\n/{{ 1,  2,  3\r11, 22, 33\r}}/";
            //int id = MakePost(38956503, testPost);

            //int id = MakePost(0, "Test post, please ignore.");

            //System.Console.WriteLine("post id: " + id);

            //string teststr = "<span class=\"jt_yellow\">Botham Jean's brother forgives, embraces Amber Guyger</span><br /><br /><a target=\"_blank\" rel=\"nofollow\" href=\"https://www.youtube.com/watch?v=dJH4adVazl4\">https://www.youtube.com/watch?v=dJH4adVazl4</a><br /><br />Damn, that's powerful. What a truly amazing person. ";
            //string teststr = "<span class=\"jt_yellow\"><span class=\"jt_yellow\">test str</span></sp";
            //teststr = FixShackTags(teststr);

            dynamic response = GetDayRootPosts(yesterday);

            //System.Console.WriteLine(response.ToString());

            SortedList<Post, Post>[] tagLists = new SortedList<Post, Post>[7];
            tagLists[0] = postsLol;
            tagLists[1] = postsInf;
            tagLists[2] = postsUnf;
            tagLists[3] = postsTag;
            tagLists[4] = postsWow;
            tagLists[5] = postsAww;
            tagLists[6] = postsWtf;

            SortedList<Post, Post>[] modLists = new SortedList<Post, Post>[5];
            modLists[0] = postsModInfo;
            modLists[1] = postsModTang;
            modLists[2] = postsModStpd;
            modLists[3] = postsModPol;
            modLists[4] = postsModNws;

            GetThreadRootTimes(response);

            if (response != null && response.rootPosts != null)
            {
                int c = response.rootPosts.Count;
                System.Console.WriteLine("Thread Count: "+c);
            }

            GetThreads(response); //this does most of the work, getting emoji counts, and also make the lists of top tagged posts

            foreach (Person pp in posterList.Values)
            {
                posters.Add(pp, pp); //makes a sorted list by number of emojis
            }

            string bodyParent = SUMMARY_THREAD_START + yesterday.ToLongDateString() + ", the "+AddOrdinal(yesterday.DayOfYear)+" day of the year.\n";
            bodyParent += "b[" + rootCount + "]b _[e[Root Posts]e]_, b[" + replyCount + "]b /[l[Replies]l]/";
            if (interestingCount > 0) {
                bodyParent += ", b[" + interestingCount + "]b b{interesting}b posts";
            }
            //if (ontopicCount > 0) { //default is ontopic, so we don't really need to print the number
            //    bodyParent += ", " + ontopicCount + " ontopic posts";
            //}
            if (tangentCount > 0)
            {
                bodyParent += ", b[" + tangentCount + "]b y{tangent}y posts";
            }
            if (stupidCount > 0)
            {
                bodyParent += ", b[" + stupidCount + "]b g{stupid}g posts";
            }
            if (politicalCount > 0)
            {
                bodyParent += ", b[" + politicalCount + "]b n[political]n posts";
            }
            if (nwsCount > 0)
            {
                bodyParent += ", b[" + nwsCount + "]b r{nws}r posts";
            }

            bodyParent += "\n";

            //daily thing
            switch (DateTime.Now.DayOfWeek)
            {
                case System.DayOfWeek.Sunday:
                    {
                        
                        int tid = GetTodayThreadId("Sunday is for Pets");
                        if (tid > 0)
                            {
                                bodyParent += "🐶 Sunday 🐱 Pets 🐹 Thread 🐾 : https://www.shacknews.com/chatty?id=" + tid + "#item_" + tid ;
                            }
                            else
                            {
                                bodyParent += "Check out today's 🐶🐹Pets🐾🐱 thread!";
                            }
                        tid = GetTodayThreadId("THE OFFICIAL SHACKNEWS NFL THREAD");
                        if (tid > 0)
                        {
                            bodyParent += " Also, 🏈 FOOTBALL 🏈 :  https://www.shacknews.com/chatty?id=" + tid + "#item_" + tid ;
                        }
                        bodyParent += "\n";
                        break;
                    }
                case System.DayOfWeek.Monday:
                    bodyParent += "⚡ Mercury Monday! Have you signed up? https://www.shacknews.com/mercury/overview ⚡\n";
                    break;
                case System.DayOfWeek.Tuesday:
                    {
                        bodyParent += "📚 Topics Tuesdays: ";
                        System.Random rnd = new Random();
                        switch (rnd.Next(5))
                        {
                            case 4:
                                bodyParent += "⭐ Reviews: https://www.shacknews.com/topic/review \n";
                                break;
                            case 1:
                                bodyParent += "📗 Guides: https://www.shacknews.com/topic/guide \n"; 
                                break;
                            case 2: 
                                bodyParent += "🎙️ Podcasts: https://www.shacknews.com/topic/podcast \n"; 
                                break;
                            case 3:
                                bodyParent += "🎞️ Videos: https://www.shacknews.com/topic/video \n"; 
                                break;
                            default:
                                bodyParent += "📖 Long-reads: https://www.shacknews.com/topic/long-read \n"; 
                                break;
                        }
                    }
                    break;
                case System.DayOfWeek.Wednesday:
                    //EPIC STORE REMINDER
                    bodyParent += "⚠️ Reminder: Last day to get the current free Epic Store game!\n";
                    break;
                case System.DayOfWeek.Thursday:
                    //EPIC STORE REMINDER
                    bodyParent += "❗ A new free Epic Store game is available.\n";
                    break;
                case System.DayOfWeek.Friday:
                    {
                        int tid = GetTodayThreadId("FRIDAY GIF ANIMATION THREAD");
                        if (tid > 0)
                        {
                            bodyParent += "🖼️ Today's GIF Thread: https://www.shacknews.com/chatty?id=" + tid + "#item_" + tid + "\n";
                        }
                        else
                        {
                            bodyParent += "🖼️ Check out today's GIF thread!\n";
                        }
                        break;
                    }
                case System.DayOfWeek.Saturday:
                    { 
                        int tid = GetTodayThreadId("Selfie Saturday");
                        if (tid > 0)
                        {
                            bodyParent += "📱 Selfie Saturday Thread: https://www.shacknews.com/chatty?id=" + tid + "#item_" + tid + "\n";
                        }
                        else
                        {
                            bodyParent += "📱 Check out today's Selfie Saturday thread!\n";
                        }
                        break;
                    }

            }

            bodyParent += "p[In reply]p:";


            
            //body = EncodeEmoji(body);

            System.Console.WriteLine(bodyParent);
            System.Console.WriteLine("\n\n");


            int id = MakePost(0, bodyParent);
            /*
            id =  1;
            /*/
            id = GetNewRootPostId(bodyParent,Program.USERNAME);
            //*/

            if (id > 0)
            {

                string body2 = "Moderated Posts"+ " s[Roots posts from " +
                minPostDate.ToShortDateString() + " " + minPostDate.ToShortTimeString() + " to " +
                maxPostDate.ToShortDateString() + " " + maxPostDate.ToShortTimeString() + " " + System.TimeZoneInfo.Local.StandardName +
                "]s:\n";
                for (int i = 0;i< modLists.Length; i++)
                {
                    if(modLists[i].Count < 10 && modLists[i].Count > 0)
                    {
                        body2 += ModTypes[i] + " posts:\n";
                        foreach(Post p in modLists[i].Values){
                            body2 += PrintPost(p, -1, 128) + "\n\n";
                        }
                        body2 += "\n\n";
                    }
                }

                body2 += "\n\n\n\nComment or Suggestions for this thread?  Contact y{TroZ}y  Source: https://github.com/TroZ/ShacknewsSummaryPoster ";

                System.Console.WriteLine(body2);
                System.Console.WriteLine("\n\n");
                MakePost(id, body2);
                Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons 

                for (int tagType = 0; tagType < PostCompareTag.TAG_MAX; tagType++)
                {

                    System.Console.WriteLine("Posting type " + PostCompareTag.GetTagName(tagType));

                    int totalCount = 0;
                    switch(tagType){
                        default:
                        case PostCompareTag.TAG_LOL: totalCount = lolCount; break;
                        case PostCompareTag.TAG_INF: totalCount = infCount; break;
                        case PostCompareTag.TAG_UNF: totalCount = unfCount; break;
                        case PostCompareTag.TAG_TAG: totalCount = tagCount; break;
                        case PostCompareTag.TAG_WOW: totalCount = wowCount; break;
                        case PostCompareTag.TAG_AWW: totalCount = awwCount; break;
                        case PostCompareTag.TAG_WTF: totalCount = wtfCount; break;
                    }

                    int count = 3;
                    if (tagType < PostCompareTag.TAG_WOW && tagType != PostCompareTag.TAG_UNF)
                    {
                        count = 5;
                    }
                    if(totalCount > 100)
                    {
                        count = 5;
                    }

                    if (count > tagLists[tagType].Count)
                    {
                        count = tagLists[tagType].Count;
                    }

                    string body = "" + totalCount + " " + PostCompareTag.GetTagName(tagType) + "s tagged. ";
                    body += "Top " + count + " " + PostCompareTag.GetTagName(tagType) + "s:\n\n";
                    body += PrintMultiLoler(tagLists[tagType], count, tagType);
                    body += PrintPostList(tagLists[tagType], count, tagType);

                    body += "\n\n\n\n";
                    //now add top person list
                    SortedList<Person, Person> personTagged = new SortedList<Person, Person>(new PersonCompareTag(tagType));
                    foreach(Person P in posterList.Values)
                    {
                        if (PersonCompareTag.GetTagCount(P, tagType) > 0)
                        {
                            personTagged.Add(P, P);
                        }
                    }
                    int maxp = 10;
                    if(personTagged.Count < maxp)
                    {
                        maxp = personTagged.Count;
                    }
                    body += "Top " + maxp + " " + PersonCompareTag.GetTagName(tagType) + "'d posters:\n";
                    int pp = 0;
                    foreach(Person P in personTagged.Values)
                    {
                        body += "" + PersonCompareTag.GetTagCount(P, tagType) + " - " + P.Name + "\n";
                        pp++;
                        if (pp >= maxp) {
                            break;
                        }
                    }



                    System.Console.WriteLine(body);
                    System.Console.WriteLine("\n\n");
                    MakePost(id, body); //post a tag report
                    Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons 

                }


                if (DateTime.Now.DayOfWeek == System.DayOfWeek.Sunday ||
                    DateTime.Now.DayOfWeek == System.DayOfWeek.Monday ||
                    DateTime.Now.DayOfWeek == System.DayOfWeek.Friday)
                {
                    System.Console.WriteLine("Posting Shackbattles");
                    MakePost(id, "Upcomming SHACKBATTLES:\n\n" + GetUrl("http://shackbattl.es/external/ShackBattlesPost.aspx"));
                    Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons 
                }



                //thread summary post:
                body2 = "Largest thread: " + biggestThreadCount + " replies\n";
                body2 += PrintPost(biggestThread, -1, 200) + "\n\n\n";

                if (mostReplied != biggestThread)
                {
                    body2 += "Post with the most direct replies (" + mostReplied.ReplyCount + "):\n";
                    body2 += PrintPost(mostReplied, -1, 200) + "\n\n\n";
                }
                else
                {
                    body2 += "That post also has the most direct replies (" + mostReplied.ReplyCount + ")\n\n\n";
                }

                //most shack tags
                body2 += "Post with the most Shack Tags (" + mostTagsCount + "):\n";
                body2 += PrintPost(mostTags, -1, 200) + "\n\n\n";


                //chattiestThreads
                body2 += "Chattiest Threads (with 10 or more posts):\n";
                for(int c = 0;c<3 && c < threadChattyness.Count; c++)
                {
                    Post p = threadChattyness.Values[c];
                    body2 += String.Format("{0:0.0} words per post, started ", p.ThreadChattyness);
                    body2 += PrintPost(p, -1, 100) + "\n\n\n";
                } 
                body2 += "\n\n\n";

                //top users posts
                SortedList<Person, Person> users = new SortedList<Person, Person>(new PersonComparePost());
                {
                    
                    foreach (Person user in posters.Values)
                    {
                        users.Add(user, user);
                    }
                    int mostPosts = 25;
                    if (users.Count > 0)
                    {
                        mostPosts = users.Values[0].PostCount;
                    }
                    body2 += "\nTop Posters:\nTotal Posts, Root Posts, Replies, Characters, Name\n/{{";
                    for (int i = 0; i < 20 && i < users.Count; i++)
                    {
                        Person u = users.Values[i];
                        if (i < 10 || u.PostCount > (mostPosts * .6) || u.CharCount > 10000)
                        {
                            body2 += "" + (""+u.PostCount).PadLeft(3) + ", " + (""+(u.PostCount - u.ReplyCount)).PadLeft(2) + ", " +
                               (""+ u.ReplyCount).PadLeft(3) + ", " + (""+u.CharCount).PadLeft(5) + ", " + u.Name + "\r\n";
                        }
                    }
                    body2 += "}}/\n\n";

                    body2 += "Unique Posters: " + posterList.Count + "\n";
                }

                body2 += "\n\n\n" + GetEmojiReport() + "\n\n\n";

                //post time
                body2 += "Posts Per Hour:\nHour from start, root posts, replies, total posts\n/{{";
                for(int t = 0; t < maxHours; t++)
                {
                    if (postTime[0, t] + postTime[1, t] > 0)
                    {
                        body2 += "" + ("" + t).PadLeft(2) + ", " + ("" + postTime[0, t]).PadLeft(3) + ", " + ("" + postTime[1, t]).PadLeft(3) + ", " +
                            ("" + (postTime[0, t] + postTime[1, t])).PadLeft(3) +"\r\n";
                    }
                }
                body2 += "}}/\n";


                System.Console.WriteLine(body2);
                System.Console.WriteLine("\n\n");
                MakePost(id, body2);
                Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons 

                //Featured Articles / recent videos
                {

                    string mystring = GetFeaturedStories();
                    mystring += "\n\n\n\nComment or Suggestions for this thread?  Contact y{TroZ}y  Source: https://github.com/TroZ/ShacknewsSummaryPoster ";
                    System.Console.WriteLine(mystring);
                    System.Console.WriteLine("\n\n");
                    MakePost(id, mystring);
                }




                //reply to chart
                {
                    //first find longest length of 50 most posting shackers
                    System.Text.StringBuilder buf = new System.Text.StringBuilder();
                    buf.Append("User V  \\ Replied to>\n/{{");
                    int len = 0;
                    int maxusers = 32;
                    if (users.Count < maxusers) maxusers = users.Count;
                    for (int l = 0; l < maxusers; l++)
                    {
                        if (users.Values[l].Name.Length > len)
                        {
                            len = users.Values[l].Name.Length;
                        }
                    }
                    if (len > 20) len = 20;

                    string padding = new string(' ', len);
                    padding += "|";

                    //make column headers
                    for (int i = 0; i < len; i++)
                    {
                        buf.Append(padding);
                        for (int j = 0; j < maxusers + 1; j++)
                        {
                            string name = "<ROOT>";
                            if (j > 0)
                            {
                                name = users.Values[j - 1].Name;
                            }
                            if (len - i <= name.Length)
                            {
                                buf.Append(' ');
                                buf.Append(name[name.Length - (len-i)]);
                            }
                            else
                            {
                                buf.Append("  ");
                            }
                        }
                        buf.Append("\r\n");
                    }
                    buf.Append(padding);
                    buf.Append(new string('-', (maxusers+1) * 2));
                    buf.Append("\r\n");
                    //Make rows
                    for (int i = 0; i < maxusers; i++)
                    {
                        string name = users.Values[i].Name;
                        if (name.Length > 20) name = name.Substring(0, 20);
                        buf.Append(name.PadLeft(20));
                        buf.Append('|');

                        for (int j = 0; j < maxusers + 1; j++)
                        {
                            name = "";
                            if (j > 0)
                            {
                                name = users.Values[j - 1].Name;
                            }
                            name += "|" + users.Values[i].Name;
                            if (replyChart.ContainsKey(name))
                            {
                                int num = replyChart[name];
                                string val = "  ";
                                if (num > 0)
                                {
                                    if (num > 255 && num < 4096) //should never be above 4096
                                    {
                                        byte[] c = new byte[2];
                                        c[1] = (byte)(num & 0xf);
                                        c[0] = (byte)(num >> 8);
                                        
                                        val = "_[" + System.Convert.ToBase64String(c) + "]_";
                                    }
                                    else if (num > 99)
                                    {
                                        val = "/["+num.ToString("X")+"]/";
                                    }
                                    else
                                    {
                                        val = "" + num;
                                    }
                                    if (i + 1 == j)
                                    {
                                        val = "y{" + val.PadLeft(2) + "}y";
                                    }
                                    else
                                    {
                                        val = val.PadLeft(2);
                                    }
                                }
                                buf.Append(val);
                            }
                            else
                            {
                                buf.Append("  ");
                            }
                        }
                        buf.Append("\r\n");
                    }
                    buf.Append("}}/");

                    System.Console.WriteLine(buf.ToString());
                    System.Console.WriteLine("\n\n");
                }
            }

            System.Console.WriteLine("Done!");
        }

        private string GetEmojiReport()
        {
            string body = "Posts with most emoji:\nPost, Number of Emoji, Unique Emoji, Emojis\n/{{";
            for (int i = 0; i < 10 && i < posts.Count; i++)
            {
                Post p = posts.Values[i];
                body += "s[s[https://www.shacknews.com/chatty?id=" + p.Id + "#item_" + p.Id + "]s]s , " + (""+p.NumEmoji).PadLeft(2) + ", " + (""+p.UniqueEmoji).PadLeft(2) + ", " + ((p.Emojis.Length<42)?p.Emojis:p.Emojis.Substring(0,40)+"...") + "\r\n";
            }
            body += "}}/\n\nPosters using the most emoji:\nName, Number of Emoji, Unique Emoji, Emojis\n/{{";
            for (int i = 0; i < 10 && i < posters.Count; i++)
            {
                Person per = posters.Values[i];
                body += "" + (per.Name).PadRight(20) + ", " + (""+per.EmojiCount).PadLeft(2) + ", " + (""+per.UniqueEmoji).PadLeft(2) + ", " + ((per.Emojis.Length < 42) ? per.Emojis : per.Emojis.Substring(0, 40) + "...") + "\r\n";
            }
            body += "}}/\n";
            return body;
        }

        public static int GetNewRootPostId(string body, string username)
        {
            //this isn't a good way to do this. Only works for root posts (as that is all we request).
            //won't work if body has shacktags (in the first line of the post (although we try to work around that, assuming the start isn't in a tag))

            int pos = body.Length;
            int pos2 = body.IndexOf('\n');
            if (pos2 > -1)
            {
                pos = pos2;
            }
            //attempt to deal with shacktags, but less accurate
            pos2 = body.IndexOf("{");
            if(pos2 > -1 && pos2 < pos)
            {
                pos = pos2 - 1;
            }
            pos2 = body.IndexOf("[");
            if (pos2 > -1 && pos2 < pos)
            {
                pos = pos2 - 1;
            }

            string text = body.Substring(0, pos);


            for (int i = 0; i < 3; i++)
            {

                Thread.Sleep(10 * 1000);//wait 10 seconds for post to process
                dynamic root = GetJSON(Program.APIURL + "getChattyRootPosts?limit=30");

                if (root != null && root.rootPosts != null)
                {
                    foreach (dynamic post in root.rootPosts)
                    {
                        if (post != null && post.id != null)
                        {
                            int id = post.id;
                            string name = post.author;
                            string pbody = post.body;

                            if (name.ToLower().Equals(username.ToLower()) && pbody.StartsWith(text))
                            {
                                //this seems to be the post we are looking for
                                return id;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public static string PrintMultiLoler(SortedList<Post, Post> list, int num, int type)
        {
            string names = "";
            for (int i = 0; i < num && i < list.Count; i++)
            {
                for (int j = i+1; j < num && j < list.Count; j++)
                {
                    if(list.Values[i].Author == list.Values[j].Author)
                    {
                        if (!names.Contains(list.Values[i].Author))
                        {
                            if(names.Length > 0)
                            {
                                names += ", ";
                            }
                            names += list.Values[i].Author;
                        }
                    }
                }
            }
            if (names.Length > 0)
            {
                names = "\nMulti-" + PostCompareTag.GetTagName(type) + "er bonus for " + names + "!\n\n";
            }
            return names;
        }

        public static string PrintPostList(SortedList<Post, Post> list, int num, int type)
        {
            //makes a list of top <num> posts from the list <list> of tag type <type>
            string ret = "";
            for (int i = 0; i < num && i < list.Count; i++)
            {
                Post p = list.Values[i];
                ret += PrintPost(p, type);
                ret += "\n\n\n";
            }
            return ret;
        }

        public static string PrintPost(Post p, int type, int maxLen = 700)
        {
            //prints a single post, noteing the number of tages of type <type> it received
            string ret = "";
            if (type >= 0)
            {
                ret += PostCompareTag.GetTagCount(p, type) + " " + PostCompareTag.GetTagName(type) + "s ";
            }
            ret += "By y{" + p.Author + "}y s[s[https://www.shacknews.com/chatty?id=" + p.Id + "#item_" + p.Id + "]s]s\n";

            if (p.Nws || p.Text.ToLower().Contains("nws"))
            {
                ret += "r{!!!(Possible NWS Post detected!)!!!}r\n";
            }



            string text = p.Text;
            //strip link tags
            text = rxLink.Replace(text, "");
            //replace breaks
            text = text.Replace("<br>", "\n").Replace("<br />", "\n");

            //limit length
            bool trimmed = false;
            if (text.Length > (maxLen+25))
            {
                text = text.Substring(0, maxLen) ;
                trimmed = true;
            }

            ret += CleanUpPost(text);

            if (trimmed)
            {
                ret += " ....";
            }

            return ret;
        }

        public static string CleanUpPost(string text)
        {
            //the text of the posts we get are encoded in HTML, this 'converts' back to 'plain text' with shacktags
            string ret = text.Replace("<br>", "\n").Replace("<br />", "\n");
            //ret = rxTag.Replace(ret, "");  //removes html tags  - TODO improve to replace html tags with correct shack tags - Look at findtag function here: https://github.com/askedrelic/todayIs/blob/master/post.php 
            ret = FixShackTags(ret);
            //ret = EncodeEmoji(ret);
            return ret;
        }


        public static string FixShackTags(string comment)
        {
            //from https://github.com/askedrelic/todayIs/blob/master/post.php findtag
            //Apparently needs <br> or <br/> or <br /> already converted to \n

            //TODO: add code tag support
            string cmt = comment;
            int i = 0;
            Stack<string> stack = new Stack<string>();
            string ret = "";

            while (i < cmt.Length) {
                if (cmt[i] == '<') {
                    i++;
                    if (cmt[i] != '/')
                    {
                        string tagbody = cmt.Substring(i, (i+25<cmt.Length)?25:cmt.Length-i);
                        string[] token = tagbody.Split(" =\"<>".ToCharArray());
                        switch (token[0])
                        {
                            case "i":
                                ret += "/[";
                                stack.Push("]/");
                                break;
                            case "b":
                                ret += "*[";
                                stack.Push("]*");
                                break;
                            case "u":
                                ret += "_[";
                                stack.Push("]_");
                                break;
                            case "span":
                                if (token.Length > 3) { 
                                    string spanClass = token[3];//this should be the span's class
                                    switch (spanClass)
                                    {
                                        case "jt_blue":
                                            stack.Push("}b");
                                            ret += "b{";
                                            break;
                                        case "jt_red":
                                            stack.Push("}r");
                                            ret += "r{";
                                            break;
                                        case "jt_green":
                                            stack.Push("}g");
                                            ret += "g{";
                                            break;
                                        case "jt_yellow":
                                            stack.Push("}y");
                                            ret += "y{";
                                            break;
                                        case "jt_sample":
                                            stack.Push("]s");
                                            ret += "s[";
                                            break;
                                        case "jt_spoiler":
                                            stack.Push("]o");
                                            ret += "o[";
                                            break;
                                        case "jt_strike":
                                            stack.Push("]-");
                                            ret += "-[";
                                            break;
                                        case "jt_lime":
                                            stack.Push("]l");
                                            ret += "l[";
                                            break;
                                        case "jt_pink":
                                            stack.Push("]p");
                                            ret += "p[";
                                            break;
                                        case "jt_orange":
                                            stack.Push("]n");
                                            ret += "n[";
                                            break;
                                        case "jt_fuchsia": //this isn't really a tag, is it?
                                            stack.Push("]f");
                                            ret += "f[";
                                            break;
                                        case "jt_olive":
                                            stack.Push("]e");
                                            ret += "e[";
                                            break;
                                        case "jt_quote":
                                            stack.Push("]q");
                                            ret += "q[";
                                            break;
                                    }
                                }
                                else
                                {
                                    stack.Push(""); //found span without a class, add an empty tag to the stack so when we hit the end, we have something to pop (this sould never happen)
                                }
                                break;

                        }
                    }
                    else
                    {
                        // it's a /closing tag
                        i++;
                        string tagbody = cmt.Substring(i, (i + 10 < cmt.Length) ? 10 : cmt.Length - i);
                        string[] token = tagbody.Split(" =\"<>".ToCharArray());
                        switch (token[0])
                        {
                            case "b":
                                ret += "]*";
                                if (stack.Peek() == "]*") stack.Pop();
                                break;
                            case "i":
                                ret += "]/";
                                if (stack.Peek() == "]/") stack.Pop();
                                break;
                            case "u":
                                ret += "]_";
                                if (stack.Peek() == "]_") stack.Pop();
                                break;
                            case "span":
                                ret += stack.Pop();
                                break;
                        }
                    }
                    //fast foward to end of tag
                    int mark = i;
                    if (i < cmt.Length)
                    {
                        while (cmt[i++] != '>')
                        {
                            //ghetto code to stop unclosed < at of line
                            if (i >= cmt.Length)
                            {
                                //last tag is unclosed - ignore?
                                break;

                                //previous include last tag if unclosed code.
                                //ret += cmt[mark - 1];
                                //i = mark;
                                //break;
                            }
                        }
                    }
                }
                else
                {
                    ret += cmt[i];
                    i++;
                }
            }

            //close unclosed tags
            while(stack.Count > 0)
            {
                ret += stack.Pop();
            }

            return ret;
        }
    
        public static string GetPostPlainText(string text)
        {
            string ret = text.Replace("<br>", "\n").Replace("<br />", "\n");
            ret = rxTag.Replace(ret, "");
            return ret;
        }


        public static string EncodeEmoji(String text)
        {
            //not used - this was written that emoji would need to be converted to HTML Entities to be posted, but apparently the API expects UTF-8/16 
            //(not sure if the web request is doing a conversion) and handles the conversion
            string ret = "";

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsSurrogate(c) && i + 1 < text.Length)
                {
                    int codepoint = char.ConvertToUtf32(text, i);
                    ret += "&#x" + codepoint.ToString("X") + ";";
                    i++;
                }
                else
                {
                    ret += c;
                }
            }
            return ret;
        }


        public dynamic GetDayRootPosts(DateTime day)
        {
            //gets the root posts for the specified day (UTC)
            String date = "";

            //set to midnight (local time);
            //DateTime d = day.AddHours(-day.Hour);
            //d = d.AddMinutes(-d.Minute);
            //d = d.AddSeconds(-d.Second);
            DateTime utc = day.ToUniversalTime();//d.ToUniversalTime();

            utc = utc.AddHours(-utc.Hour);
            utc = utc.AddMinutes(-utc.Minute);
            utc = utc.AddSeconds(-utc.Second);

            //startTime = utc.AddMinutes(-(System.TimeZoneInfo.Local.GetUtcOffset(day).TotalMinutes)); //not sure where this comes from, is the date not really utc as the doc says?
            startTime = utc.AddHours(5);//not sure why this doesn't seem right, but adding 5 makes the earliest posts come out to be the same hour as startTime, so...
            DateTime test = startTime.ToLocalTime();
            Console.WriteLine("test time = " + test);

            utc = utc.AddDays(1); //adding 1 day due to it seeming that the date is the end date of the day requested, not the start date.

            //utc = utc.AddMinutes(TimeZoneInfo.Local.GetUtcOffset(d).TotalMinutes);
            //the time doesn't seem to matter, will alwayss return the posts for the 24 hours ending on that UTC day.

            date = utc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            date = System.Web.HttpUtility.UrlPathEncode(date);

            System.Console.WriteLine("Requesting posts for " + date);

            return GetJSON(Program.APIURL + "getChattyRootPosts?limit=1000&date=" + date);
        }

        public static dynamic GetJSON(string url)
        {
            //returns an object representing the JSON object returned from the provided URL
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using var streamReader = new StreamReader(httpResponse.GetResponseStream());
            var responseText = streamReader.ReadToEnd();

            return JsonConvert.DeserializeObject(responseText);
        }

        public static string GetUrl(string url)
        {
            //returns the content from th especified URL
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using var streamReader = new StreamReader(httpResponse.GetResponseStream());
            var responseText = streamReader.ReadToEnd();

            return responseText;
        }

        public static int MakePost(int parent, string body, int attempt = 1)
        {
            /*  Toggle comment - switch the beginning of this line between /* and //* (add or remove first /) to toggle function on or off
            return 1;
            /*/
             
            //posts <body> to Shacknews as a reply to post <parent> (or root if parent is 0)
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(Program.APIURL + "postComment");
            httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {

                string post = "username=" + System.Web.HttpUtility.UrlPathEncode(Program.USERNAME) + "&" +
                    "password=" + System.Web.HttpUtility.UrlPathEncode(Program.PASSWORD) + "&" +
                    "parentId=" + parent + "&" +
                    "text=" + System.Web.HttpUtility.UrlEncode(body);
                //System.Console.WriteLine(post);

                streamWriter.Write(post);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseText = streamReader.ReadToEnd();

                dynamic resp = JsonConvert.DeserializeObject(responseText);
                if (resp.error == null && resp.result == "success")
                {
                    //this doesn't necessarily return the id for the post just made as the post takes some time to process even after the post web request returns
                    //dynamic latest = GetJSON(Program.APIURL + "getNewestPostInfo");
                    //int id = latest.id;
                    return 1;// id; 
                }
                else
                {
                    System.Console.WriteLine(responseText);

                    if (attempt > 0 || responseText.Contains("ERR_POST_RATE_LIMIT"))
                    {
                        Thread.Sleep(120 * 1000); //hope it was PRL and retry after waiting
                       return MakePost(parent, body, 0);
                    }
                    else
                    {
                        throw new Exception("Error making post to " + Program.APIURL + ", result: " + responseText);
                    }
                }
            }

            //return 0;
            //*/
        }

        public void GetThreadRootTimes(dynamic root)
        {
            //finds the min and max times from the list of root posts provided
            if (root != null && root.rootPosts != null)
            {
                foreach (dynamic post in root.rootPosts)
                {
                    if (post != null && post.id != null)
                    {
                        int id = post.id;
                        string date = post.date;
                        DateTime postTime = post.date.Value;
                        //DateTime postTime = DateTime.Parse(date);
                        postTime = postTime.ToLocalTime();

                        if (postTime > maxPostDate)
                        {
                            maxPostDate = postTime;
                        }
                        if (postTime < minPostDate)
                        {
                            minPostDate = postTime;
                        }
                    }
                }
            }
        }

        public void GetThreads(dynamic root)
        {
            //if past a list of root posts, requests the full thread for each post and processses all the posts
            //it automatically delays 100ms between each request
            //it does skip the daily thread summary thread, so that it doesn't count emoji in the emoji summary post (this may cause it to skip some lol-tagged posts though)
            rootCount = 0;
            if (root != null && root.rootPosts != null)
            {
                foreach (dynamic post in root.rootPosts)
                {
                    if (post != null && post.id != null)
                    {
                        int id = post.id;
                        string text = post.body;
                        if (!text.StartsWith(SUMMARY_THREAD_START)) //don't count previous day's summary thread, as this would recount all the emoji from the day before
                        {

                            Thread.Sleep(100);
                            GetThread(id);

                            rootCount++;
                            if (rootCount % 10 == 0)
                            {
                                System.Console.WriteLine("Thread " + rootCount);
                            }
                        }
                    }
                }
            }
        }

        public void GetThread(int id)
        {
            //gets all the posts for the thread with post <id> thread and parses the posts
            string url = Program.APIURL + "getThread?id=" + id;
            dynamic thread = GetJSON(url);

            //System.Console.WriteLine(thread.ToString());
            if (thread != null)
            {
                if (thread.threads != null && thread.threads[0].posts != null)
                {
                    foreach (dynamic post in thread.threads[0].posts)
                    {
                        if (post != null)
                        {
                            ParsePost(post);
                        }
                    }
                }
            }

            //calc thread stats
            if(currentThread.Count > biggestThreadCount)
            {
                biggestThreadCount = currentThread.Count;
                biggestThread = currentThread[id];
            }
            //calc num replies
            foreach(Post child in currentThread.Values)
            {
                if(child.ParentId != 0)
                {
                    Post parent = currentThread[child.ParentId];
                    if(parent != null)
                    {
                        parent.ReplyCount++;
                    }
                }
            }
            //set max replies
            foreach (Post post in currentThread.Values)
            {
                if(mostReplied == null || post.ReplyCount > mostReplied.ReplyCount)
                {
                    mostReplied = post;
                }
            }
            //chattyness
            int wordCount = 0;
            Post root = null;
            foreach (Post post in currentThread.Values)
            {
                if(post.ParentId == 0)
                {
                    root = post;
                }
                wordCount += post.WordCount;
            }
            if (root != null && currentThread.Count > 9)
            {
                root.ThreadChattyness = wordCount / (double)currentThread.Count;
                threadChattyness.Add(root,root);
            }


            //reply to table
            foreach(Post p in currentThread.Values)
            {
                int parent = p.ParentId;
                string parentName = "";
                if(parent > 0 && currentThread.ContainsKey(parent))
                {
                    parentName = currentThread[parent].Author;
                }

                string entry = parentName + "|" + p.Author;

                if (replyChart.ContainsKey(entry))
                {
                    replyChart[entry]++;
                }
                else
                {
                    replyChart[entry] = 1;
                }
            }
                
            currentThread.Clear();
        }

        public void ParsePost(dynamic post)
        {
            //parses a post, adding it to the emoji list if it contains emoji, adding to to the lol-tag lists if it is tagged, and counting it's moderating type
            if (post != null)
            {
                string author = post.author;
                string body = post.body;
                int id = post.id;
                string date = post.date;
                int parent = post.parentId;
                string mod = post.category;

                if(parent != 0)
                {
                    replyCount++;
                }

                switch (mod.ToLower())
                {
                    case "ontopic": ontopicCount++; break;
                    case "nws": nwsCount++; break;
                    case "stupid": stupidCount++; break;
                    case "political": politicalCount++; break;
                    case "tangent": tangentCount++; break;
                    case "informative": interestingCount++; break;
                }

                Person p = GetPerson(author);
                p.PostCount++;
                if (parent != 0)
                {
                    p.ReplyCount++;
                }
                p.CharCount += body.Length;


                Post pt = new Post();
                pt.Author = p.Name;
                pt.Id = id;
                pt.PostDate = DateTime.Parse(date);
                pt.ParentId = parent;
                pt.Text = body;
                currentThread.Add(pt.Id, pt);

                int count;
                HashSet<string> emojis = new HashSet<string>();
                count = GetEmojis(body, emojis);
                pt.Emojis = String.Join("",emojis);
                pt.NumEmoji = count;
                pt.UniqueEmoji = emojis.Count;
                pt.ModCategory = mod;

                p.EmojiCount += pt.NumEmoji;

                if (pt.NumEmoji > 0)
                {
                    posts.Add(pt, pt);

                    //add emoji from post to person
                    p.addEmoji(emojis);
                }

                //do lols
                if (post.lols != null)
                {
                    foreach (dynamic lol in post.lols)
                    {
                        string type = lol.tag;
                        int lolnum = lol.count;
                        if (lolnum > 0)
                        {
                            switch (type.ToLower())
                            {
                                case "lol": pt.Tag_lol = lolnum; AddLol(postsLol, pt, PostCompareTag.TAG_LOL); lolCount += lolnum; break;
                                case "inf": pt.Tag_inf = lolnum; AddLol(postsInf, pt, PostCompareTag.TAG_INF); infCount += lolnum; break;
                                case "unf": pt.Tag_unf = lolnum; AddLol(postsUnf, pt, PostCompareTag.TAG_UNF); unfCount += lolnum; break;
                                case "tag": pt.Tag_tag = lolnum; AddLol(postsTag, pt, PostCompareTag.TAG_TAG); tagCount += lolnum; break;
                                case "wow": pt.Tag_wow = lolnum; AddLol(postsWow, pt, PostCompareTag.TAG_WOW); wowCount += lolnum; break;
                                case "aww": pt.Tag_aww = lolnum; AddLol(postsAww, pt, PostCompareTag.TAG_AWW); awwCount += lolnum; break;
                                case "wtf": pt.Tag_wtf = lolnum; AddLol(postsWtf, pt, PostCompareTag.TAG_WTF); wtfCount += lolnum; break;
                            }
                        }
                    }
                }
                //person lols
                p.Tag_lol_recv_count += pt.Tag_lol;
                p.Tag_inf_recv_count += pt.Tag_inf;
                p.Tag_unf_recv_count += pt.Tag_unf;
                p.Tag_tag_recv_count += pt.Tag_tag;
                p.Tag_wow_recv_count += pt.Tag_wow;
                p.Tag_aww_recv_count += pt.Tag_aww;
                p.Tag_wtf_recv_count += pt.Tag_wtf;

                //do mod categories
                if (pt.ModCategory != "ontopic") //ignore ontopic, as that is the default which most posts are tagged
                {
                    switch (pt.ModCategory)
                    {
                        case "informative": postsModInfo.Add(pt, pt); break;
                        case "tangent": postsModTang.Add(pt, pt); break;
                        case "stupid": postsModStpd.Add(pt, pt); break;
                        case "political": postsModPol.Add(pt, pt); break;
                        case "nws": postsModNws.Add(pt, pt); break;
                    }
                }

                int shacktags = CountShackTags(pt.Text);
                if(shacktags > mostTagsCount)
                {
                    mostTags = pt;
                    mostTagsCount = shacktags;
                }


                //add to date array
                bool root = parent == 0;
                DateTime postT = pt.PostDate;//.ToLocalTime();//.ToUniversalTime();  already UTC?
                int hour = (int)(postT - startTime).TotalHours;
                if(hour < maxHours && hour >= 0)
                {
                    postTime[root ? 0 : 1, hour]++;
                }
                else
                {
                    Console.WriteLine("this shouldn't happen"); //but could if a post was made after the end of the reporting period (and we are also running well after the reporting period)
                }

                //count words, ignore urls
                string text = body.Trim();
                text = text.Replace("<br>", "\n").Replace("<br />", "\n");
                text = rxFullLink.Replace(text, "");

                pt.WordCount = CountWords(text);
            }
        }

        public static int CountWords(string text)
        {
            int wordCount = 0, index = 0;

            // skip whitespace until first word
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;

            while (index < text.Length)
            {
                // check if current char is part of a word
                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                    index++;

                wordCount++;

                // skip whitespace until next word
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;
            }

            return wordCount;
        }

        public static int CountShackTags(string text)
        {
            //count span tags, as they are the style changes (except for <i>, <b> and <u>)
            int count = 0, n = 0;
            string substring = "<span";

            while ((n = text.IndexOf(substring, n, StringComparison.InvariantCulture)) != -1)
            {
                n += substring.Length;
                ++count;
            }

            substring = "<i>";
            n = 0;
            while ((n = text.IndexOf(substring, n, StringComparison.InvariantCulture)) != -1)
            {
                n += substring.Length;
                ++count;
            }

            substring = "<b>";
            n = 0;
            while ((n = text.IndexOf(substring, n, StringComparison.InvariantCulture)) != -1)
            {
                n += substring.Length;
                ++count;
            }

            substring = "<u>";
            n = 0;
            while ((n = text.IndexOf(substring, n, StringComparison.InvariantCulture)) != -1)
            {
                n += substring.Length;
                ++count;
            }

            return count;
        }

        public static void AddLol(SortedList<Post, Post> list, Post post, int type)
        {
            //adds a post to a lol-tag list if it should be added (it is one of the top five so far for that list)
            if (list.Count < 5)
            {
                list.Add(post, post);
            }
            else if (PostCompareTag.GetTagCount(list.Values[4], type) < PostCompareTag.GetTagCount(post, type))
            {
                list.Add(post, post);
            }
        }

        public static int GetEmojis(string text, HashSet<string> emojis )
        {
            //returns a string of unique emojis, a total count and a unique count for a specified text
            int count = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                String em = "";
                if (c > 0x2100 && c < 0x2c00) //some characters in this range are emoji (snowman, umbrella), so count them all.
                {
                    em += c;
                }
                else if (char.IsSurrogate(c) && i + 1 < text.Length && char.IsSurrogate(text[i + 1]))//non-basic multilingual plain character - count them all as emoji
                {
                    em += "" + c + text[i + 1];
                    i++;
                }

                if (em.Length > 0) {
                    //check for additional codepoints that should be included (zero width joiner, fitzpatrick, emoji varation selector)
                    bool added = true;
                    while (added && i + 1 < text.Length)
                    {
                        added = false;
                        char n = text[i + 1];
                        char nn = ' ';
                        if (i + 2 < text.Length)
                        {
                            nn = text[i + 2];
                        }
                        if( n == 0xfe0f)
                        {
                            em += "" + n;
                            i++;
                            added = true;
                        }
                        else if (n == 0x200D) //zero width joiner
                        {
                            //zwj - include zwj and next character (or two if next is a surrogate)
                            if (char.IsSurrogate(nn) && i + 3 < text.Length && char.IsSurrogate(text[i + 3]))
                            {
                                em += "" + n + nn + text[i + 3];
                                i += 3;
                            }
                            else
                            {
                                em += "" + n + nn;
                                i += 2;
                            }
                            added = true;
                        } else if (n == 0xd83c && nn >= 0xdffb && nn <= 0xdfff) //fitzpatrick as surrogate pairs
                        {
                            em += "" + n + nn;
                            i += 2;
                            added = true;
                        }

                    }
                }


                if (em.Length > 0)
                {
                    count++;
                    emojis.Add(em);
                }
            }

            return count;

            /* this was written assuming the API return the emoji encoded as an html entity, but apparently this isn't the case.
            if (text.Contains("&#"))
            {
                System.Console.WriteLine("emoji?");
            }

            MatchCollection matches = rx.Matches(text);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string emoji = match.Value;
                    count++;
                    emoji = NormalizeEmoji(emoji);

                    if (!emojis.Contains(emoji))
                    {
                        emojis += emoji;
                        unique++;
                    }
                }
            }
            */
        }

        public static string NormalizeEmoji(string emoji)
        {
            //this was written assuming we were receiving emoji as html entites, which have a hex or decimal representation.
            //this function converts them all to hex (potentially smaller), for comparison purposes
            string ret = emoji.ToLower();
            if (ret[2] != 'x')
            {
                int pos = ret.IndexOf(';');
                string num = ret.Substring(2, pos - 2);
                int codepoint = int.Parse(num);
                ret = "&#x" + codepoint.ToString("X") + ";";
            }

            return ret;
        }

        public Person GetPerson(string name)
        {
            //gets a person from the poster list dictionary, or creates a new person object and adds it to the list if that person doesn't exist.
            if (posterList.ContainsKey(name))
            {
                return posterList[name];
            }
            Person p = new Person();
            p.Name = name;
             p.EmojiCount = 0;
            posterList.Add(name, p);
            return p;
        }


        public string GetFeaturedStories()
        {
            string text = "_[Featured Articles]_:\n";

            string url = "https://www.shacknews.com/topic/feature";
            string html = GetUrl(url);

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");

            var articles = htmlBody.SelectNodes("//article");
            for(int i=0;i<articles.Count;i++)
            {

                var article = articles[i];
                /* this always returned data for the first article, even if article.innerHtml showed different data
                var titleNode = article.SelectSingleNode("//h3");
                var urlNode = article.SelectSingleNode("//a[@class=\"article-title\"]");
                var descNode = article.SelectSingleNode("//div[@class=\"article-content\"]/p");
                var timeNode = article.SelectSingleNode("//time");
                */

                var descends = article.Descendants();

                HtmlAgilityPack.HtmlNode urlNode = null;
                HtmlAgilityPack.HtmlNode titleNode = null;
                HtmlAgilityPack.HtmlNode descNode = null;
                HtmlAgilityPack.HtmlNode timeNode = null;

                foreach (var node in descends)
                {
                    if(node.Name == "h3")
                    {
                        titleNode = node;
                    }else if(node.Name == "a" && node.Attributes["class"]!= null  && node.Attributes["class"].Value == "article-title")
                    {
                        urlNode = node;
                    }
                    else if (node.Name == "time")
                    {
                        timeNode = node;
                    }
                    else if (node.Name == "div" && node.Attributes["class"] != null && node.Attributes["class"].Value == "article-content")
                    {
                        var children = node.ChildNodes;
                        foreach(var child in children)
                        {
                            if(child.Name == "p")
                            {
                                descNode = child;
                                break;
                            }
                        }
                    }
                }

                
                if(titleNode!=null && urlNode!=null && descNode!=null && timeNode != null)
                {
                    var time = timeNode.Attributes["datetime"].Value;

                    DateTime dt = DateTime.Parse(time);
                    if(dt.AddDays(2) > DateTime.Now || i < 3)
                    {
                        //include article
                        text += titleNode.InnerText + " s[" + urlNode.Attributes["href"].Value + "]s\n";
                        text += descNode.InnerText + "\n\n\n";
                    }
                }
            }


            text += "\n_[Recent Videos]_:\n";

            url = "https://www.youtube.com/feeds/videos.xml?channel_id=UCLR08NT874M_Mpgg9vtkv-g";
            html = GetUrl(url);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(html);

            /*
            var entries = doc.GetElementsByTagName("entry");
            foreach(XmlNode entry in entries)
            {
                XmlNode pubtime = entry.SelectSingleNode("//published");

                DateTime ptime = DateTime.Parse(pubtime.InnerText);
                if(ptime.AddDays(3) > DateTime.Now)
                {
                    XmlNode linkNode = entry.SelectSingleNode("//link");
                    

                    XmlNode mediaNode = entry.SelectSingleNode("//media:group");
                    if (mediaNode != null)
                    {
                        XmlNode titleNode = mediaNode.SelectSingleNode("//media:title");

                        if (titleNode != null && linkNode != null) { 
                            string link = linkNode.Attributes["href"].Value;
                            string title = titleNode.InnerText;

                            text += title + "\n";
                            text += link + "\n\n\n";
                        }

                    }
                }
            }
            */
            var publishedNode = doc.GetElementsByTagName("published"); //there is a published node for the feed that we must skip
            var linkNode = doc.GetElementsByTagName("link");//and two link nodes that we must skip at the beginning of the feed
            var mtitleNode = doc.GetElementsByTagName("media:title");

            for (int i = 0; i < publishedNode.Count-1 && i < linkNode.Count-2 && i < mtitleNode.Count; i++)
            {
                DateTime ptime = DateTime.Parse(publishedNode[i+1].InnerText);
                if (ptime.AddDays(2) > DateTime.Now || i < 3)
                {
                    string link = linkNode[i+2].Attributes["href"].Value;
                    string title = mtitleNode[i].InnerText;

                    text += title + "\n";
                    text += link + "\n\n\n";
                }
            }

            return text;
        }


        public int GetTodayThreadId(string msgStart)
        {
            int id = -1;
            string start = msgStart.ToLower();

            dynamic threads = GetDayRootPosts(DateTime.Now);

            if (threads != null && threads.rootPosts != null)
            {
                foreach (dynamic post in threads.rootPosts)
                {
                    if (post != null && post.id != null)
                    {
                        int tid = post.id;
                        string text = post.body;

                        text = rxTag.Replace(text, "");  //removes html tags (shacktags)

                        if (text.ToLower().StartsWith(start))
                        {
                            id = tid;
                            break;
                        }
                    }
                }
            }

            return id;
        }

        public static string AddOrdinal(int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
            }

        }
    }

}
