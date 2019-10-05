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
    //This is intended to be run about noon Central Time, so that all the threads reported on have mostly run their course.

    //TODO  Implement Birthdays and -[ShackBattles]- from https://github.com/askedrelic/todayIs

    class Program
    {
        public const string USERNAME = "<YOUR NAME HERE>";
        public const string PASSWORD = "<YOUR PASSWORD HERE>";

        public const string APIURL = "https://winchatty.com/v2/";

        

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            new ShackPostReport();
        }

    }
}
