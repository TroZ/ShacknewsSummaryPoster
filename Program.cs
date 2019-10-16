//using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
//using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.IO;
//using System.Net;
//using System.Text.RegularExpressions;
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
    //This is intended to be run about 6PM Central Time, so that all the threads reported on have mostly run their course.

    //TODO  Implement Birthdays from https://github.com/askedrelic/todayIs - Does the database still exist?


    class Program
    {

        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) //this doesn't exist???
            .AddXmlFile("ShackPostReport.xml", optional: false, reloadOnChange: false)

            // This allows us to set a system environment variable to Development
            // when running a compiled Release build on a local workstation, so we don't
            // have to alter our real production appsettings file for compiled-local-test.
            //.AddXmlFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)

            //.AddEnvironmentVariables()
            //.AddAzureKeyVault()
            .Build();

        private static string uSERNAME = "<YOUR NAME HERE>"; //"<YOUR NAME HERE>";
        private static string pASSWORD = "<YOUR PASSWORD HERE>"; //"<YOUR PASSWORD HERE>";

        private static string aPIURL = "https://winchatty.com/v2/";

        private static bool sLEEP = false;

        public static string USERNAME { get => uSERNAME; set => uSERNAME = value; }
        public static string PASSWORD { get => pASSWORD; set => pASSWORD = value; }
        public static string APIURL { get => aPIURL; set => aPIURL = value; }
        public static bool SLEEP { get => sLEEP; set => sLEEP = value; }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!\n");


            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole()
                    .AddEventLog();
            });
            ILogger logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("ShackPostSummary Starting up...");


            Console.WriteLine("\n\nSettings:");

            IConfigurationSection config = Configuration.GetSection("settings");

            foreach(KeyValuePair<string,string> pair in config.AsEnumerable())
            {
                Console.WriteLine("" + pair.Key + " = " + pair.Value + "");
            }

            Console.WriteLine("\n\n");




            USERNAME = config["username"];//My.Default.USERNAME; //ConfigurationManager.AppSettings.Get("Username"); //ShackPostSummary.Default.Login;
            PASSWORD = config["password"];//My.Default.PASSWORD; //ConfigurationManager.AppSettings.Get("Password");
            APIURL = config["apiurl"];//My.Default.APIURL; //ConfigurationManager.AppSettings.Get("APIurl");
            SLEEP = config["sleep"].ToLower() == "true";//My.Default.SLEEP; //ConfigurationManager.AppSettings.Get("Sleep") == "True";


            //System.Console.WriteLine("Posting as '" + USERNAME + "' with pass '"+ PASSWORD+"' and sleep = " + SLEEP);
            logger.LogInformation("Posting as '" + USERNAME + "' and sleep = " + SLEEP);

            System.Console.WriteLine("\nShort delay...\n");
            Thread.Sleep(10 * 1000);  //10 second delay to allow network to re-connect if we just awoke from sleep.


            //test network
            int count = 5;
            while (count > 0)
            {
                try
                {
                    string content = ShackPostReport.GetUrl(APIURL+ "readme");
                    if (content.Length > 0)
                    {
                        break; //network
                    }
                    count--;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error when testing network connection", APIURL);
                }
                Thread.Sleep(30 * 1000);//wait and try again
            }

            //ready to run
            if (count > 0)
            {
                try
                {
                    new ShackPostReport();
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "Error occurred while running post report!");
                }
            }
            else
            {
                logger.LogError("Failed to contact API!");
            }


            if (SLEEP)
            {
                System.Console.WriteLine("Sleeping Computer in 30 seconds\n");
                logger.LogInformation("Sleeping Computer in 30 seconds");
                //System.Media.SystemSounds.Beep.Play();
                Thread.Sleep(1000);
                System.Console.Beep();

                Thread.Sleep(30 * 1000);

                //System.Media.SystemSounds.Beep.Play();
                Thread.Sleep(1000);
                System.Console.Beep();
                Thread.Sleep(1000);

                //sleep computer wehn done
                System.Console.WriteLine("Sleeping!\n\n");
                logger.LogInformation("Sleeping Computer and exitting");

                //System.Windows.Forms.Application.SetSuspendState(PowerState.Suspend, false, false);
                Process.Start("shutdown", "/h /f");
            }
            else
            {
                logger.LogInformation("Exitting");
            }
        }


    }
}
