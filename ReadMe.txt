This is a C# console application that scans through the 
Shacknews.com 'Chatty' forum posts for the previous day,
and makes a 'summary' thread, highlighting emoji use and
the posts that get the most user tags for that day.
It uses one of the Shacknews user made API endpoints,
https://winchatty.com/v2/ by default. See 
http://winchatty.com/v2/readme and 
https://www.shackwiki.com/wiki/Shack_alternatives#Shack_APIs
This is intended to be run as a daily task at about noon
Eastern Time.

Semi-based off of https://github.com/askedrelic/todayIs
but uses a completely different method as lmno.pc no
longer provides user tags.

NOTE: Before compiling / running this program,
you need to set a vaild Shacknews account name / pass
in Program.cs