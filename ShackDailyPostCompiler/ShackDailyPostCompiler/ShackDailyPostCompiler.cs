using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
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
namespace ShackDailyPostCompiler
{
    class ShackDailyPostCompiler
    {
        const string SUMMARY_THREAD_START = "Shack Post Report for ";

        PostDataList postDataList = new PostDataList(Program.logger,Program.POSTSFILE);


        public ShackDailyPostCompiler()
        {
            //load current list
            postDataList.Load();


            //get posts from last 12(ish) hours
            GetPosts();


            //save list
            postDataList.Save();
        }


        public void GetPosts()
        {
            //this will get the current root posts on the Shack - 18 hours worth (post lifetime on Shacknews.com)
            //so if we run this routine every 12 hours, we won't miss a post.
            dynamic root = GetJSON(Program.APIURL + "getChattyRootPosts?limit=1000");

            foreach (dynamic post in root.rootPosts)
            {
                if (post != null && post.id != null)
                {
                    int id = post.id;
                    string date = post.date;
                    bool summary = false;
                    DateTime postTime = post.date.Value;
                    //DateTime postTime = DateTime.Parse(date);
                    postTime = postTime.ToLocalTime();

                    string text = post.body;
                    if (text.StartsWith(SUMMARY_THREAD_START)) //don't count previous day's summary thread, as this would recount all the emoji from the day before
                    {
                        summary = true;
                    }

                    PostData pd = new PostData(id, postTime);
                    pd.summary = summary;

                    if (!postDataList.ContainsKey(id))
                    {
                        postDataList.Add(pd);
                    }
                }
            }
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
    }
}
