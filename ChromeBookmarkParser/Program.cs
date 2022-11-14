using HtmlAgilityPack;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChromeBookmarkParser;

class Program
{
    static void Main()
    {
        var downloadFolderPath = new KnownFolder(KnownFolderType.Downloads).Path;
        var bookmarks_ = "bookmarks_";
        var possibleBookmarkFiles = Directory.EnumerateFiles(downloadFolderPath, $"{bookmarks_}*.html");       
        var bookmarks = possibleBookmarkFiles.OrderByDescending(f =>
        {
            var fileName = Path.GetFileNameWithoutExtension(f);
            if (DateTime.TryParseExact(fileName[bookmarks_.Length..], "d_MM_yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDt))
            {
                return fromDt;
            }
            return default; // Files with invalid dates will be sorted last
        }).ToList();       
        var bookmarkFile = bookmarks.FirstOrDefault();
        if (bookmarks.Count != 1)
        {
            Console.WriteLine($"{bookmarks.Count} possible files found");
            Console.WriteLine($"Using most-recent file: {bookmarkFile}");
            Console.WriteLine();
        }
        var regex = new Regex(@"(\d{4})");
        var filmsList = new List<string>();
        var booksList = new List<string>();
        var doc = new HtmlDocument();
        doc.Load(bookmarkFile);
        var sn = doc.DocumentNode.LastChild.PreviousSibling;
        var t = sn.Descendants();
        var seenFilms = false;
        var seenBooks = false;
        foreach (var i in t)
        {
            if (i.Name == "dt")
            {
                var childNodes = i.ChildNodes;
                var h3 = childNodes.FindFirst("H3");
                if (h3?.InnerText == "Bookmarks bar")
                {
                    var cnFilms = h3.NextSibling.NextSibling.LastChild.LastChild.LastChild.LastChild.LastChild.LastChild.ChildNodes;
                    if (!seenFilms && cnFilms.First().InnerText == "Films")
                    {
                        var f = cnFilms.FindFirst("p").LastChild.LastChild.ChildNodes;
                        foreach (var film in f)
                        {
                            if (film.Name == "dt")
                            {
                                filmsList.Add(System.Net.WebUtility.HtmlDecode(film.FirstChild.InnerText));
                            }
                        }
                        seenFilms = true;
                    }
                }

                var cnBooks = h3?.NextSibling.NextSibling.LastChild.LastChild.ChildNodes;
                if (!seenBooks && cnBooks.First().InnerText == "Books")
                {
                    var b = cnBooks.FindFirst("p").LastChild.LastChild.ChildNodes;
                    foreach (var book in b)
                    {
                        if (book.Name == "dt")
                        {
                            booksList.Add(System.Net.WebUtility.HtmlDecode(book.FirstChild.InnerText));
                        }
                    }
                    seenBooks = true;
                }
            }

        }

        Console.WriteLine($"Reading {filmsList.Count} films");

        var years = new Dictionary<int, List<string>>();
        var unscheduledFilms = new List<string>();
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
                unscheduledFilms.Add(line);
            }
        }
        foreach (var year in years.OrderBy(y => y.Key))
        {
            Console.WriteLine();
            Console.WriteLine($"---------{year.Key}---------");
            Console.WriteLine($"{string.Join(Environment.NewLine, year.Value.OrderBy(f => f).Select(f => f))}");
        }
        if (unscheduledFilms.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"---UnscheduledFilms---");
            Console.WriteLine($"{string.Join(Environment.NewLine, unscheduledFilms.OrderBy(f => f).Select(f => f))}");
        }

        //Console.WriteLine();
        //Console.WriteLine();
        //Console.WriteLine($"Reading {booksList.Count} books");
        //foreach (var book in booksList.OrderBy(b => b))
        //{
        //    Console.WriteLine(book);
        //}
        Console.ReadLine();
    }
}
