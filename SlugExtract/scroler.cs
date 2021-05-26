using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;

namespace SlugExtract
{
    class scroler
    {
        static void Main(string[] args)
        {
            ccfg.Load(cfgpath);
            for ( ; ; )
            {
                string currAction;
                currAction = ConsoleFace.ChoicePrompt("Available actions:\n[fetch] tigsource snaps\nselect read/write [paths]\n[scan] a singular document\nget WBM [links] for failed pages\nget [images] from gwynstash\n[exit]\n", new string[] { "fetch", "scan", "exit", "paths", "links", "images" });
                Console.WriteLine();
                switch (currAction)
                {
                    
                    case "scan":
                        string PA = 1.ToString();
                        HtmlDocument htd = new HtmlDocument();
                        string tein;
                        tein = File.ReadAllText(ccfg.SourcePath);
                        htd.LoadHtml(tein);
                        List<PostInfo> lp;
                        ScanDoc(htd, out lp);
                        WritePosts(lp, ccfg.OutputPath, PA);
                        break;
                    case "fetch":
                        bool ActuallyRecord = true;
                        int lowb = 131;
                        int higb = 367;
                        List<int> OlFP = JsonConvert.DeserializeObject<List<int>>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "errlist.txt")));
                        List<int> FailedPages = new List<int>();
                        HtmlWeb htw = new HtmlWeb();
                        HtmlDocument chtd;
                        for (int l = 0; l < OlFP.Count; l++)
                        {
                            int i = OlFP[l];
                            for (int attempts = 3; attempts > 0; attempts--)
                            {
                                try { chtd = htw.Load(WbLinkFromPageIndex(i)); if (htw.StatusCode != System.Net.HttpStatusCode.OK) throw new System.Net.WebException($"Error loading page: return code {htw.StatusCode}"); goto success; }
                                catch (System.Net.WebException e)
                                {
                                    Console.WriteLine($"Could not load page {i}: an exception has occured.\n{e}\nAttempts left: {attempts}.\n");
                                }
                            }
                            Console.WriteLine($"MAX NUMBER OF RETRIES EXCEEDED; PAGE {i} HAS BEEN SKIPPED\n");
                            FailedPages.Add(i);
                            continue;
                        success:
                            if (ActuallyRecord)
                            {
                                List<PostInfo> lpi;
                                try
                                {
                                    ScanDoc(chtd, out lpi);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Could not scan page {i}: exception encountered. Skipping.");
                                    Console.WriteLine(e);
                                    FailedPages.Add(i);
                                    continue;
                                }
                                WritePosts(lpi, ccfg.OutputPath, i.ToString());
                                Console.WriteLine($"Page fetched and scanned: {i}\n");
                            }
                            else
                            {
                                Console.WriteLine($"Page successfully fetched: {i}");
                            }
                        }
                        if (FailedPages.Count > 0)
                        {
                            Console.WriteLine("ONE OR MORE PAGES FAILED TO LOAD AND WERE SKIPPED:");
                            foreach (int i in FailedPages) Console.WriteLine(i);
                            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "errlist.txt"), JsonConvert.SerializeObject(FailedPages, Formatting.Indented));

                        }
                        break;
                    case "paths":
                        string AAAA;
                        AAAA = ConsoleFace.InputPrompt($"Enter source path (leave empty to keep it \"{ccfg.SourcePath}\")");
                        if (AAAA != string.Empty) { ccfg.SourcePath = AAAA; }
                        AAAA = ConsoleFace.InputPrompt($"Enter output path (leave empty to keep it \"{ccfg.OutputPath}\")");
                        if (AAAA != string.Empty) { ccfg.OutputPath = AAAA; }
                        break;
                    case "links":
                        List<int> OldFailedPages = JsonConvert.DeserializeObject<List<int>>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "errlist.txt")));
                        List<string> output = new List<string>();
                        foreach (int fpin in OldFailedPages)
                        {
                            string el = WbLinkFromPageIndex(fpin);
                            output.Add(el);
                            Console.WriteLine(el);
                        }
                        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "pages.txt"), JsonConvert.SerializeObject(output, Formatting.Indented));
                        break;
                    case "images":
                        DirectoryInfo din = new DirectoryInfo(Path.Combine(ccfg.OutputPath, "FetchedImages"));
                        FileInfo[] files = din.GetFiles();
                        foreach (FileInfo fi in files)
                        {
                            try
                            {
                                using (var bmp = new System.Drawing.Bitmap(fi.FullName))
                                {

                                }
                                
                            }
                            catch
                            {
                                fi.Delete();
                            }
                        }
                        break;
                        List<string> FailedImages = new List<string>();
                        string[] allLinks = File.ReadAllLines("outputlinks.txt");
                        WebClient client = new WebClient();
                        foreach (string imglink in allLinks)
                        {
                            if (!Directory.Exists(Path.Combine(ccfg.OutputPath, "FetchedImages"))) Directory.CreateDirectory(Path.Combine(ccfg.OutputPath, "FetchedImages"));
                            for (int retries = 3; retries >= 0; retries--)
                            {
                                try
                                {
                                    string filename = Regex.Split(imglink, "/").Last();
                                    client.DownloadFile(imglink, Path.Combine(ccfg.OutputPath, "FetchedImages", filename));
                                    client.DownloadData(imglink);
                                    goto success;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Something went wrong.");
                                    Console.WriteLine(e);
                                    Console.WriteLine($"retries left: {retries}");
                                }
                            }
                            FailedImages.Add(imglink);
                        success:
                        Console.WriteLine($"Fetch successful: {imglink}\n");
                        }
                        if (FailedImages.Count > 0)
                        {
                            Console.WriteLine("ONE OR MORE IMAGES FAILED TO LOAD:");
                            foreach (string s in FailedImages) { Console.WriteLine(s); }
                            File.WriteAllText("FailedImages.json", JsonConvert.SerializeObject(FailedImages, Formatting.Indented));
                        }
                        break;

                }
                if (currAction == "exit") break;
                Console.WriteLine();
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            ccfg.Save(cfgpath);
        }
        public static string cfgpath => Path.Combine(Directory.GetCurrentDirectory(), "scrolerconfig.json");
        public static void ScanDoc(HtmlDocument doc, out List<PostInfo> result)
        {
            result = new List<PostInfo>();
            try
            {
                
                var clev = doc.DocumentNode.SelectNodes("//div[@class='post']");
                foreach (HtmlNode tolog in clev)
                {
                    HtmlNode authorlabel = tolog.SelectSingleNode("../../td/b/a");
                    if (authorlabel == null) continue;
                    
                    if (authorlabel.InnerHtml.Contains("JLJac") || authorlabel.InnerHtml.Contains("jamesprimate")) 
                    {
                        HtmlNode replylabel = tolog.SelectSingleNode("..//td[@valign]/div[@class='smalltext']");
                        result.Add(new PostInfo(tolog, replylabel.InnerHtml, (authorlabel.InnerText)));
                    }
                }
            }
            catch (NullReferenceException) { }
        }
        public static void WritePosts(List<PostInfo> posts, string trunkpath, string PageIndex)
        {
            if (!Directory.Exists(trunkpath)) Directory.CreateDirectory(trunkpath);
            string lastyear = "-1";
            string lastdate = "December 31";
            foreach (PostInfo pim in posts)
            {
                string[] shatter = Regex.Split(pim.Labt, "<b>|</b>");
                string postIndex;
                string year;
                string date;
                string InsertIfDateBorked = string.Empty;
                if (shatter[1].StartsWith("Reply #"))
                {
                    postIndex = (Regex.Split(shatter[1], "Reply #| on")[1]);
                }
                else postIndex = ("Original post");
                try
                {
                    year = Regex.Split(shatter[2], ", ")[1];
                    date = Regex.Split(shatter[2], ", ")[0].Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    InsertIfDateBorked = "\nDATE/YEAR PRESUMED, MIGHT BE WRONG";
                    year = lastyear;
                    date = lastdate;
                }
                
                string currtarpath = Path.Combine(trunkpath, year, date, postIndex);
                if (!Directory.Exists(currtarpath)) Directory.CreateDirectory(currtarpath);
                File.WriteAllText(Path.Combine(currtarpath, "stub.txt"), $"{pim.Author}\n{PageIndex}{InsertIfDateBorked}\n\n" + pim.relatedpost.InnerHtml);
                Console.WriteLine($"Documented post {postIndex}, from {date}, {year}, sender: {pim.Author}.");
                if (InsertIfDateBorked != string.Empty) Console.WriteLine($"Parsing date for {postIndex} has failed; previous date presumed to be true.");
                lastyear = year;
                lastdate = date;
            }
        }
        public static string WbLinkFromPageIndex(int index)
        {
            return $"https://web.archive.org/web/20210224131114/https://forums.tigsource.com/index.php?topic=25183.{index * 20}";
        }
        public static void FetchImagesFromSavedPost(string savedpost)
        {
            HtmlNode node = HtmlNode.CreateNode(savedpost);
        }
        
        public struct PostInfo
        {
            public PostInfo(HtmlNode post, string numlabel, string author)
            {
                relatedpost = post;
                Labt = numlabel;
                Author = author;
            }
            public string Author;
            public string Labt;
            public HtmlNode relatedpost;
        }
        public static scrolerCfg ccfg = new scrolerCfg();
        public class scrolerCfg
        {
            public string OutputPath = string.Empty;
            public string SourcePath = string.Empty;
            public void Save(string confpath)
            {
                try
                {
                    string tosave = JsonConvert.SerializeObject(this, Formatting.Indented);
                    File.WriteAllText(confpath, tosave);
                }
                catch (IOException ioe) { Console.WriteLine("ERROR WRITING CONFIG FILE: "); Console.WriteLine(ioe); }
                
            }
            public void Load(string confpath)
            {
                try
                {
                    if (File.Exists(confpath))
                    {
                        string toload = File.ReadAllText(confpath);
                        scrolerCfg gatheredcfg = JsonConvert.DeserializeObject<scrolerCfg>(toload);
                        this.SourcePath = gatheredcfg.SourcePath;
                        this.OutputPath = gatheredcfg.OutputPath;
                    }
                    else OutputPath = string.Empty;
                }
                catch (IOException ioe) { Console.WriteLine("ERROR READING CONFIG FILE: "); Console.WriteLine(ioe); }
                
            }
        }

    }
}
