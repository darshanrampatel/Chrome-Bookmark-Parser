using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChromeBookmarkParser
{
    class Program
    {
        static void Main()
        {
            var downloadFolderPath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), @"Downloads");
            var bookmarks = Directory.EnumerateFiles(downloadFolderPath, "bookmarks_*.html");
            var bookmarksCounts = bookmarks.Count();
            if (bookmarksCounts != 1)
            {
                Console.WriteLine($"ERROR: {bookmarks.Count()} possible files found");
                Console.WriteLine($"Please ensure there is {(bookmarksCounts == 0 ? "" : "only ")}one possible file and try again");
            }
            else
            {
                var regex = new Regex(@"(\d{4})");
                var filmsList = new List<string>();
                var doc = new HtmlDocument();
                doc.Load(bookmarks.First());
                var sn = doc.DocumentNode.LastChild.PreviousSibling;
                var t = sn.Descendants();
                foreach (var i in t)
                {
                    if (i.Name == "dt")
                    {
                        var childNodes = i.ChildNodes;
                        var h3 = childNodes.FindFirst("H3");
                        if (h3.InnerText == "Bookmarks bar")
                        {
                            var cn = h3.NextSibling.NextSibling.LastChild.LastChild.LastChild.LastChild.LastChild.LastChild.ChildNodes;
                            if (cn.First().InnerText == "Films")
                            {
                                var f = cn.FindFirst("p").LastChild.LastChild.ChildNodes;
                                foreach (var film in f)
                                {
                                    if (film.Name == "dt")
                                    {
                                        filmsList.Add(System.Net.WebUtility.HtmlDecode(film.FirstChild.InnerText));
                                    }
                                }
                                break;
                            }
                        }
                    }

                }

                Console.WriteLine($"Reading {filmsList.Count} films");

                var years = new Dictionary<int, List<string>>();
                foreach (var line in filmsList)
                {
                    var dateFound = regex.Match(line.Trim());
                    if (dateFound.Success && int.TryParse(dateFound.Value, out int year))
                    {
                        if (!years.ContainsKey(year))
                        {
                            years.Add(year, new List<string>());
                        }
                        years[year].Add(line.Replace($"({dateFound})", ""));
                    }
                    else
                    {
                        Console.WriteLine($"Ignoring: {line}");
                    }
                }
                foreach (var year in years.OrderBy(y => y.Key))
                {
                    Console.WriteLine();
                    Console.WriteLine($"---{year.Key}---");
                    Console.WriteLine($"{string.Join(Environment.NewLine, year.Value.OrderBy(f => f).Select(f => f))}");
                }
            }
            Console.ReadLine();
        }
    }
}
