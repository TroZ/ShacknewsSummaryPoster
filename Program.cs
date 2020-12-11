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

        private static string pOSTfILE = "PostDates.txt";

        public static string USERNAME { get => uSERNAME; set => uSERNAME = value; }
        public static string PASSWORD { get => pASSWORD; set => pASSWORD = value; }
        public static string APIURL { get => aPIURL; set => aPIURL = value; }
        public static bool SLEEP { get => sLEEP; set => sLEEP = value; }
        public static string POSTSFILE { get => pOSTfILE; set => pOSTfILE = value; }

        // from http://www.newgeneralservicelist.org/ with some modifications.
        public static string[] commonWords = { "the", "be", "am", "i'm", "is", "isn't", "s", "are", "we're", "aren't", "aint", "been", "was", "wasn't", "were", "weren't", "being", "bein", "beings", "and", "ands", "of", "to", "a", "an", "in", "ins", "have", "haven't", "i've", "has", "hasn't", "had", "hadn't", "i'd", "having", "hafta", "it", "its", "it's", "you", "you're", "you'll", "you've", "ye", "your", "yours", "he", "he's", "him", "im", "his", "for", "they", "they're", "them", "em", "their", "theirs", "not", "t", "that", "that's", "those", "we", "us", "our", "ours", "on", "with", "this", "these", "i", "me", "my", "do", "does", "don't", "doesn't", "did", "didn't", "doing", "done", "doin", "as", "at", "she", "she's", "her", "hers", "but", "buts", "from", "by", "will", "wills", "willed", "willing", "won't", "i'll", "or", "say", "says", "said", "saying", "sayings", "go", "goes", "went", "going", "gone", "goin", "gonna", "so", "all", "if", "ifs", "one", "ones", "would", "wouldn't", "about", "can", "cans", "canned", "canning", "cannot", "can't", "which", "there", "there's", "know", "knows", "knew", "knowing", "known", "more", "get", "gets", "got", "getting", "gotten", "gettin", "gotta", "who", "whom", "whose", "like", "likes", "liked", "liking", "when", "think", "thinks", "thought", "thinking", "thoughts", "make", "makes", "made", "making", "time", "times", "timed", "timing", "see", "sees", "saw", "seeing", "seen", "what", "what's", "up", "ups", "upped", "upping", "some", "other", "others", "out", "outs", "outed", "outing", "outings", "good", "goodest", "people", "peoples", "peopled", "peopling", "year", "years", "take", "takes", "took", "taking", "taken", "no", "well", "wells", "welled", "welling", "because", "cos", "very", "just", "come", "comes", "came", "coming", "cometh", "could", "couldn't", "coulda", "work", "works", "worked", "working", "workings", "use", "uses", "used", "using", "than", "now", "then", "also", "into", "only", "look", "looks", "looked", "looking", "want", "wants", "wanted", "wanting", "wanna", "give", "gives", "gave", "giving", "given", "first", "firsts", "new", "newer", "newest", "way", "ways", "find", "finds", "finding", "findings", "over", "overs", "any", "after", "day", "days", "where", "thing", "things", "most", "should", "shouldn't", "shoulda", "need", "needs", "needed", "needing", "much", "right", "rights", "righted", "righting", "righter", "how", "hows", "back", "backs", "backed", "backing", "mean", "means", "meaning", "meant", "meanings", "meaner", "meanest", "even", "evens", "evened", "may", "here", "many", "such", "last", "lasts", "lasted", "lasting", "child", "children", "tell", "tells", "told", "telling", "really", "call", "calls", "called", "calling", "before", "company", "companies", "through", "down", "downs", "downed", "downing", "show", "shows", "showed", "showing", "shown", "life", "man", "mans", "manned", "manning", "men", "change", "changes", "changed", "changing", "place", "places", "placed", "placing", "long", "longs", "longed", "longing", "longer", "longest", "between", "feel", "feels", "felt", "feeling", "feelings", "too", "still", "stills", "stilled", "stilling", "stiller", "stillest", "problem", "problems", "write", "writes", "wrote", "writing", "written", "writings", "same", "lot", "lots", "great", "greater", "greatest", "greats", "try", "tries", "tried", "trying", "leave", "leaves", "leaved", "leaving", "leavings", "number", "numbers", "numbered", "numbering", "numberings", "both", "own", "owns", "owned", "owning", "part", "parts", "parted", "parting", "point", "points", "pointed", "pointing", "little", "littler", "littlest", "help", "helps", "helped", "helping", "helpings", "ask", "asks", "asked", "asking", "meet", "meets", "meeting", "met", "meetings", "start", "starts", "started", "starting", "talk", "talks", "talked", "talking", "something", "put", "puts", "putted", "putting", "another", "become", "becomes", "became", "becoming", "interest", "interests", "interested", "interesting", "country", "countries", "old", "older", "oldest", "olds", "each", "school", "schools", "schooled", "schooling", "schoolings", "late", "later", "latest", "high", "higher", "highest", "highs", "different", "off", "offs", "offed", "offing", "next", "end", "ends", "ended", "ending", "live", "lives", "lived", "living", "why", "while", "whiles", "whiled", "whiling", "world", "worlds", "week", "weeks", "play", "plays", "played", "playing", "might", "mightn't", "mighta", "must", "mustn't", "musta", "home", "homes", "homed", "homing", "never", "include", "includes", "included", "including", "course", "courses", "coursed", "coursing", "house", "houses", "housed", "housing", "housings", "report", "reports", "reported", "reporting", "group", "groups", "grouped", "grouping", "case", "cases", "cased", "casing", "casings", "woman", "women", "around", "book", "books", "booked", "booking", "bookings", "family", "families", "seem", "seems", "seemed", "seeming", "let", "lets", "letting", "lemme", "lettings", "again", "kind", "kinds", "kinder", "kindest", "keep", "keeps", "keeping", "kept", "hear", "hears", "heard", "hearing", "hearings", "system", "systems", "every", "question", "questions", "questioned", "questioning", "questionings", "during", "always", "big", "bigger", "biggest", "set", "sets", "setting", "settings", "small", "smaller", "smallest", "study", "studies", "studied", "studying", "follow", "follows", "followed", "following", "followings", "begin", "begins", "began", "beginning", "begun", "beginnings", "important", "since", "run", "runs", "ran", "running", "under", "turn", "turns", "turned", "turning", "few", "fewer", "fewest", "bring", "brings", "brought", "bringing", "early", "earlier", "earliest", "hand", "hands", "handed", "handing", "state", "states", "stated", "stating", "move", "moves", "moved", "moving", "money", "moneys", "fact", "facts", "however", "area", "areas", "provide", "provides", "provided", "providing", "name", "names", "named", "naming", "read", "reads", "reading", "readings", "friend", "friends", "month", "months", "large", "larger", "largest", "business", "businesses", "without", "information", "open", "opens", "opened", "opening", "openings", "order", "orders", "ordered", "ordering", "government", "governments", "word", "words", "worded", "wording", "wordings", "issue", "issues", "issued", "issuing", "market", "markets", "marketed", "marketing", "pay", "pays", "paid", "paying", "build", "builds", "building", "buildings", "built", "hold", "holds", "held", "holding", "holdings", "service", "services", "serviced", "servicing", "against", "believe", "believes", "believed", "believing", "second", "seconds", "seconded", "seconding", "sec", "though", "yes", "yeses", "love", "loves", "loved", "loving", "increase", "increases", "increased", "increasing", "job", "jobs", "jobbing", "plan", "plans", "planned", "planning", "result", "results", "resulted", "resulting", "away", "example", "examples", "happen", "happens", "happened", "happening", "happenings", "offer", "offers", "offered", "offering", "offerings", "young", "younger", "youngest", "close", "closes", "closed", "closing", "closer", "closest", "program", "programs", "programmed", "programming", "programme", "programmes", "programed", "programing", "lead", "leads", "led", "leading", "buy", "buys", "bought", "buying", "understand", "understands", "understood", "understanding", "understandings", "thank", "thanks", "thanked", "thanking", "far", "farther", "farthest", "today", "hour", "hours", "student", "students", "face", "faces", "faced", "facing", "hope", "hopes", "hoped", "hoping", "idea", "ideas", "cost", "costs", "costing", "less", "room", "rooms", "roomed", "rooming", "until", "til", "reason", "reasons", "reasoned", "reasoning", "form", "forms", "formed", "forming", "spend", "spends", "spending", "spent", "head", "heads", "headed", "heading", "headings", "car", "cars", "learn", "learns", "learned", "learning", "learnt", "level", "levels", "leveled", "leveling", "levelled", "levelling", "person", "persons", "experience", "experiences", "experienced", "experiencing", "once", "member", "members", "enough", "bad", "worse", "worst", "baddest", "city", "cities", "night", "nights", "able", "abler", "ablest", "support", "supports", "supported", "supporting", "whether", "line", "lines", "lined", "lining", "present", "presents", "presented", "presenting", "side", "sides", "sided", "siding", "sidings", "quite", "although", "sure", "surer", "surest", "term", "terms", "termed", "terming", "least", "age", "ages", "aged", "aging", "ageing", "low", "lows", "lowed", "lowing", "lower", "lowest", "lowers", "lowered", "lowering", "speak", "speaks", "spoke", "speaking", "spoken", "within", "process", "processes", "processed", "processing", "public", "publics", "often", "train", "trains", "trained", "training", "trainings", "possible", "actually", "rather", "view", "views", "viewed", "viewing", "together", "consider", "considers", "considered", "considering", "price", "prices", "priced", "pricing", "pricings", "parent", "parents", "parented", "parenting", "hard", "harder", "hardest", "party", "parties", "partied", "partying", "local", "locals", "control", "controls", "controlled", "controlling", "already", "concern", "concerns", "concerned", "concerning", "product", "products", "lose", "loses", "lost", "losing", "story", "stories", "storied", "storey", "storys", "almost", "continue", "continues", "continued", "continuing", "stand", "stands", "stood", "standing", "whole", "wholes", "yet", "rate", "rates", "rated", "rating", "ratings", "care", "cares", "cared", "caring", "expect", "expects", "expected", "expecting", "effect", "effects", "effected", "effecting", "sort", "sorts", "sorted", "sorting", "ever", "anything", "cause", "causes", "caused", "causing", "fall", "falls", "fell", "falling", "fallen", "deal", "deals", "dealing", "dealt", "water", "waters", "watered", "watering", "send", "sends", "sending", "sent", "allow", "allows", "allowed", "allowing", "soon", "sooner", "soonest", "watch", "watches", "watched", "watching", "base", "bases", "based", "basing", "baser", "basest", "probably", "suggest", "suggests", "suggested", "suggesting", "past", "power", "powers", "powered", "powering", "test", "tests", "tested", "testing", "visit", "visits", "visited", "visiting", "center", "centers", "centered", "centering", "centre", "centres", "centred", "grow", "grows", "grew", "growing", "grown", "nothing", "return", "returns", "returned", "returning", "mother", "mothers", "mothered", "mothering", "walk", "walks", "walked", "walking", "matter", "matters", "mattered", "mattering", "mind", "minds", "minded", "minding", "value", "values", "valued", "valuing", "office", "offices", "record", "records", "recorded", "recording", "recordings", "stay", "stays", "stayed", "staying", "force", "forces", "forced", "forcing", "stop", "stops", "stopped", "stopping", "several", "light", "lights", "lighted", "lit", "lighting", "lightest", "develop", "develops", "developed", "developing", "remember", "remembers", "remembered", "remembering", "bit", "bits", "bitted", "share", "shares", "shared", "sharing", "real", "realer", "answer", "answers", "answered", "answering", "sit", "sits", "sat", "sitting", "figure", "figures", "figured", "figuring", "letter", "letters", "lettering", "letterings", "decide", "decides", "decided", "deciding", "language", "languages", "subject", "subjects", "subjected", "subjecting", "class", "classes", "classed", "classing", "development", "developments", "town", "towns", "half", "halves", "halved", "halving", "minute", "minutes", "food", "foods", "break", "breaks", "broke", "breaking", "broken", "clear", "clears", "cleared", "clearing", "clearer", "clearest", "future", "futures", "either", "ago", "per", "remain", "remains", "remained", "remaining", "top", "tops", "topped", "topping", "toppings", "among", "win", "wins", "winning", "winnings", "color", "colors", "colored", "coloring", "colorings", "colour", "colours", "coloured", "colouring", "involve", "involves", "involved", "involving", "reach", "reaches", "reached", "reaching", "social", "socials", "period", "periods", "across", "note", "notes", "noted", "noting", "history", "histories", "create", "creates", "created", "creating", "drive", "drives", "drove", "driving", "driven", "along", "type", "types", "typed", "typing", "sound", "sounds", "sounded", "sounding", "eye", "eyes", "eyed", "eyeing", "music", "game", "games", "gamed", "gaming", "political", "free", "frees", "freed", "freeing", "freer", "freest", "receive", "receives", "received", "receiving", "moment", "moments", "sale", "sales", "policy", "policies", "further", "furthers", "furthered", "furthering", "body", "bodies", "bodied", "bodying", "require", "requires", "required", "requiring", "wait", "waits", "waited", "waiting", "general", "generals", "appear", "appears", "appeared", "appearing", "toward", "towards", "team", "teams", "teamed", "teaming", "easy", "easier", "easiest", "individual", "individuals", "full", "fuller", "fullest", "black", "blacks", "blacked", "blacking", "blacker", "blackest", "sense", "senses", "sensed", "sensing", "perhaps", "add", "adds", "added", "adding", "rule", "rules", "ruled", "ruling", "rulings", "pass", "passes", "passed", "passing", "produce", "produces", "produced", "producing", "sell", "sells", "sold", "selling", "short", "shorts", "shorted", "shorting", "shorter", "shortest", "agree", "agrees", "agreed", "agreeing", "law", "laws", "everything", "research", "researches", "researched", "researching", "cover", "covers", "covered", "covering", "coverings", "paper", "papers", "papered", "papering", "position", "positions", "positioned", "positioning", "near", "nears", "neared", "nearing", "nearer", "nearest", "human", "humans", "computer", "computers", "situation", "situations", "staff", "staffs", "staffed", "staffing", "activity", "activities", "film", "films", "filmed", "filming", "morning", "mornings", "war", "wars", "warred", "warring", "account", "accounts", "accounted", "accounting", "shop", "shops", "shopped", "shopping", "major", "majors", "majored", "majoring", "someone", "above", "design", "designs", "designed", "designing", "event", "events", "special", "specials", "sometimes", "condition", "conditions", "conditioned", "conditioning", "conditionings", "carry", "carries", "carried", "carrying", "choose", "chooses", "chose", "choosing", "chosen", "father", "fathers", "fathered", "fathering", "decision", "decisions", "table", "tables", "tabled", "certain", "forward", "forwards", "forwarded", "forwarding", "main", "die", "dies", "died", "dying", "bear", "bears", "bearing", "borne", "bearings", "cut", "cuts", "cutting", "cuttings", "describe", "describes", "described", "describing", "himself", "available", "especially", "strong", "stronger", "strongest", "rise", "rises", "rising", "risen", "girl", "girls", "maybe", "community", "communities", "else", "particular", "particulars", "role", "roles", "join", "joins", "joined", "joining", "difficult", "please", "pleases", "pleased", "pleasing", "detail", "details", "detailed", "detailing", "difference", "differences", "differenced", "differencing", "action", "actions", "health", "eat", "eats", "ate", "eating", "eaten", "step", "steps", "stepped", "stepping", "true", "trues", "trued", "truing", "truer", "truest", "phone", "phones", "phoned", "phoning", "themselves", "draw", "draws", "drew", "drawing", "drawn", "drawings", "white", "whiter", "whitest", "whites", "date", "dates", "dated", "dating", "practice", "practices", "practiced", "practicing", "practise", "practised", "practises", "practising", "model", "models", "modeled", "modeling", "modelled", "modelling", "raise", "raises", "raised", "raising", "raisings", "customer", "customers", "front", "fronts", "fronted", "fronting", "explain", "explains", "explained", "explaining", "door", "doors", "outside", "outsides", "behind", "economic", "economics", "site", "sites", "sited", "approach", "approaches", "approached", "approaching", "teacher", "teachers", "land", "lands", "landed", "landing", "landings", "charge", "charges", "charged", "charging", "finally", "sign", "signs", "signed", "signing", "claim", "claims", "claimed", "claiming", "relationship", "relationships", "travel", "travels", "traveled", "traveling", "travelled", "travelling", "enjoy", "enjoys", "enjoyed", "enjoying", "death", "deaths", "nice", "nicer", "nicest", "amount", "amounts", "amounted", "improve", "improves", "improved", "improving", "picture", "pictures", "pictured", "picturing", "boy", "boys", "regard", "regards", "regarded", "regarding", "organization", "organizations", "organisation", "organisations", "happy", "happier", "happiest", "couple", "couples", "coupled", "coupling", "act", "acts", "acted", "acting", "range", "ranges", "ranged", "ranging", "quality", "qualities", "project", "projects", "projected", "projecting", "round", "rounds", "rounded", "rounding", "rounder", "roundest", "opportunity", "opportunities", "road", "roads", "accord", "accords", "accorded", "according", "list", "lists", "listed", "listing", "listings", "wish", "wishes", "wished", "wishing", "therefore", "wear", "wears", "wore", "wearing", "worn", "fund", "funds", "funded", "funding", "rest", "rests", "rested", "resting", "kid", "kids", "kidded", "kidding", "industry", "industries", "education", "educations", "measure", "measures", "measured", "measuring", "kill", "kills", "killed", "killing", "killings", "serve", "serves", "served", "serving", "servings", "likely", "likelier", "likeliest", "certainly", "national", "nationals", "itself", "teach", "teaches", "taught", "teaching", "teachings", "field", "fields", "fielded", "fielding", "security", "securities", "air", "airs", "aired", "airing", "benefit", "benefits", "benefited", "benefiting", "benefitted", "benefitting", "trade", "trades", "traded", "trading", "risk", "risks", "risked", "risking", "news", "standard", "standards", "vote", "votes", "voted", "voting", "percent", "percents", "focus", "focuses", "focused", "focusing", "focussed", "focuss", "focusses", "focussing", "stage", "stages", "staged", "staging", "space", "spaces", "spaced", "spacing", "instead", "realize", "realizes", "realized", "realizing", "realise", "realises", "realised", "realising", "usually", "data", "datum", "single", "singles", "singled", "singling", "address", "addresses", "addressed", "addressing", "performance", "performances", "chance", "chances", "chanced", "chancing", "accept", "accepts", "accepted", "accepting", "society", "societies", "technology", "technologies", "mention", "mentions", "mentioned", "mentioning", "choice", "choices", "choicer", "choicest", "save", "saves", "saved", "saving", "savings", "common", "commonest", "culture", "cultures", "cultured", "culturing", "total", "totals", "totaled", "totaling", "totalled", "totalling", "demand", "demands", "demanded", "demanding", "material", "materials", "limit", "limits", "limited", "limiting", "listen", "listens", "listened", "listening", "due", "wrong", "wrongs", "wronged", "wronging", "foot", "foots", "footed", "footing", "feet", "effort", "efforts", "attention", "attentions", "upon", "check", "checks", "checked", "checking", "complete", "completes", "completed", "completing", "lie", "lies", "lied", "lying", "lain", "pick", "picks", "picked", "picking", "reduce", "reduces", "reduced", "reducing", "personal", "personals", "ground", "grounds", "grounded", "grounding", "animal", "animals", "arrive", "arrives", "arrived", "arriving", "patient", "patients", "current", "currents", "century", "centuries", "evidence", "evidences", "evidenced", "evidencing", "exist", "exists", "existed", "existing", "similar", "fight", "fights", "fought", "fighting", "leader", "leaders", "fine", "fines", "fined", "fining", "finer", "finest", "street", "streets", "former", "formers", "contact", "contacts", "contacted", "contacting", "particularly", "wife", "wives", "sport", "sports", "sported", "sporting", "prepare", "prepares", "prepared", "preparing", "discuss", "discusses", "discussed", "discussing", "response", "responses", "voice", "voices", "voiced", "voicing", "piece", "pieces", "pieced", "piecing", "finish", "finishes", "finished", "finishing", "suppose", "supposes", "supposed", "supposing", "apply", "applies", "applied", "applying", "president", "presidents", "fire", "fires", "fired", "firing", "firings", "compare", "compares", "compared", "comparing", "court", "courts", "courted", "courting", "police", "polices", "policed", "policing", "store", "stores", "stored", "storing", "poor", "poorer", "poorest", "knowledge", "laugh", "laughs", "laughed", "laughing", "arm", "arms", "armed", "arming", "heart", "hearts", "hearted", "source", "sources", "sourced", "sourcing", "employee", "employees", "manage", "manages", "managed", "managing", "simply", "bank", "banks", "banked", "banking", "firm", "firms", "firmed", "firming", "firmer", "firmest", "cell", "cells", "celled", "article", "articles", "articled", "articling", "fast", "fasts", "fasted", "fasting", "faster", "fastest", "attack", "attacks", "attacked", "attacking", "foreign", "surprise", "surprises", "surprised", "surprising", "feature", "features", "featured", "featuring", "factor", "factors", "factored", "factoring", "factorings", "pretty", "pretties", "prettying", "prettier", "prettiest", "recently", "affect", "affects", "affected", "affecting", "drop", "drops", "dropped", "dropping", "recent", "relate", "relates", "related", "relating", "official", "officials", "financial", "financials", "miss", "misses", "missed", "missing", "art", "arts", "campaign", "campaigns", "campaigned", "campaigning", "private", "pause", "pauses", "paused", "pausing", "everyone", "forget", "forgets", "forgot", "forgetting", "forgotten", "page", "pages", "paged", "paging", "worry", "worries", "worried", "worrying", "summer", "summers", "summered", "drink", "drinks", "drank", "drinking", "opinion", "opinions", "opinioned", "park", "parks", "parked", "parking", "represent", "represents", "represented", "representing", "key", "keys", "keyed", "keying", "inside", "insides", "manager", "managers", "international", "internationals", "contain", "contains", "contained", "containing", "notice", "notices", "noticed", "noticing", "wonder", "wonders", "wondered", "wondering", "wonderings", "nature", "natures", "natured", "structure", "structures", "structured", "structuring", "section", "sections", "sectioned", "sectioning", "myself", "exactly", "plant", "plants", "planted", "planting", "plantings", "paint", "paints", "painted", "painting", "paintings", "worker", "workers", "press", "presses", "pressed", "pressing", "pressings", "whatever", "necessary", "necessaries", "region", "regions", "growth", "growths", "evening", "evenings", "influence", "influences", "influenced", "influencing", "respect", "respects", "respected", "respecting", "various", "catch", "catches", "caught", "catching", "thus", "skill", "skills", "skilled", "attempt", "attempts", "attempted", "attempting", "son", "sons", "simple", "simpler", "simplest", "medium", "mediums", "average", "averages", "averaged", "averaging", "stock", "stocks", "stocked", "stocking", "management", "managements", "character", "characters", "bed", "beds", "bedded", "bedding", "beddings", "hit", "hits", "hitting", "establish", "establishes", "established", "establishing", "indeed", "final", "finals", "economy", "economies", "fit", "fits", "fitted", "fitting", "fitter", "fittest", "fittings", "guy", "guys", "guyed", "guying", "function", "functions", "functioned", "functioning", "yesterday", "yesterdays", "image", "images", "imaged", "imaging", "size", "sizes", "sized", "sizing", "behavior", "behaviors", "behaviour", "behaviours", "addition", "additions", "determine", "determines", "determined", "determining", "station", "stations", "stationed", "stationing", "population", "populations", "fail", "fails", "failed", "failing", "failings", "environment", "environments", "production", "productions", "contract", "contracts", "contracted", "contracting", "player", "players", "comment", "comments", "commented", "commenting", "enter", "enters", "entered", "entering", "occur", "occurs", "occurred", "occurring", "alone", "significant", "drug", "drugs", "drugged", "drugging", "wall", "walls", "walled", "walling", "series", "direct", "directs", "directed", "directing", "success", "successes", "tomorrow", "tomorrows", "director", "directors", "clearly", "lack", "lacks", "lacked", "lacking", "review", "reviews", "reviewed", "reviewing", "depend", "depends", "depended", "depending", "race", "races", "raced", "racing", "recognize", "recognizes", "recognized", "recognizing", "recognise", "recognises", "recognised", "recognising", "window", "windows", "windowed", "windowing", "purpose", "purposes", "purposed", "purposing", "department", "departments", "gain", "gains", "gained", "gaining", "tree", "trees", "college", "colleges", "argue", "argues", "argued", "arguing", "board", "boards", "boarded", "boarding", "holiday", "holidays", "holidayed", "holidaying", "mark", "marks", "marked", "marking", "markings", "church", "churches", "churched", "churching", "machine", "machines", "machined", "machining", "achieve", "achieves", "achieved", "achieving", "item", "items", "prove", "proves", "proved", "proving", "proven", "cent", "cents", "season", "seasons", "seasoned", "seasoning", "seasonings", "floor", "floors", "floored", "flooring", "floorings", "stuff", "stuffs", "stuffed", "stuffing", "wide", "wider", "widest", "anyone", "method", "methods", "analysis", "analyses", "election", "elections", "military", "militaries", "hotel", "hotels", "club", "clubs", "clubbed", "clubbing", "below", "movie", "movies", "doctor", "doctors", "doctored", "doctoring", "discussion", "discussions", "sorry", "sorrier", "sorriest", "challenge", "challenges", "challenged", "challenging", "nation", "nations", "nearly", "statement", "statements", "link", "links", "linked", "linking", "despite", "introduce", "introduces", "introduced", "introducing", "advantage", "advantages", "advantaged", "ready", "readies", "readied", "readying", "readier", "readiest", "marry", "marries", "married", "marrying", "strike", "strikes", "struck", "striking", "mile", "miles", "seek", "seeks", "sought", "seeking", "ability", "abilities", "unit", "units", "card", "cards", "carded", "carding", "hospital", "hospitals", "quickly", "interview", "interviews", "interviewed", "interviewing", "agreement", "agreements", "release", "releases", "released", "releasing", "tax", "taxes", "taxed", "taxing", "solution", "solutions", "capital", "capitals", "popular", "specific", "specifics", "beautiful", "fear", "fears", "feared", "fearing", "aim", "aims", "aimed", "aiming", "television", "televisions", "serious", "target", "targets", "targeted", "targeting", "degree", "degrees", "pull", "pulls", "pulled", "pulling", "red", "reds", "redder", "reddest", "husband", "husbands", "husbanded", "husbanding", "access", "accesses", "accessed", "accessing", "movement", "movements", "treat", "treats", "treated", "treating", "identify", "identifies", "identified", "identifying", "loss", "losses", "shall", "modern", "moderns", "pressure", "pressures", "pressured", "pressuring", "bus", "buses", "bused", "busing", "treatment", "treatments", "yourself", "yourselves", "supply", "supplies", "supplied", "supplying", "village", "villages", "worth", "natural", "naturals", "express", "expresses", "expressed", "expressing", "indicate", "indicates", "indicated", "indicating", "attend", "attends", "attended", "attending", "brother", "brothers", "investment", "investments", "score", "scores", "scored", "scoring", "scorings", "organize", "organizes", "organized", "organizing", "organise", "organises", "organised", "organising", "trip", "trips", "tripped", "tripping", "beyond", "sleep", "sleeps", "slept", "sleeping", "fish", "fishes", "fished", "fishing", "promise", "promises", "promised", "promising", "potential", "potentials", "energy", "energies", "trouble", "troubles", "troubled", "troubling", "relation", "relations", "touch", "touches", "touched", "touching", "file", "files", "filed", "filing", "filings", "middle", "middles", "middled", "middling", "bar", "bars", "barred", "barring", "suffer", "suffers", "suffered", "suffering", "sufferred", "sufferring", "strategy", "strategies", "deep", "deeper", "deepest", "deeps", "except", "excepts", "excepted", "excepting", "clean", "cleans", "cleaned", "cleaning", "cleanings", "tend", "tends", "tended", "tending", "advance", "advances", "advanced", "advancing", "fill", "fills", "filled", "filling", "fillings", "star", "stars", "starred", "starring", "network", "networks", "networked", "networking", "generally", "operation", "operations", "match", "matches", "matched", "matching", "avoid", "avoids", "avoided", "avoiding", "seat", "seats", "seated", "seating", "throw", "throws", "threw", "throwing", "thrown", "task", "tasks", "tasked", "tasking", "normal", "normals", "goal", "goals", "associate", "associates", "associated", "associating", "blue", "blues", "blued", "bluing", "bluer", "bluest", "positive", "positives", "option", "options", "box", "boxes", "boxed", "boxing", "huge", "huger", "hugest", "message", "messages", "messaged", "messaging", "instance", "instances", "instanced", "instancing", "style", "styles", "styled", "styling", "refer", "refers", "referred", "referring", "refered", "refering", "cold", "colder", "coldest", "colds", "push", "pushes", "pushed", "pushing", "quarter", "quarters", "quartered", "quartering", "assume", "assumes", "assumed", "assuming", "baby", "babies", "babied", "babying", "successful", "sing", "sings", "sang", "singing", "sung", "doubt", "doubts", "doubted", "doubting", "competition", "competitions", "theory", "theories", "propose", "proposes", "proposed", "proposing", "reference", "references", "referenced", "referencing", "argument", "arguments", "adult", "adults", "fly", "flies", "flew", "flying", "flown", "document", "documents", "documented", "documenting", "pattern", "patterns", "patterned", "patterning", "application", "applications", "hot", "hots", "hotter", "hottest", "obviously", "unclear", "bill", "bills", "billed", "billing", "search", "searches", "searched", "searching", "separate", "separates", "separated", "separating", "central", "centrals", "career", "careers", "careered", "careering", "anyway", "anyways", "speech", "speeches", "dog", "dogs", "dogged", "dogging", "officer", "officers", "officered", "officering", "throughout", "oil", "oils", "oiled", "oiling", "dress", "dresses", "dressed", "dressing", "profit", "profits", "profited", "profiting", "guess", "guesses", "guessed", "guessing", "fun", "protect", "protects", "protected", "protecting", "resource", "resources", "resourced", "resourcing", "science", "sciences", "disease", "diseases", "diseased", "balance", "balances", "balanced", "balancing", "damage", "damages", "damaged", "damaging", "basis", "author", "authors", "authored", "authoring", "basic", "basics", "encourage", "encourages", "encouraged", "encouraging", "hair", "hairs", "haired", "male", "males", "operate", "operates", "operated", "operating", "reflect", "reflects", "reflected", "reflecting", "exercise", "exercises", "exercised", "exercising", "useful", "restaurant", "restaurants", "income", "incomes", "property", "properties", "previous", "dark", "darker", "darkest", "imagine", "imagines", "imagined", "imagining", "imaginings", "okay", "okays", "okayed", "okaying", "ok", "earn", "earns", "earned", "earning", "earnings", "daughter", "daughters", "post", "posts", "posted", "posting", "postings", "newspaper", "newspapers", "define", "defines", "defined", "defining", "conclusion", "conclusions", "clock", "clocks", "clocked", "clocking", "everybody", "weekend", "weekends", "weekending", "perform", "performs", "performed", "performing", "professional", "professionals", "mine", "mines", "mined", "mining", "debate", "debates", "debated", "debating", "memory", "memories", "green", "greens", "greened", "greening", "greener", "greenest", "song", "songs", "object", "objects", "objected", "objecting", "maintain", "maintains", "maintained", "maintaining", "credit", "credits", "credited", "crediting", "ring", "rings", "ringed", "rang", "ringing", "rung", "discover", "discovers", "discovered", "discovering", "dead", "deader", "deadest", "afternoon", "afternoons", "prefer", "prefers", "preferred", "preferring", "prefered", "prefering", "extend", "extends", "extended", "extending", "possibility", "possibilities", "direction", "directions", "facility", "facilities", "variety", "varieties", "daily", "dailies", "clothes", "screen", "screens", "screened", "screening", "screenings", "track", "tracks", "tracked", "tracking", "dance", "dances", "danced", "dancing", "completely", "female", "females", "responsibility", "responsibilities", "original", "originals", "sister", "sisters", "rock", "rocks", "rocked", "rocking", "dream", "dreams", "dreamed", "dreaming", "dreamt", "nor", "university", "universities", "easily", "agency", "agencies", "dollar", "dollars", "garden", "gardens", "gardened", "gardening", "fix", "fixes", "fixed", "fixing", "fixings", "ahead", "cross", "crosses", "crossed", "crossing", "crossings", "yeah", "weight", "weights", "weighted", "weighting", "weightings", "legal", "proposal", "proposals", "version", "versions", "versioned", "conversation", "conversations", "somebody", "pound", "pounds", "pounded", "pounding", "poundings", "magazine", "magazines", "shape", "shapes", "shaped", "shaping", "sea", "seas", "immediately", "welcome", "welcomes", "welcomed", "welcoming", "smile", "smiles", "smiled", "smiling", "communication", "communications", "agent", "agents", "traditional", "replace", "replaces", "replaced", "replacing", "judge", "judges", "judged", "judging", "herself", "suddenly", "generation", "generations", "estimate", "estimates", "estimated", "estimating", "favorite", "favorites", "favourite", "favourites", "difficulty", "difficulties", "purchase", "purchases", "purchased", "purchasing"};
        public static Dictionary<String, String> CommonWords = new Dictionary<string, string>();

        public static ILogger logger;

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
            logger = loggerFactory.CreateLogger<Program>();
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
            POSTSFILE = config["postfile"];


            //System.Console.WriteLine("Posting as '" + USERNAME + "' with pass '"+ PASSWORD+"' and sleep = " + SLEEP);
            logger.LogInformation("Posting as '" + USERNAME + "' and sleep = " + SLEEP);

#if !DEBUG
            System.Console.WriteLine("\nShort delay...\n");
            Thread.Sleep(10 * 1000);  //10 second delay to allow network to re-connect if we just awoke from sleep.
#endif

            foreach(string word in commonWords)
            {
                CommonWords.Add(word, word);
                if (word.Contains("'"))
                {
                    string w = word.Replace('\'', '’');
                    CommonWords.Add(w, w);
                }
            }


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
