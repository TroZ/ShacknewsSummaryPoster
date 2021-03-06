﻿
using Newtonsoft.Json;
using PuppeteerSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;

/*
MIT License

Copyright (c) 2020 Brian Risinger

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

        const int PRL_DELAY = 1000; //60 * 1000; //Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons 

        PostDataList postDataList = new PostDataList(Program.logger, Program.POSTSFILE);


        DateTime startTime = DateTime.Now; //this gets replaced in GetThreadRootTimes
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


        int partialListMinSize = 10;
        readonly SortedList<Post, Post> threadChattyness = new SortedList<Post, Post>(new PostCompareChattyness());

        readonly Dictionary<string, int> replyChart = new Dictionary<string, int>();

        //Post biggestThread;
        //int biggestThreadCount=0;
        readonly SortedList<Post, Post> postsBiggestThread = new SortedList<Post, Post>(new PostCompareThreadSize());
        //Post mostReplied;
        readonly SortedList<Post, Post> postsMostReplies = new SortedList<Post, Post>(new PostCompareReplyCount());
        //Post mostTags;
        //int mostTagsCount;
        readonly SortedList<Post, Post> postsMostShacktags = new SortedList<Post, Post>(new PostCompareShacktags());
        readonly Dictionary<int, Post> currentThread = new Dictionary<int, Post>();

        readonly Dictionary<string, int> wordList = new Dictionary<string, int>();

        readonly Dictionary<Post, Post> threadPosts = new Dictionary<Post, Post>();

        readonly Dictionary<string, int> emojiPopularity = new Dictionary<string, int>();

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
       


        static int maxHours = 48; //normally one day of post then extra 18 hours for a root post at the end to time out 
        int[,] postTime = new int[2, maxHours];

        public ShackPostReport()
        {

            DateTime now = DateTime.Now;

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

            //dynamic response = GetDayRootPosts(yesterday); // old code - dead after 8/21/2020 Winchatty api change

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


            postDataList.Load();

            
            DateTime yesterday = DateTime.Now.AddDays(-1);
            yesterday = yesterday.AddHours(-yesterday.Hour);
            yesterday = yesterday.AddMinutes(-yesterday.Minute);
            yesterday = yesterday.AddSeconds(-yesterday.Second);
            //yesdterday should now be midnight of yesterday;
            
            List<PostData> threads = GetThreadRootTimes(yesterday);

            if (threads != null)
            {
                int c = threads.Count;
                System.Console.WriteLine("Thread Count: " + c);
            }

            GetThreads(threads); //this does most of the work, getting emoji counts, and also make the lists of top tagged posts

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
                        if(tid < 1)
                        {
                            tid = GetTodayThreadId("THE OFFICIAL NFL THREAD");
                        }
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
#if DEBUG
            id = 1;
#else
            id = GetNewRootPostId(bodyParent,Program.USERNAME);
#endif


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
                Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons 

                MakeTagPosts(id, tagLists);



                if (DateTime.Now.DayOfWeek == System.DayOfWeek.Sunday ||
                    DateTime.Now.DayOfWeek == System.DayOfWeek.Monday ||
                    DateTime.Now.DayOfWeek == System.DayOfWeek.Friday)
                {
                    System.Console.WriteLine("Posting Shackbattles");
                    try
                    {
                        MakePost(id, "Upcoming SHACKBATTLES:\n\n" + GetUrl("http://shackbattl.es/external/ShackBattlesPost.aspx"));
                    }catch (Exception ex)
                    {
                        Program.logger.LogError(ex, "Error when getting ShackBattles");
                    }
                    Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons 
                }



                //thread summary post:
                Post biggestThread = postsBiggestThread.Values[0];
                body2 = "Largest thread: " + biggestThread.ThreadSize + " replies\n";
                body2 += PrintPost(biggestThread, -1, 100) + "\n\n\n";

                Post mostReplied = postsMostReplies.Values[0];
                if (mostReplied != biggestThread)
                {
                    body2 += "Post with the most direct replies (" + mostReplied.ReplyCount + "):\n";
                    body2 += PrintPost(mostReplied, -1, 100) + "\n\n\n\n";
                }
                else
                {
                    body2 += "\nThat post also has the most direct replies (" + mostReplied.ReplyCount + ")\n\n\n\n";
                }

                //most shack tags
                Post mostShacktags = postsMostShacktags.Values[0];
                body2 += "Post with the most Shack Tags (" + mostShacktags.Shacktags + "):\n";
                body2 += PrintPost(mostShacktags, -1, 100) + "\n\n\n";


                //chattiestThreads
                body2 += "\nChattiest Threads (with 10 or more posts):\n";
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
                body2 += "Posts Per Hour:\nHour from start, root posts, replies\n/{{";
                for(int t = 0; t < maxHours; t++)
                {
                    if (postTime[0, t] + postTime[1, t] > 0)
                    {
                        body2 += "" + ("" + t).PadLeft(2) + ", " + ("" + postTime[0, t]).PadLeft(3) + ", " + ("" + postTime[1, t]).PadLeft(3) + "\r\n";
                    }
                }
                body2 += "}}/\n";


                body2 += "\n\n\nWords of the day: ("+ wordList.Count+" unique words)\n";
                SortedList<Word, Word> wordOrder = new SortedList<Word, Word>(new WordCompare());
                foreach(string word in wordList.Keys)
                {
                    if (!Program.CommonWords.ContainsKey(word))
                    {
                        Word w = new Word(word);
                        w.Count = wordList[word];
                        wordOrder.Add(w, w);
                    }
                }
                int totWords = 20;
                if(wordOrder.Count < 20)
                {
                    totWords = wordOrder.Count;
                }
                for(int i = 0; i < totWords; i++)
                {
                    if (i > 0)
                    {
                        body2 += ", ";
                    }
                    body2 += wordOrder.Values[i].WordString + "(" + wordOrder.Values[i].Count + ")";
                }


                System.Console.WriteLine(body2);
                System.Console.WriteLine("\n\n");
                MakePost(id, body2);
                Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons 

                //Featured Articles / recent videos
                {

                    string mystring = GetFeaturedStories();
                    mystring += "\n\n\n\nComment or Suggestions for this thread?  Contact y{TroZ}y  Source: https://github.com/TroZ/ShacknewsSummaryPoster ";
                    System.Console.WriteLine(mystring);
                    System.Console.WriteLine("\n\n");
                    MakePost(id, mystring);
                }


                
                if (now.Day == 1)
                {
                    //do month report
                    MakeMonthlyReport(id, yesterday, tagLists);

                }
                else if(now.DayOfWeek == System.DayOfWeek.Sunday)
                {
                    //do week reply to chart (if this is not the first of the month as we will do a month chart then)
                    MakeWeeklyChart(id, yesterday);
                }

            }

            System.Console.WriteLine("Done!");
        }

        private void MakeTagPosts(int id, SortedList<Post, Post>[] tagLists, bool month = false)
        {
            for (int tagType = 0; tagType < PostCompareTag.TAG_MAX; tagType++)
            {

                System.Console.WriteLine("Posting type " + PostCompareTag.GetTagName(tagType));

                int count = 3;
                int maxp = 10;
                int maxLen = 700;
                if (month)
                {
                    count = 10;
                    maxp = 25;
                    maxLen = 250;
                }

                int totalCount = 0;
                switch (tagType)
                {
                    default:
                    case PostCompareTag.TAG_LOL: totalCount = lolCount; break;
                    case PostCompareTag.TAG_INF: totalCount = infCount; break;
                    case PostCompareTag.TAG_UNF: totalCount = unfCount; break;
                    case PostCompareTag.TAG_TAG: totalCount = tagCount; break;
                    case PostCompareTag.TAG_WOW: totalCount = wowCount; break;
                    case PostCompareTag.TAG_AWW: totalCount = awwCount; break;
                    case PostCompareTag.TAG_WTF: totalCount = wtfCount; break;
                }

                
                if (tagType < PostCompareTag.TAG_WOW && tagType != PostCompareTag.TAG_UNF && count < 5)
                {
                    count = 5;
                }
                if (totalCount > 100 && count < 5)
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
                body += PrintPostList(tagLists[tagType], count, tagType, maxLen);

                body += "\n\n\n\n";
                //now add top person list
                SortedList<Person, Person> personTagged = new SortedList<Person, Person>(new PersonCompareTag(tagType));
                foreach (Person P in posterList.Values)
                {
                    if (PersonCompareTag.GetTagCount(P, tagType) > 0)
                    {
                        personTagged.Add(P, P);
                    }
                }
                
                if (personTagged.Count < maxp)
                {
                    maxp = personTagged.Count;
                }

                body += "Top " + maxp + " " + PersonCompareTag.GetTagName(tagType) + "'d posters:\n";
                int pp = 0;
                foreach (Person P in personTagged.Values)
                {
                    body += "" + PersonCompareTag.GetTagCount(P, tagType) + " - " + P.Name + "\n";
                    pp++;
                    if (pp >= maxp)
                    {
                        break;
                    }
                }



                System.Console.WriteLine(body);
                System.Console.WriteLine("\n\n");
                MakePost(id, body); //post a tag report
                Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons 

            }
        }

        private string GetEmojiReport(bool month = false)
        {
            int max = 10;
            int size = 15;
            if (month)
            {
                max = 25;
                size = 20;
            }
            string body = "Posts with most emoji:\nPost, Number of Emoji, Unique Emoji, Emojis\n/{{";
            for (int i = 0; i < max && i < posts.Count; i++)
            {
                Post p = posts.Values[i];
                body += "s[s[https://www.shacknews.com/chatty?id=" + p.Id + "#item_" + p.Id + "]s]s , " + (""+p.NumEmoji).PadLeft(2) + ", " + (""+p.UniqueEmoji).PadLeft(2) + ", " + GetFirstCodePoints(p.Emojis,10,true) + "\r\n";
            }
            body += "}}/\n\n\nPosters using the most emoji:\nName, Number of Emoji, Unique Emoji, Emojis\n/{{";
            for (int i = 0; i < max && i < posters.Count; i++)
            {
                Person per = posters.Values[i];
                body += "" + (per.Name).PadRight(20) + ", " + (""+per.EmojiCount).PadLeft(3) + ", " + (""+per.UniqueEmoji).PadLeft(3) + ", " + GetFirstCodePoints(per.Emojis, size, true) + "\r\n";
            }
            body += "}}/\n";
            return body;
        }


        private void MakeMonthlyReport(int rootId, DateTime yesterday, SortedList<Post, Post>[] tagLists)
        {
            int userListSize = 50;
            int postListSize = 10;

            System.Console.WriteLine("\nMaking report for the month\n");

            //gather data
            DateTime day = yesterday.AddDays(-1);
                       
            while (day.Month == yesterday.Month)
            {
                Thread.Sleep(10 * 1000); //sleep 10 seconds to give API a rest...

                System.Console.WriteLine("\nGetting data for: " + day);

                
                List<PostData> posts = GetThreadRootTimes(day);

                if (posts != null )
                {
                    int c = posts.Count;
                    System.Console.WriteLine("Thread Count: " + c);
                }

                GetThreads(posts); //this does most of the work, getting emoji counts, and also make the lists of top tagged posts

                day = day.AddDays(-1);
            } 


            posters.Clear();
            foreach (Person pp in posterList.Values)
            {
                posters.Add(pp, pp); //makes a sorted list by number of emojis
            }


            //make "root" post
            string body = SUMMARY_THREAD_START + "the month of _[";
            switch (yesterday.Month)
            {
                case 1: body += "⛄l[JANUARY]l⛄"; break;
                case 2: body += "💝p[FEBRUARY]p💝"; break;
                case 3: body += "☘g{MARCH}g☘"; break;
                case 4: body += "☔b{APRIL}b☔"; break;
                case 5: body += "🌸p[MAY]p🌸"; break;
                case 6: body += "🌞y{JUNE}y🌞"; break;
                case 7: body += "🍦r{JULY}r🍦"; break;
                case 8: body += "✎y{AUGUST}y✏️"; break;
                case 9: body += "🍎r{SEPTEMBER}r🍎"; break;
                case 10: body += "🎃n[OCTOBER]n🎃"; break;
                case 11: body += "🍂e[NOVEMBER]e🍂"; break;
                case 12: body += "❄️b{DECEMBER}b❄️"; break;
            }
            body += "]_\n";
            body += "b[" + rootCount + "]b _[e[Root Posts]e]_, b[" + replyCount + "]b /[l[Replies]l]/";
            if (interestingCount > 0)
            {
                body += ", b[" + interestingCount + "]b b{interesting}b posts";
            }
            //if (ontopicCount > 0) { //default is ontopic, so we don't really need to print the number
            //    bodyParent += ", " + ontopicCount + " ontopic posts";
            //}
            if (tangentCount > 0)
            {
                body += ", b[" + tangentCount + "]b y{tangent}y posts";
            }
            if (stupidCount > 0)
            {
                body += ", b[" + stupidCount + "]b g{stupid}g posts";
            }
            if (politicalCount > 0)
            {
                body += ", b[" + politicalCount + "]b n[political]n posts";
            }
            if (nwsCount > 0)
            {
                body += ", b[" + nwsCount + "]b r{nws}r posts";
            }
            body += "\ns[Roots posts from " +
                minPostDate.ToShortDateString() + " " + minPostDate.ToShortTimeString() + " to " +
                maxPostDate.ToShortDateString() + " " + maxPostDate.ToShortTimeString() + " " + System.TimeZoneInfo.Local.StandardName +
                ", summary threads not counted]s";
            body += "\nIn reply:";
            System.Console.WriteLine(body);
            System.Console.WriteLine("\n\n");
            int id = MakePost(rootId, body);
            Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons


            //Get reply ID
            id = GetNewThreadPostId(rootId, body, Program.USERNAME);

            if(id < 10)
            {
                //unable to find post, just reply to the day's post instead
                id = rootId;
            }

            //make child posts
            MakeTagPosts(id, tagLists, true);



            body = "Largest Threads:\n";
            for (int c = 0; c < postListSize && c < postsBiggestThread.Count; c++)
            {
                Post p = postsBiggestThread.Values[c];
                body += String.Format("{0} posts in thread ", p.ThreadSize);
                body += PrintPost(p, -1, 100) + "\n\n\n";
            }
            body += "\n\n";

            body += "Most Replies:\n";
            for (int c = 0; c < postListSize && c < postsMostReplies.Count; c++)
            {
                Post p = postsMostReplies.Values[c];
                body += String.Format("{0} replies ", p.ReplyCount);
                body += PrintPost(p, -1, 100) + "\n\n\n";
            }
            body += "\n\n";

            System.Console.WriteLine(body);
            System.Console.WriteLine("\n\n");
            MakePost(id, body);
            Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons

            body = "Chattiest Threads (with 10 or more posts):\n";
            for (int c = 0; c < postListSize && c < threadChattyness.Count; c++)
            {
                Post p = threadChattyness.Values[c];
                body += String.Format("{0:0.0} words per post, started ", p.ThreadChattyness);
                body += PrintPost(p, -1, 100) + "\n\n\n";
            }

            System.Console.WriteLine(body);
            System.Console.WriteLine("\n\n");
            MakePost(id, body);
            Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons


            //most shacktagged post
            body = "Post with the most Shack Tags:\n";
            for (int c = 0; c < postListSize && c < postsMostShacktags.Count; c++)
            {
                Post p = postsMostShacktags.Values[c];
                body += String.Format("{0} shacktags ", p.Shacktags);
                body += PrintPost(p, -1, 100) + "\n\n\n";
            }

            //most shacktag using people
            body += "\n\nUsers using the most Shacktags:\n/{{";
            {
                SortedList<Person, Person> usersShacktags = new SortedList<Person, Person>(new PersonCompareShacktags());
                foreach (Person user in posters.Values)
                {
                    if (user.ShacktagCount > 0)
                    {
                        usersShacktags.Add(user, user);
                    }
                }
                for (int i = 0; i < userListSize && i < usersShacktags.Count; i++)
                {
                    Person u = usersShacktags.Values[i];
                    body += "" + ("" + u.ShacktagCount).PadLeft(3) + " " + u.Name + "\r\n";
                }
                body += "}}/\n\n";
            }
            System.Console.WriteLine(body);
            System.Console.WriteLine("\n\n");
            MakePost(id, body);
            Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons



            //emoji & popularity
            {
                string emojibody = "Most Popular Emoji\nTimes Used, Emoji\n/{{";
                SortedList<Word, Word> emojiOrder = new SortedList<Word, Word>(new WordCompare());
                foreach (string word in emojiPopularity.Keys)
                {
                    Word w = new Word(word);
                    w.Count = emojiPopularity[word];
                    emojiOrder.Add(w, w);
                }
                int totEmoji = 25;
                if (emojiOrder.Count < 20)
                {
                    totEmoji = emojiOrder.Count;
                }
                for (int i = 0; i < totEmoji; i++)
                {
                    emojibody += ("" + emojiOrder.Values[i].Count).PadLeft(4) + ", " + emojiOrder.Values[i].WordString+"\n";
                }
                emojibody += "}}/";

                body = GetEmojiReport(true);
                //split the emoji post in two due to large size of encoded emoji
                string[] postBodies = body.Split("\n\n\n", 2, StringSplitOptions.RemoveEmptyEntries);

                if((postBodies[0].Length + emojibody.Length) < 2500)
                {
                    postBodies[0] += "\n\n" + emojibody;
                    emojibody = "";
                }

                System.Console.WriteLine(postBodies[0]);
                System.Console.WriteLine("\n\n");
                MakePost(id, postBodies[0]);
                Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons
                System.Console.WriteLine(postBodies[1]);
                System.Console.WriteLine("\n\n");
                MakePost(id, postBodies[1]);
                Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons

                if(emojibody.Length > 10)
                {
                    MakePost(id, emojibody);
                    Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons
                }
            }
            


            //top posters by total posts
            SortedList<Person, Person> users = new SortedList<Person, Person>(new PersonComparePost());
            SortedList<Person, Person> usersRoot = new SortedList<Person, Person>(new PersonCompareThreadStart());
            SortedList<Person, Person> usersChars = new SortedList<Person, Person>(new PersonCompareCharacters());
            foreach (Person user in posters.Values)
            {
                users.Add(user, user);
                usersChars.Add(user, user);
                if(user.RootPostCount > 0)
                {
                    usersRoot.Add(user, user);
                }
            }
            body = "Top Posters by total posts:\nTotal Posts, Root Posts, Replies, Name\n/{{";
            for (int i = 0; i < userListSize && i < users.Count; i++)
            {
                Person u = users.Values[i];
                body += "" + ("" + u.PostCount).PadLeft(3) + ", " + ("" + (u.RootPostCount)).PadLeft(3) + ", " +
                               ("" + u.ReplyCount).PadLeft(3) + ", " + u.Name + "\r\n";
            }
            body += "}}/\n\n";

            //unique
            body += "Unique Posters: " + posterList.Count + "\n\n\n";

            //top posters by thread starters
            body += "Top Posters by threads started:\nThreads started, # Posts in threads started, Name\n/{{";
            for (int i = 0; i < userListSize && i < usersRoot.Count; i++)
            {
                Person u = usersRoot.Values[i];
                body += "" + ("" + u.RootPostCount).PadLeft(3) + ", " + ("" + (u.TotalThreadSize)).PadLeft(5) + ", " +
                                u.Name + "\r\n";
            }
            body += "}}/\n\n";

            //top posters by characters
            body += "Top Posters by characters posted:\nCharacters, Name\n/{{";
            for (int i = 0; i < userListSize && i < usersChars.Count; i++)
            {
                Person u = usersChars.Values[i];
                body += "" + ("" + u.CharCount).PadLeft(6) + ", " + u.Name + "\r\n";
            }
            body += "}}/\n\n";

            System.Console.WriteLine(body);
            System.Console.WriteLine("\n\n");
            MakePost(id, body);
            Thread.Sleep(PRL_DELAY); //wait 60 seconds for PRL reasons



            //body = MakeReplyToChart(users, true);
            //System.Console.WriteLine(body);
            //System.Console.WriteLine("\n\n");
            //MakePost(id, body);
            //Thread.Sleep(60 * 1000); //wait 60 seconds for PRL reasons

            string filename = MakeReplyToChartImageAsync(users,true).Result;
            string url = UploadImage(filename);

            body = "Reply To Chart\n";
            body += url + "\n";


            body += "\n\n\nWords of the Week (" + wordList.Count + " unique words):\n";
            SortedList<Word, Word> wordOrder = new SortedList<Word, Word>(new WordCompare());
            foreach (string word in wordList.Keys)
            {
                if (!Program.CommonWords.ContainsKey(word))
                {
                    Word w = new Word(word);
                    w.Count = wordList[word];
                    wordOrder.Add(w, w);
                }
            }
            int totWords = 100;
            if (wordOrder.Count < totWords)
            {
                totWords = wordOrder.Count;
            }
            for (int i = 0; i < totWords; i++)
            {
                if (i > 0)
                {
                    body += ", ";
                }
                body += wordOrder.Values[i].WordString + "(" + wordOrder.Values[i].Count + ")";
            }


            //top tagged threads
            SortedList<Post, Post> threadsAll = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_MAX));
            SortedList<Post, Post> threadsLol = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_LOL));
            SortedList<Post, Post> threadsInf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_INF));
            SortedList<Post, Post> threadsUnf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_UNF));
            SortedList<Post, Post> threadsTag = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_TAG));
            SortedList<Post, Post> threadsWow = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WOW));
            SortedList<Post, Post> threadsAww = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_AWW));
            SortedList<Post, Post> threadsWtf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WTF));
            foreach (Post p in threadPosts.Values)
            {
                threadsAll.Add(p, p);
                threadsLol.Add(p, p);
                threadsInf.Add(p, p);
                threadsUnf.Add(p, p);
                threadsTag.Add(p, p);
                threadsWow.Add(p, p);
                threadsAww.Add(p, p);
                threadsWtf.Add(p, p);
            }
            body += "\n\n\n\nThreads with most total tags:\n";
            for (int c = 0; c < 3 && c < threadsAll.Count; c++)
            {
                Post p = threadsAll.Values[c];
                body += String.Format("{0} tags in thread ", PostCompareTag.GetTagCount(p, PostCompareTag.TAG_MAX));
                body += PrintPost(p, -1, 75) + "\n\n\n";
            }
            body += "\n\n";
            SortedList<Post, Post>[] threadLists = { threadsLol, threadsInf, threadsUnf, threadsTag, threadsWow, threadsAww, threadsWtf };
            for (int c = 0; c < threadLists.Length; c++)
            {
                body += "Thread with most " + PostCompareTag.GetTagName(c) + " tags:\n";
                if (threadLists[c].Count > 0)
                {
                    Post p = threadLists[c].Values[0];
                    body += String.Format("{0} {1} tags in thread ", PostCompareTag.GetTagCount(p, c), PostCompareTag.GetTagName(c));
                    body += PrintPost(p, -1, 75) + "\n\n\n\n";
                }
            }


            body += "\n\nImportant Linked Pages:\n";
            foreach (Word ww in wordOrder.Values)
            {
                String word = ww.WordString;
                if (word.StartsWith("http") && ww.Count > 1 && word.Length > 10 && body.Length + word.Length < 4900)
                {
                    body += "" + word + "\n";
                }
            }

            System.Console.WriteLine(body);
            System.Console.WriteLine("\n\n");
            MakePost(id, body);

        }

        private void MakeWeeklyChart(int rootId, DateTime yesterday)
        {
            //should be called on a Sunday to make a chart for previous Sunday to Saturday

            System.Console.WriteLine("\nMaking report for the week\n");

            //gather data
            DateTime day = yesterday.AddDays(-1);

            for(int i=0;i<6;i++)  //already have data for yesterday, need 6 more days for a week.
            {
                Thread.Sleep(10 * 1000); //sleep 10 seconds to give API a rest...

                System.Console.WriteLine("\nGetting data for: " + day);

                List<PostData> posts = GetThreadRootTimes(day);

                if (posts != null )
                {
                    int c = posts.Count;
                    System.Console.WriteLine("Thread Count: " + c);
                }

                GetThreads(posts); //this does most of the work, getting emoji counts, and also make the lists of top tagged posts

                day = day.AddDays(-1);
            }

            //make top posters list
            SortedList<Person, Person> users = new SortedList<Person, Person>(new PersonComparePost());
            foreach (Person user in posters.Values)
            {
                users.Add(user, user);
            }



            //string body = MakeReplyToChart(users);
            //System.Console.WriteLine(body);
            //System.Console.WriteLine("\n\n");
            //MakePost(rootId, body);

            string filename = MakeReplyToChartImageAsync(users).Result;
            string url = UploadImage(filename);

            string body = "";
            body = "This Week:\n\nReply To Chart\n";
            body += url + "\n"; //filename + "\n";


            body += "\n\n\nWords of the Week (" + wordList.Count + " unique words):\n";
            SortedList<Word, Word> wordOrder = new SortedList<Word, Word>(new WordCompare());
            foreach (string word in wordList.Keys)
            {
                if (!Program.CommonWords.ContainsKey(word))
                {
                    Word w = new Word(word);
                    w.Count = wordList[word];
                    wordOrder.Add(w, w);
                }
            }
            int totWords = 50;
            if (wordOrder.Count < totWords)
            {
                totWords = wordOrder.Count;
            }
            for (int i = 0; i < totWords; i++)
            {
                if (i > 0)
                {
                    body += ", ";
                }
                body += wordOrder.Values[i].WordString+"("+ wordOrder.Values[i].Count+")";
            }


            //top tagged threads
            SortedList<Post, Post> threadsAll = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_MAX));
            SortedList<Post, Post> threadsLol = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_LOL));
            SortedList<Post, Post> threadsInf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_INF));
            SortedList<Post, Post> threadsUnf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_UNF));
            SortedList<Post, Post> threadsTag = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_TAG));
            SortedList<Post, Post> threadsWow = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WOW));
            SortedList<Post, Post> threadsAww = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_AWW));
            SortedList<Post, Post> threadsWtf = new SortedList<Post, Post>(new PostCompareTag(PostCompareTag.TAG_WTF));
            foreach(Post p in threadPosts.Values)
            {
                threadsAll.Add(p, p);
                threadsLol.Add(p, p);
                threadsInf.Add(p, p);
                threadsUnf.Add(p, p);
                threadsTag.Add(p, p);
                threadsWow.Add(p, p);
                threadsAww.Add(p, p);
                threadsWtf.Add(p, p);
            }
            body += "\n\n\n\nThreads with most total tags:\n";
            for (int c = 0; c < 3 && c < threadsAll.Count; c++)
            {
                Post p = threadsAll.Values[c];
                body += String.Format("{0} tags in thread ", PostCompareTag.GetTagCount(p, PostCompareTag.TAG_MAX));
                body += PrintPost(p, -1, 75) + "\n\n\n";
            }
            body += "\n\n";
            SortedList<Post, Post>[] threadLists = { threadsLol, threadsInf, threadsUnf, threadsTag, threadsWow, threadsAww, threadsWtf };
            for(int c = 0; c < threadLists.Length; c++)
            {
                body += "Thread with most "+PostCompareTag.GetTagName(c)+" tags:\n";
                if(threadLists[c].Count > 0)
                {
                    Post p = threadLists[c].Values[0];
                    body += String.Format("{0} {1} tags in thread ", PostCompareTag.GetTagCount(p, c), PostCompareTag.GetTagName(c));
                    body += PrintPost(p, -1, 75) + "\n\n\n\n";
                }
            }


            body += "\n\nImportant Linked Pages:\n";
            foreach (Word ww in wordOrder.Values)
            {
                String word = ww.WordString;
                if (word.StartsWith("http") && ww.Count > 1 && word.Length > 10 && body.Length + word.Length < 4900 )
                {
                    body += "" + word + "\n";
                }
            }

            System.Console.WriteLine(body);
            System.Console.WriteLine("\n\n");
            MakePost(rootId, body);
        }

        private string MakeReplyToChart(SortedList<Person, Person> users, bool month = false)
        {
            //reply to chart - how the top 40 posters (by post count) replied to each other
            {
                //first find longest length of 50 most posting shackers
                System.Text.StringBuilder buf = new System.Text.StringBuilder();
                buf.Append("/{{");
                int len = 10;
                int maxusers = 40;
                /*
                if (users.Count < maxusers) maxusers = users.Count;
                for (int l = 0; l < maxusers; l++)
                {
                    if (users.Values[l].Name.Length > len)
                    {
                        len = users.Values[l].Name.Length;
                    }
                }
                */
                //limit it to 11 characters if more than that (the above probably isn't even needed as it will likely be above 11)
                if (len > 10) len = 10;

                string padding = new string(' ', len);
                padding += "|";

                //make column headers
                for (int i = 0; i < len; i++)
                {
                    if (i < 10)
                    {
                        if (i == 0)
                        {
                            buf.Append(" TOP USER".PadRight(len));
                        }
                        else if(i == 1)
                        {
                            buf.Append(" MESSAGES".PadRight(len));
                        }
                        else if (i == 2)
                        {
                            if (month)
                            {
                                buf.Append("THIS MONTH".PadRight(len));
                            }
                            else
                            {
                                buf.Append("THIS  WEEK".PadRight(len));
                            }
                        }
                        else if(i == 3)
                        {
                            buf.Append("User -->".PadLeft(len));
                        }
                        else if(i == 4)
                        {
                            buf.Append("Replied to".PadRight(len));
                        }
                        else if (i == 5)
                        {
                            buf.Append("below user".PadRight(len));
                        }
                        else if (i == 6)
                        {
                            buf.Append(" ".PadRight(len));
                        }
                        else if (i == 7)
                        {
                            buf.Append(">99 = _[HEX]_ ");
                        }
                        else if (i == 8)
                        {
                            buf.Append(">255_[/[Base64]/]_");
                        }
                        else if (i == 9)
                        {
                            buf.Append("y{Self Reply}y");
                        }
                        buf.Append("|");
                    }
                    else
                    {
                        buf.Append(padding);
                    }
                    for (int j = 0; j < maxusers; j++)
                    {
                        string name = users.Values[j].Name;
                        if (len <= name.Length)
                        {
                            buf.Append(' ');
                            buf.Append(name[ i ]);
                        }
                        else if( i >= len - name.Length)
                        {
                            buf.Append(' ');
                            buf.Append(name[i - (len - name.Length)]);
                        }
                        else
                        {
                            buf.Append("  ");
                        }
                    }
                    buf.Append("\r\n");
                }
                //separator
                buf.Append(padding);
                buf.Append(new string('-', (maxusers) * 2));
                buf.Append("\r\n");
                //Make rows
                for (int i = 0; i < maxusers + 1; i++)
                {
                    string name = "";
                    if (i == 0)
                    {
                        name = "NEW POST";
                    }
                    else
                    {
                        name = users.Values[i-1].Name;
                    }
                    if (name.Length > len) name = name.Substring(0, len);
                    buf.Append(name.PadLeft(len));
                    buf.Append('|');

                    for (int j = 0; j < maxusers ; j++)
                    {
                        name = "";
                        if (i > 0)
                        {
                            name = users.Values[i].Name;
                        }
                        name += "|" + users.Values[j].Name;
                        if (replyChart.ContainsKey(name))
                        {
                            int num = replyChart[name];
                            string val = "  ";
                            if (num > 0)
                            {
                                if(num > 4096)
                                {
                                    val = "_[**]_";
                                }
                                else if (num > 255 && num < 4096) //should never be above 4096
                                {
                                    byte[] c = new byte[2];
                                    c[1] = (byte)(num & 0xf);
                                    c[0] = (byte)(num >> 8);

                                    val = "_[/[" + Base64.b64ConvertInt(num,2) + "]/]_";
                                }
                                else if (num > 99)
                                {
                                    val = "_[" + num.ToString("X") + "]_";
                                }
                                else
                                {
                                    val = "" + num;
                                }

                                if (i == j +1)
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

                //try some cleanup
                int size = buf.Length;
                do
                {
                    size = buf.Length;
                    buf.Replace("  \r\n", "\r\n");
                } while (buf.Length < size);

/*
                if(buf.Length < 4950)
                {
                    buf.Append("\n >99 = _[HEX]_, > 255 = _[/[Base 64 Val]/]_");
                }
                else if(buf.Length < 4975)
                {
                    buf.Append("\n_[HEX]_ _[/[Base64]/]_");
                }
                if (buf.Length < 4980)
                {
                    buf.Append(", y{Reply to Self}y");
                }
*/
                if (buf.Length <= 4950)
                {
                    buf.Append("Values are 2 digits, Names are above 1s column.");
                }

                System.Console.WriteLine(buf.ToString());
                System.Console.WriteLine("\n\n");
                return buf.ToString();
            }
        }

        private async System.Threading.Tasks.Task<string> MakeReplyToChartImageAsync(SortedList<Person, Person> users, bool month = false)
        {
            //reply to chart - how the top 40 posters (by post count) replied to each other
            System.Text.StringBuilder buf = new System.Text.StringBuilder();
            
            int maxusers = 100;
            if (month)
            {
                maxusers = 250;
            }
            if(maxusers > users.Count)
            {
                maxusers = users.Count;
            }

            DateTime date = DateTime.Now;
            date = date.AddDays(-1);


            buf.Append("<http>\n<head>\n<title>Reply To Chart</title>\n");
            buf.Append("<style>\n");
            buf.Append("body{background: black;color: white;}\n");
            buf.Append("table{font-size: 12px;}\n");
            buf.Append(".head td{position: relative;min-width: 2em;text-align: left;}\n");
            //the translate here should really be 1.7em, 1px, but for some reason the browser we are using needs it to be bigger
            buf.Append(".head td div{writing-mode: vertical-rl;font-weight: bold;transform: translate(2.2em, 1px) rotate(200deg);top: 0;bottom: 0;position: absolute;}\n");
            buf.Append("th{text-align: right}\n");
            buf.Append(".head td.corner{text-align:right;height: 13em;width: 13em;vertical-align: bottom;}\n");
            buf.Append("td{text-align: center;}\n");
            buf.Append(".s55{background: #70ffff;}\n");
            buf.Append(".s35{background: #40ffff;}\n");
            buf.Append(".s20{background: #00ffff}\n");
            buf.Append(".s10{background: #00cccc}\n");
            buf.Append(".s5{background: #009999}\n");
            buf.Append(".s{background: #007777}\n");
            buf.Append(".r55{background: #ff70ff;}\n");
            buf.Append(".r35{background: #ff40ff;}\n");
            buf.Append(".r20{background: #ff00ff}\n");
            buf.Append(".r10{background: #cc00cc}\n");
            buf.Append(".r5{background: #990099}\n");
            buf.Append(".r{background: #770077}\n");
            buf.Append(".u55{background: #ff6060;}\n");
            buf.Append(".u35{background: #ff4040;}\n");
            buf.Append(".u20{background: #cc1010}\n");
            buf.Append(".u10{background: #990000}\n");
            buf.Append(".u5{background: #770000}\n");
            buf.Append(".o55{background: #40ff40;}\n");
            buf.Append(".o35{background: #00ff00;}\n");
            buf.Append(".o20{background: #00cc00}\n");
            buf.Append(".o10{background: #009900}\n");
            buf.Append(".o5{background: #007700}\n");
            buf.Append(".v0{color: white;}\n");
            buf.Append(".v1{color: #ddddff}\n");
            buf.Append(".v2{color: #bbbbff}\n");
            buf.Append(".v3{color: #9999ff}\n");
            buf.Append(".v4{color: #8585ff}\n");
            buf.Append(".v5{color: #7070ff}\n");
            buf.Append(".v6{color: #5555ff}\n");
            buf.Append(".v7{color: #4040ff}\n");
            buf.Append(".v8{color: #2525ff}\n");
            buf.Append(".v9{color: #1010ff}\n");
            buf.Append(".v10{color: #0000ee}\n");
            buf.Append("</style>\n");
            buf.Append("</head>\n<body>\n");

            buf.Append("<h2>Top User Posts for the ");
            if (month)
            {
                buf.Append("Month of ");
                buf.Append(date.ToString("yyyy-MM"));
            }
            else
            {
                buf.Append("Week of ");
                buf.Append(date.ToString("yyyy-MM-dd"));
            }
            buf.Append("</h2>\n");
            buf.Append("<table id=\"table\">\n");
            buf.Append("<tr class=head><td class=corner>User --><br>was replied to<br>by below user</td>\n");

            //make column headers
            buf.Append("<td><div>ROOT POST</div></td>\n");
            for (int j = 0; j < maxusers; j++)
            {
                string name = users.Values[j].Name;
                buf.Append("<td><div>");
                buf.Append(name);
                buf.Append("</div></td>\n");
            }
            buf.Append("\n");


            //Make rows
            for (int i = 0; i < maxusers ; i++)
            {
                string name = users.Values[i].Name;
                buf.Append("<tr><th>");
                buf.Append(name);
                buf.Append("</th>");

                for (int j = 0; j < maxusers+1; j++)
                {
                    name = "";
                    string oppo = users.Values[i].Name + "|";
                    if (j > 0)
                    {
                        name = users.Values[j-1].Name;
                        oppo += users.Values[j-1].Name;
                    }
                    name += "|" + users.Values[i].Name;
                    int num = 0;
                    if (replyChart.ContainsKey(name))
                    {
                        num = replyChart[name];
                    }
                    int diff = num;
                    if(j>0 && j-1!=i && replyChart.ContainsKey(oppo))
                    {
                        diff = num - replyChart[oppo];
                    }
                    if (num != 0 || diff < -4)
                    {
                        string cls = "";
                        if (j == 0)
                        {
                            //reply to root
                            cls += "r";
                        }
                        else if(j - 1 == i)
                        {
                            //reply to self
                            cls += "s";
                        }
                        else if(diff > 4)
                        {
                            //over opposite pair
                            cls += "o";
                        }
                        else if (diff < -4)
                        {
                            //under opposite pair
                            cls += "u";
                        }

                        if (cls.Length > 0 || num > 10)
                        {
                            if (diff >= 55 || diff <= -55)
                            {
                                cls += "55";
                            }
                            else if(diff >= 35 || diff <= -35)
                            {
                                cls += "35";
                            }
                            else if(diff >= 20 || diff <= -20)
                            {
                                cls += "20";
                            }
                            else if (diff >= 10 || diff <= -10)
                            {
                                cls += "10";
                            }
                            else if (diff >= 5 || diff <= -5)
                            {
                                cls += "5";
                            }

                            if (num > 10)
                            {
                                int v = (num-1) / 10;
                                if (v > 10) v = 10;
                                cls += " v" + v;
                            }

                            buf.Append("<td class=\"");
                            buf.Append(cls);
                            buf.Append("\">");
                            if (num != 0)
                            {
                                buf.Append("" + num);
                            }
                            buf.Append("</td>");
                        }
                        else
                        {
                            buf.Append("<td>");
                            buf.Append("" + num);
                            buf.Append("</td>");
                        }
                    }
                    else
                    {
                        buf.Append("<td></td>");
                    }

                }
                buf.Append("</tr>\n");
            }
            buf.Append("</table>\n");
            buf.Append("</body>\n");
            buf.Append("</html>\n");

            

            System.Console.WriteLine(buf.ToString());
            System.Console.WriteLine("\n\n");


            string filename = Directory.GetCurrentDirectory() + "\\" + "replychart";
            if (month)
            {
                filename += "month";
            }
            else
            {
                filename += "week";
            }

            string filename2 = filename + date.ToString("yyyyMMdd") + ".html";
            filename += date.ToString("yyyyMMdd")+".png";


            System.IO.File.WriteAllText(filename2, buf.ToString());


            /*
                        //Create a ThreadRunner object
                        ThreadRunner threadRunner = new ThreadRunner();
                        //Create a WebView through the ThreadRunner
                        WebView webView = threadRunner.CreateWebView();


                        threadRunner.Send(() =>
                        {
                            //webView.Resize(new System.Drawing.Size())

                            //Load Google's home page
                            webView.LoadHtmlAndWait(buf.ToString());

                            Object o;
                            o = webView.EvalScript("document.body.scrollWidth");
                            int width = int.Parse(o.ToString());
                            o = webView.EvalScript("document.body.scrollHeight");
                            int height = int.Parse(o.ToString());

                            webView.Resize(width+20, height+20);

                            //Capture screenshot and save it to a file
                            webView.Capture(new System.Drawing.Rectangle(0,0,width,height)).Save(filename, ImageFormat.Png);
                        });

                        webView.Dispose();
                        threadRunner.Stop();



            // * /
                        var th = new Thread(() => {
                            Microsoft.Toolkit.Wpf.UI.Controls.WebView br = new Microsoft.Toolkit.Wpf.UI.Controls.WebView();
                            //br.NavigationCompleted += browser_DocumentCompleted;
                            br.NavigateToString(buf.ToString());

                        });
                        th.SetApartmentState(ApartmentState.STA);
                        th.Start();
            //*/


            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });

            using (var page = await browser.NewPageAsync())
            {
                await page.SetContentAsync(buf.ToString());
                var result = await page.GetContentAsync();

                //var val = page.EvaluateFunctionAsync("document.body.scrollWidth");
                //int width = int.Parse(val.Result.ToString());
                //val = page.EvaluateFunctionAsync("document.body.scrollHeight");
                //int height = int.Parse(val.Result.ToString());
                int width = await page.EvaluateFunctionAsync<int>("()=> document.body.scrollWidth");
                int height = await page.EvaluateFunctionAsync<int>("()=> document.body.scrollHeight");

                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = width,
                    Height = height
                });

                await page.ScreenshotAsync(filename);
            }



            return filename;
            
        }

/*
        void browser_DocumentCompleted(Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlNavigationCompletedEventArgs e)
        {
            
        }
*/
        public static int GetNewRootPostId(string body, string username)
        {
            //this isn't a good way to do this. Only works for root posts (as that is all we request).
            //won't work if body has shacktags (in the first line of the post (although we try to work around that, assuming the start isn't in a tag))
            //well, we can attempt to reverse all shacktags now, but that isn't implemented here yet.

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

        public static int GetNewThreadPostId(int threadID, string body, string username)
        {
            //this isn't a good way to do this. 
            //won't work if body has shacktags (in the first line of the post (although we try to work around that, assuming the start isn't in a tag))
            //well, we can attempt to reverse all shacktags now, but that isn't implemented here yet.

            int pos = body.Length;
            int pos2 = body.IndexOf('\n');
            if (pos2 > -1)
            {
                pos = pos2;
            }
            //attempt to deal with shacktags, but less accurate
            pos2 = body.IndexOf("{");
            if (pos2 > -1 && pos2 < pos)
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

                string url = Program.APIURL + "getThread?id=" + threadID;

                try
                {
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
                }catch (Exception ex)
                {
                    Program.logger.LogError(ex, "Error Getting New Post ID"); 
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

        public static string PrintPostList(SortedList<Post, Post> list, int num, int type, int maxLen = 700)
        {
            //makes a list of top <num> posts from the list <list> of tag type <type>
            string ret = "";
            for (int i = 0; i < num && i < list.Count; i++)
            {
                Post p = list.Values[i];
                ret += PrintPost(p, type, maxLen);
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
                switch (type)
                {
                    default:
                        ret += PostCompareTag.GetTagCount(p, type) + " " + PostCompareTag.GetTagName(type) + "s ";
                        break;
                }
                
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
            int trimlen = maxLen;
            string result = "";
            do {
                string trimtext = text;
                if (text.Length > (maxLen + 25) && trimlen < text.Length)
                {
                    trimtext = text.Substring(0, trimlen);
                    trimmed = true;
                }
                else
                {
                    trimmed = false;
                }

                result = CleanUpPost(trimtext);

                trimlen += 25;//if we fid we trimmed too short, retry at this size
            } while (result.Length < maxLen - 25 && trimlen < text.Length);
            ret += result;

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
                if (cmt[i] == '<' && (i+1) < cmt.Length) {
                    i++;
                    bool skip = false;
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
                                        case "jt_wtf242"://custom grey text just for him
                                            break;
                                    }
                                }
                                else
                                {
                                    stack.Push(""); //found span without a class, add an empty tag to the stack so when we hit the end, we have something to pop (this should never happen)
                                }
                                break;
                            default:
                                ret += '<';
                                skip = true;
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
                                if (stack.Count > 0 && stack.Peek() == "]*") stack.Pop();
                                break;
                            case "i":
                                ret += "]/";
                                if (stack.Count > 0 && stack.Peek() == "]/") stack.Pop();
                                break;
                            case "u":
                                ret += "]_";
                                if (stack.Count > 0 && stack.Peek() == "]_") stack.Pop();
                                break;
                            case "span":
                                if (stack.Count > 0)
                                {
                                    ret += stack.Pop();
                                }
                                break;
                        }
                    }
                    //fast foward to end of tag
                    int mark = i;
                    if (i < cmt.Length && !skip)
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
                    if (cmt[i] != '<')
                    {
                        ret += cmt[i];
                    }
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
            //not used - this was written assuming that emoji would need to be converted to HTML Entities to be posted, but apparently the API expects UTF-8/16 
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

        // old code - dead after 8/21/2020 Winchatty api change
        /*
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
            startTime = utc.AddHours(5);//not sure why this doesn't seem right, but adding 5 makes the earliest posts come out to be the same hour as startTime, so...   Are the times Central Time based?
            if(day.IsDaylightSavingTime() == false)
            {
                startTime = utc.AddHours(6);
            }
            //DateTime test = startTime.ToLocalTime();
            //Console.WriteLine("test time = " + test);

            if (utc.Day == day.Day)
            {
                utc = utc.AddDays(1); //adding 1 day due to it seeming that the date is the end date of the day requested, not the start date.
            }

            //utc = utc.AddMinutes(TimeZoneInfo.Local.GetUtcOffset(d).TotalMinutes);
            //the time doesn't seem to matter, will always return the posts for the 24 hours ending on that UTC day.

            date = utc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            date = System.Web.HttpUtility.UrlPathEncode(date);

            System.Console.WriteLine("Requesting posts for " + date);

            return GetJSON(Program.APIURL + "getChattyRootPosts?limit=1000&date=" + date);
        }
        */

        public dynamic GetCurrentRootPosts()
        {
            System.Console.WriteLine("Requesting current root posts" );

            return GetJSON(Program.APIURL + "getChattyRootPosts?limit=1000");
        }

        public static dynamic GetJSON(string url)
        {
            int count = 0;
            while (count < 3)
            {
                try
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
                catch (System.Net.WebException e)
                {
                    System.Console.WriteLine(e);
                    if (count == 2)
                    {
                        throw e;
                    }
                    Thread.Sleep(10 * 1000);
                }
                count++;
            }
            return null;
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
#if DEBUG
            return 1;
#else
            //posts <body> to Shacknews as a reply to post <parent> (or root if parent is 0)

            int tries = 3;
            bool success = false;
            while(!success && tries > 0){
                try{
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
                                //return 1;//it turns out we don't really want to throw an exception. It's better to miss one post than to not do any of the later posts.
                                throw new Exception("Error making post to " + Program.APIURL + ", result: " + responseText);
                            }
                        }
                    }
                }catch (Exception ex)
                {
                    Program.logger.LogError(ex, "Error Making Post");
                }
                tries--;
                Thread.Sleep(2 * PRL_DELAY); 
            }
            return 0;
#endif

            //*/
        }


        /*
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
        */

        public List<PostData> GetThreadRootTimes(DateTime day)
        {
            List < PostData > pdlist = new List<PostData>();

            startTime = day;
            if(startTime.Hour > 0)
            {
                startTime.AddHours(-startTime.Hour);
            }
            if (startTime.Minute > 0)
            {
                startTime.AddHours(-startTime.Minute);
            }
            if (startTime.Second > 0)
            {
                startTime.AddSeconds(-startTime.Second);
            }
            if (startTime.Millisecond > 0)
            {
                startTime.AddMilliseconds(-startTime.Millisecond);
            }


            foreach (PostData pd in postDataList.postDataList.Values)
            {
                DateTime date = pd.postDate;
                if(date.Year == day.Year && date.Month == day.Month && date.Day == day.Day)
                {
                    pdlist.Add(pd);

                    if (pd.postDate > maxPostDate)
                    {
                        maxPostDate = pd.postDate;
                    }
                    if (pd.postDate < minPostDate)
                    {
                        minPostDate = pd.postDate;
                    }
                }
            }

            return pdlist;
        }

        public void GetThreads(List<PostData> threadList)
        {
            //if past a list of root posts, requests the full thread for each post and processses all the posts
            //it automatically delays 100ms between each request
            //it does skip the daily thread summary thread, so that it doesn't count emoji in the emoji summary post (this may cause it to skip some lol-tagged posts though)
           

            foreach (PostData pd in threadList)
            {
                if (pd != null && pd.summary == false)
                {
                    int id = pd.id;
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

        public void GetThread(int id)
        {
            //gets all the posts for the thread with post <id> thread and parses the posts
            string url = Program.APIURL + "getThread?id=" + id;
            dynamic thread = GetJSON(url);

            //System.Console.WriteLine(thread.ToString());
            if (thread != null)
            {
                if (thread.threads != null && thread.threads.Count > 0 && thread.threads[0].posts != null)
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

            int wordCount = 0;
            Post root = null;
            foreach (Post post in currentThread.Values)
            {
                if (post.ParentId == 0)
                {
                    root = post;
                }
                wordCount += post.WordCount;
            }

            if (root == null)
            {
                //nuked thread?
                return;
            }


            root.ThreadSize = currentThread.Count;
            //calc thread stats
            if (currentThread.Count > 0 && (postsBiggestThread.Count < partialListMinSize || postsBiggestThread.Values[partialListMinSize-1].ThreadSize < currentThread.Count))
            {
                postsBiggestThread.Add(root, root);
            }
            //add thread size to root author
            Person rootAuth = posterList[root.Author];
            if (rootAuth != null)
            {
                rootAuth.TotalThreadSize += root.ThreadSize;
            }
            else
            {
                Console.WriteLine("That shouldn't have happened!");
            }
            //calc num replies
            foreach (Post child in currentThread.Values)
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
                if(post.ReplyCount > 0 && (postsMostReplies.Count < partialListMinSize || postsMostReplies.Values[partialListMinSize-1].ReplyCount < post.ReplyCount))
                {
                    postsMostReplies.Add(post, post);
                }
            }
            //chattyness
            
            
            if (root != null && currentThread.Count > 9)
            {
                root.ThreadChattyness = wordCount / (double)currentThread.Count;
                if (threadChattyness.Count < partialListMinSize || root.ThreadChattyness > threadChattyness.Values[partialListMinSize-1].ThreadChattyness)
                {
                    threadChattyness.Add(root, root);
                }
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

            //thread summary
            Post threadPost = new Post();
            threadPost.Id = root.Id;
            threadPost.Author = root.Author;
            threadPost.Text = root.Text;
            foreach (Post p in currentThread.Values)
            {
                threadPost.Tag_lol += p.Tag_lol;
                threadPost.Tag_inf += p.Tag_inf;
                threadPost.Tag_unf += p.Tag_unf;
                threadPost.Tag_tag += p.Tag_tag;
                threadPost.Tag_wow += p.Tag_wow;
                threadPost.Tag_aww += p.Tag_aww;
                threadPost.Tag_wtf += p.Tag_wtf;
            }
            threadPosts.Add(threadPost, threadPost);


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

                if (pt.Text.Contains("&lt;") || pt.Text.Contains("&gt;"))
                {
                    pt.Text = HtmlDecode(pt.Text);
                }

                int count;
                HashSet<string> emojis = new HashSet<string>();
                count = GetEmojis(body, emojis, emojiPopularity);
                pt.Emojis = String.Join("",emojis);
                pt.NumEmoji = count;
                pt.UniqueEmoji = emojis.Count;
                pt.ModCategory = mod;

                p.EmojiCount += pt.NumEmoji;

                if (pt.NumEmoji > 0)
                {
                    posts.Add(pt, pt);

                    //add emoji from post to person
                    p.AddEmoji(emojis);
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

                pt.Shacktags = CountShackTags(pt.Text);
                if (pt.Shacktags > 0 && (postsMostShacktags.Count < partialListMinSize || postsMostShacktags.Values[partialListMinSize - 1].Shacktags < pt.Shacktags))
                {
                    postsMostShacktags.Add(pt, pt);
                }
                p.ShacktagCount += pt.Shacktags;


                //add to date array
                bool root = parent == 0;
                DateTime postT = pt.PostDate.ToLocalTime(); //We want to post date to be local time, so that any post from midnight to 1am appears as hour 0
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
                //text = rxFullLink.Replace(text, "");
                text = rxTag.Replace(text, "");

                pt.WordCount = CountWords(text);
            }
        }

        public int CountWords(string text)
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


            string[] words = text.Split(" \r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach(string word in words)
            {
                string w = word.Trim().ToLower();
                while (w.EndsWith('.') || w.EndsWith(',') || w.EndsWith('!') || w.EndsWith('?') || w.EndsWith(')') || w.EndsWith('}') || w.EndsWith(']') || w.EndsWith('\"') || w.EndsWith('\''))
                {
                    w = w.Substring(0, w.Length - 1);
                }
                while (w.StartsWith('\"') || w.StartsWith('\'') || w.StartsWith('(') || w.StartsWith('[') || w.StartsWith('{'))
                {
                    w = w.Substring(1, w.Length - 1);
                }
                if (w.Length > 3) {

                    if (wordList.ContainsKey(w))
                    {
                        wordList[w] = wordList[w] + 1;
                    }
                    else
                    {
                        wordList.Add(w, 1);
                    }
                }
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

        public void AddLol(SortedList<Post, Post> list, Post post, int type)
        {
            //adds a post to a lol-tag list if it should be added (it is one of the top five so far for that list)
            if (list.Count < partialListMinSize)
            {
                list.Add(post, post);
            }
            else if (PostCompareTag.GetTagCount(list.Values[partialListMinSize-1], type) < PostCompareTag.GetTagCount(post, type))
            {
                list.Add(post, post);
            }
        }

        public static int GetEmojis(string text, HashSet<string> emojis, Dictionary<string, int> ep )
        {
            //returns a string of unique emojis, a total count and a unique count for a specified text
            int count = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                String em = "";
                if (c >= 0x2100 && c < 0x2c00) //some characters in this range are emoji (snowman, umbrella), so count them all.
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

                    //add to popular emojis
                    if (ep.ContainsKey(em))
                    {
                        int val = ep.GetValueOrDefault(em);
                        ep.Remove(em);
                        ep.Add(em, val + 1);
                    }
                    else
                    {
                        ep.Add(em, 1);
                    }

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

        public static string GetFirstCodePoints(String text,int len, bool showTrunc = false)
        {
            //returns the first LEN codepoints from TEXT.  
            string ret = "";
            int count = 0;
            foreach(char c in text)
            {
                if (Char.IsHighSurrogate(c))
                {
                    //assume next character is a low surrogate, and don't count this one
                    ret += c;
                    continue;
                }
                if (Char.IsLowSurrogate(c) && !Char.IsHighSurrogate(ret[ret.Length -1]))
                {
                    //this shouldn't happen
                    continue;
                }
                ret += c;

                count++;
                if(count >= len)
                {
                    break;
                }
            }

            if (showTrunc)
            {
                if(ret.Length < text.Length)
                {
                    ret += "...";
                }
            }
            return ret;
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

            dynamic threads = GetCurrentRootPosts();

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

                        text = text.Replace("  ", " ");
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



        public static String UploadImage(string filename)
        {
            /*  Toggle comment - switch the beginning of this line between /* and //* (add or remove first /) to toggle function on or off
            return "";
            /*/

            //posts <body> to Shacknews as a reply to post <parent> (or root if parent is 0)
#if !DEBUG
            //string fileLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "somefile.jpg";
            NameValueCollection values = new NameValueCollection();
            NameValueCollection files = new NameValueCollection();
            values.Add("type", "direct");
            files.Add("userfile[]", filename);
            string html = SendFormRequest("http://chattypics.com/uploadtoskynet.php", values, files);

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            var input = htmlDoc.DocumentNode.SelectSingleNode("//input[@id='link11']");
            if(input != null)
            {
                return input.GetAttributeValue("value", "");
            }
#endif

            return "Upload Failed";

            //*/
        }

        private static string SendFormRequest(string url, NameValueCollection values, NameValueCollection files = null)
        {
            string boundary = "----------------ShackPostReport" + DateTime.Now.Ticks.ToString("x");
            // The first boundary
            byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
            // The last boundary
            byte[] trailer = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            // The first time it itereates, we need to make sure it doesn't put too many new paragraphs down or it completely messes up poor webbrick
            byte[] boundaryBytesF = System.Text.Encoding.ASCII.GetBytes("--" + boundary + "\r\n");

            // Create the request and set parameters
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            request.Credentials = System.Net.CredentialCache.DefaultCredentials;

            // Get request stream
            Stream requestStream = request.GetRequestStream();

            foreach (string key in values.Keys)
            {
                // Write item to stream
                byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}", key, values[key]));
                requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                requestStream.Write(formItemBytes, 0, formItemBytes.Length);
            }

            if (files != null)
            {
                foreach (string key in files.Keys)
                {
                    if (File.Exists(files[key]))
                    {
                        int bytesRead = 0;
                        byte[] buffer = new byte[2048];
                        byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n", key, files[key]));
                        requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                        requestStream.Write(formItemBytes, 0, formItemBytes.Length);

                        using (FileStream fileStream = new FileStream(files[key], FileMode.Open, FileAccess.Read))
                        {
                            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                // Write file content to stream, byte by byte
                                requestStream.Write(buffer, 0, bytesRead);
                            }

                            fileStream.Close();
                        }
                    }
                }
            }

            // Write trailer and close stream
            requestStream.Write(trailer, 0, trailer.Length);
            requestStream.Close();

            using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            };
        }

        public string HtmlDecode(string text)
        {
            text = text.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&apos;", "'").Replace("&quot;", "\"");
            return text;
        }
    }


}
