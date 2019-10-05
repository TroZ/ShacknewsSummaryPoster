using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

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

        Hashtable emojis = new Hashtable();
        SortedList<Post, Post> posts = new SortedList<Post, Post>(new PostCompare());
        SortedList<Person, Person> posters = new SortedList<Person, Person>(new PersonCompare());
        Dictionary<string, Person> posterList = new Dictionary<string, Person>();

        DateTime minPostDate = DateTime.Now;
        DateTime maxPostDate = new DateTime();

        SortedList<Post, Post> postsLol = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_LOL));
        SortedList<Post, Post> postsInf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_INF));
        SortedList<Post, Post> postsUnf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_UNF));
        SortedList<Post, Post> postsTag = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_TAG));
        SortedList<Post, Post> postsWow = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WOW));
        SortedList<Post, Post> postsAww = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_AWW));
        SortedList<Post, Post> postsWtf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WTF));

        //static Regex rx = new Regex(@"&#x?[a-fA-F0-9]{2,6};", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static Regex rxTag = new Regex(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        int rootCount = 0;
        int replyCount = 0;

        int ontopicCount = 0;
        int nwsCount = 0;
        int stupidCount = 0;
        int politicalCount = 0;
        int tangentCount = 0;
        int informativeCount = 0;

        int lolCount = 0;
        int infCount = 0;
        int unfCount = 0;
        int tagCount = 0;
        int wowCount = 0;
        int awwCount = 0;
        int wtfCount = 0;

        public ShackPostReport()
        {
            DateTime now = DateTime.Now;
            DateTime yesterday = now.AddDays(-1);

            //int id = MakePost(0, "Test post, please ignore.");

            //System.Console.WriteLine("post id: " + id);


            dynamic response = GetDayRootPosts(yesterday);

            //System.Console.WriteLine(response.ToString());

            SortedList<Post, Post>[] lists = new SortedList<Post, Post>[7];
            lists[0] = postsLol;
            lists[1] = postsInf;
            lists[2] = postsUnf;
            lists[3] = postsTag;
            lists[4] = postsWow;
            lists[5] = postsAww;
            lists[6] = postsWtf;

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

            string bodyParent = SUMMARY_THREAD_START + yesterday.ToLongDateString() + " s[Roots posts from " +
                minPostDate.ToShortDateString() + " " + minPostDate.ToShortTimeString() + " to " +
                maxPostDate.ToShortDateString() + " " + maxPostDate.ToShortTimeString() + " " + System.TimeZoneInfo.Local.StandardName +
                "]s\n"+rootCount+" Root Posts, "+replyCount+" Replies, "+
                informativeCount+" informative posts, "+ontopicCount+" ontpoic posts, "+tangentCount+" tangent posts, "+
                stupidCount+" stupid posts, "+politicalCount+" political posts, "+nwsCount+" nws posts.\n"+
                lolCount + " LOL tags, " + infCount + " INF tags, " + unfCount + " UNF tags, " + tagCount + " TAG tags, " + 
                wowCount + " WOW tags, " + awwCount + " AWW tags, " + wtfCount + " WTF tags.\n" +
                "In reply:";
            string body = "Posts with most emoji:\nPost, Number of Emoji, Unique Emoji, Emojis\n";
            for (int i = 0; i < 10 && i < posts.Count; i++)
            {
                Post p = posts.Values[i];
                body += "s[s[https://www.shacknews.com/chatty?id=" + p.Id + "#item_" + p.Id + "]s]s , " + p.NumEmoji + " , " + p.UniqueEmoji + " , " + p.Emojis + "\n";
            }
            body += "\nPosters using the most emoji:\nName, Number of Emoji, Unique Emoji, Emojis\n";
            for (int i = 0; i < 10 && i < posters.Count; i++)
            {
                Person per = posters.Values[i];
                body += "" + per.Name + " , " + per.EmojiCount + " , " + per.UniqueEmoji + " , " + per.Emojis + "\n";
            }
            //body = EncodeEmoji(body);

            System.Console.WriteLine(bodyParent);
            System.Console.WriteLine("\n\n");


            int id = MakePost(0, bodyParent);
            id = getNewRootPostId(bodyParent);
            if (id > 0)
            {

                System.Console.WriteLine(body);
                System.Console.WriteLine("\n\n");

                MakePost(id, body);//post emoji report

                for (int i = 0; i < PostCompareTag.TAG_MAX; i++)
                {
                    Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons 
                    System.Console.WriteLine("Posting type " + PostCompareTag.getTagName(i));

                    int count = 3;
                    if (i < PostCompareTag.TAG_WOW && i != PostCompareTag.TAG_UNF)
                    {
                        count = 5;
                    }

                    if (count > lists[i].Count)
                    {
                        count = lists[i].Count;
                    }

                    body = "Top " + count + " " + PostCompareTag.getTagName(i) + "s:\n\n";
                    body += PrintPostList(lists[i], count, i);

                    System.Console.WriteLine(body);
                    System.Console.WriteLine("\n\n");
                    MakePost(id, body); //post a tag report

                }


                Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons 
                System.Console.WriteLine("Posting Shackbattles");
                MakePost(id, "Upcomming SHACKBATTLES:\n\n"+GetUrl("http://shackbattl.es/external/ShackBattlesPost.aspx"));

            }

            System.Console.WriteLine("Done!");
        }


        public static int getNewRootPostId(string body)
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
                            String pbody = post.body;

                            if (pbody.StartsWith(text))
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

        public static string PrintPost(Post p, int type)
        {
            //prints a single post, noteing the number of tages of type <type> it received
            string ret = "";
            if (type >= 0)
            {
                ret += PostCompareTag.getTag(p, type) + " " + PostCompareTag.getTagName(type) + "s ";
            }
            ret += "By y{" + p.Author + "}y s[s[https://www.shacknews.com/chatty?id=" + p.Id + "#item_" + p.Id + "]s]s\n";

            if (p.Nws || p.Text.ToLower().Contains("nws"))
            {
                ret += "r{!!!(Possible NWS Post detected!)!!!}r\n";
            }

            string text = p.Text;
            if (text.Length > 700)
            {
                text = text.Substring(0, 640) + "..."; //640 should be enough for anyone...
            }

            ret += CleanUpPost(text);
            return ret;
        }

        public static string CleanUpPost(string text)
        {
            //the text of the posts we get are encoded in HTML, this 'converts' back to 'plain text'
            //this currently strips color / styles (shacktags)
            string ret = text.Replace("<br>", "\n").Replace("<br />", "\n");
            ret = rxTag.Replace(ret, "");  //removes html tags  - TODO improve to replace html tags with correct shack tags - Look at findtag function here: https://github.com/askedrelic/todayIs/blob/master/post.php 
            //ret = EncodeEmoji(ret);
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


        public static dynamic GetDayRootPosts(DateTime day)
        {
            //gets the root posts for the specified day (UTC)
            String date = "";
            DateTime utc = day.ToUniversalTime();
            utc = utc.AddDays(1); //adding 1 day due to it seeming that the date is the end date of the day requested, not the start date.
            utc = utc.AddHours(-utc.Hour);
            utc = utc.AddMinutes(-utc.Minute);
            utc = utc.AddSeconds(-utc.Second);

            date = utc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            date = System.Web.HttpUtility.UrlPathEncode(date);
            return GetJSON(Program.APIURL + "getChattyRootPosts?limit=1000&date=" + date);
        }

        public static dynamic GetJSON(string url)
        {
            //returns an object representing the JSON object returned from the provided URL
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseText = streamReader.ReadToEnd();

                return JsonConvert.DeserializeObject(responseText);

            }
        }

        public static string GetUrl(string url)
        {
            //returns the content from th especified URL
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseText = streamReader.ReadToEnd();

                return responseText;

            }
        }

        public static int MakePost(int parent, string body, int attempt = 1)
        {
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

                    if (attempt > 0)
                    {
                        Thread.Sleep(120 * 1000); //hope it was PRL and retry after waiting
                        MakePost(parent, body, 0);
                    }
                }
            }

            return 0;
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

                        if(postTime > maxPostDate)
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
                    case "informative": informativeCount++; break;
                }

                Person p = GetPerson(author);

                Post pt = new Post();
                pt.Author = p.Name;
                pt.Id = id;
                pt.PostDate = DateTime.Parse(date);
                pt.Text = body;

                string emojis;
                int count;
                int unique;
                GetEmojis(body, out emojis, out count, out unique);
                pt.Emojis = emojis;
                pt.NumEmoji = count;
                pt.UniqueEmoji = unique;

                if (post.category != null && post.category == "nws")
                {
                    pt.Nws = true;
                }

                p.EmojiCount += pt.NumEmoji;

                for (int i = 0; i < emojis.Length; i++)
                {
                    char c = emojis[i];
                    String em = "";
                    if (c > 0x2100 && c < 0x2c00)
                    {
                        em += c;
                    }
                    else if (char.IsSurrogate(c) && i + 1 < emojis.Length)
                    {
                        em += "" + c + emojis[i + 1];
                        i++;
                    }
                    if (em.Length > 0)
                    {
                        p.EmojiCount++;
                        if (!p.Emojis.Contains(em))
                        {
                            p.Emojis += em;
                            p.UniqueEmoji++;
                        }
                    }
                }


                if (pt.NumEmoji > 0)
                {
                    posts.Add(pt, pt);

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
            }
        }

        public static void AddLol(SortedList<Post, Post> list, Post post, int type)
        {
            //adds a post to a lol-tag list if it should be added (it is one of the top five so far for that list)
            if (list.Count < 5)
            {
                list.Add(post, post);
            }
            else if (PostCompareTag.getTag(list.Values[4], type) < PostCompareTag.getTag(post, type))
            {
                list.Add(post, post);
            }
        }

        public static void GetEmojis(string text, out string emojis, out int count, out int unique)
        {
            //returns a string of unique emojis, a total count and a unique count for a specified text
            emojis = "";
            count = 0;
            unique = 0;



            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                String em = "";
                if (c > 0x2100 && c < 0x2c00) //some characters in this range are emoji (snowman, umbrella), so count them all.
                {
                    em += c;
                }
                else if (char.IsSurrogate(c) && i + 1 < text.Length)//non-basic multilingual plain character - count them all as emoji
                {
                    em += "" + c + text[i + 1];
                    i++;
                }

                if (em.Length > 0)
                {
                    count++;
                    if (!emojis.Contains(em))
                    {
                        emojis += em;
                        unique++;
                    }
                }
            }

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
            p.Emojis = "";
            p.EmojiCount = 0;
            posterList.Add(name, p);
            return p;
        }
    }
}
