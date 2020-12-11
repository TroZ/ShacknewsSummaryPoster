using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shackmojis
{
    class PostDataList
    {

        public SortedList<int, PostData> postDataList = new SortedList<int, PostData>();

        ILogger logger;
        string filepath;

        public PostDataList(ILogger log, string file)
        {
            logger = log;
            filepath = file;
        }

        public bool Load()
        {
            string basefile = filepath;

            if (File.Exists(basefile))
            {
                string[] lines = File.ReadAllLines(basefile);

                foreach (string line in lines)
                {
                    int pos = line.IndexOf(" ");
                    string id = line.Substring(0, pos);
                    string date = line.Substring(pos + 1);
                    bool summary = false;

                    if (date.EndsWith(" *"))
                    {
                        date = date.Substring(0, date.Length - 2);
                        summary = true;
                    }

                    DateTime postDate = DateTime.Parse(date);
                    int postId = Int32.Parse(id);

                    PostData pd = new PostData(postId, postDate);
                    if (summary)
                    {
                        pd.summary = true;
                    }

                    postDataList.Add(postId, pd);
                }

                logger.LogInformation("Loaded " + postDataList.Count + " records.");

                return true;
            }

            return false;
        }

        public bool Save()
        {
            string basefile = filepath;

            string bak = basefile + ".bak";

            //delete any previous backup file
            if (File.Exists(bak))
            {
                File.Delete(bak);
            }

            //rename current file to backup
            if (File.Exists(basefile))
            {
                File.Move(basefile, bak);
            }

            List<string> lines = new List<string>();

            DateTime cutoffDate = DateTime.Now;
            cutoffDate = cutoffDate.AddDays(-40);

            foreach (PostData pd in postDataList.Values)
            {
                if (pd.postDate > cutoffDate)
                {
                    string line = "" + pd.id + " " + pd.postDate;
                    if (pd.summary)
                    {
                        line += " *";
                    }
                    lines.Add(line);
                }
            }

            logger.LogInformation("Saving " + lines.Count + " of " + postDataList.Count + " records.");

            File.WriteAllLines(basefile, lines);

            return true;
        }

        public bool ContainsKey(int key)
        {
            return postDataList.ContainsKey(key);
        }

        public void Add(PostData pd)
        {
            postDataList.Add(pd.id, pd);
        }
    }
}
