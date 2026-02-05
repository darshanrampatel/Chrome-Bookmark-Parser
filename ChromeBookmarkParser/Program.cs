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
        var folderPath = new KnownFolder(KnownFolderType.Documents).Path;
        var bookmarks_ = "bookmarks_";
        var possibleBookmarkFiles = Directory.EnumerateFiles(folderPath, $"{bookmarks_}*.html");
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
            if (bookmarkFile == default)
            {
                Console.WriteLine($"Could not find a suitable file in {folderPath}");
                return;
            }
            Console.WriteLine($"Using most-recent file: {bookmarkFile}");
            Console.WriteLine();
        }
        var regex = new Regex(@"(\d{4})");
        var filmsList = new List<string>();
        var booksList = new List<string>();
        var doc = new HtmlDocument();
        doc.Load(bookmarkFile);

        // Find the "Films" folder using XPath - much more robust than chained navigation
        var filmsFolder = doc.DocumentNode.SelectSingleNode("//h3[text()='Films']");
        if (filmsFolder != null)
        {
            // The <DL> containing the bookmarks is the next sibling element after the H3
            var filmsDl = filmsFolder.NextSibling;
            while (filmsDl != null && filmsDl.Name != "dl")
            {
                filmsDl = filmsDl.NextSibling;
            }

            if (filmsDl != null)
            {
                var filmLinks = filmsDl.SelectNodes(".//dt/a");
                if (filmLinks != null)
                {
                    foreach (var film in filmLinks)
                    {
                        filmsList.Add(System.Net.WebUtility.HtmlDecode(film.InnerText));
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Could not find 'Films' folder in bookmarks");
        }

        // Find the "Books" folder using XPath
        var booksFolder = doc.DocumentNode.SelectSingleNode("//h3[text()='Books']");
        if (booksFolder != null)
        {
            var booksDl = booksFolder.NextSibling;
            while (booksDl != null && booksDl.Name != "dl")
            {
                booksDl = booksDl.NextSibling;
            }

            if (booksDl != null)
            {
                var bookLinks = booksDl.SelectNodes(".//dt/a");
                if (bookLinks != null)
                {
                    foreach (var book in bookLinks)
                    {
                        booksList.Add(System.Net.WebUtility.HtmlDecode(book.InnerText));
                    }
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
        var duplicateFilms = years.SelectMany(y => y.Value).GroupBy(f => f).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateFilms.Any())
        {
            Console.WriteLine($"{nameof(years)} contains duplicates!");
            Console.WriteLine($"{string.Join(Environment.NewLine, duplicateFilms.OrderBy(f => f))}");
        }
        else
        {
            foreach (var year in years.OrderBy(y => y.Key))
            {
                Console.WriteLine();
                Console.WriteLine($"---------{year.Key}---------");
                Console.WriteLine($"{string.Join(Environment.NewLine, year.Value.OrderBy(f => f).Select(f => f))}");
            }
        }
        if (unscheduledFilms.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"---UnscheduledFilms---");
            var duplicateUnscheduledFilms = unscheduledFilms.GroupBy(f => f).Where(g => g.Count() > 1).Select(g => g.Key);
            if (duplicateUnscheduledFilms.Any())
            {
                Console.WriteLine($"{nameof(unscheduledFilms)} contains duplicates!");
                Console.WriteLine($"{string.Join(Environment.NewLine, duplicateUnscheduledFilms.OrderBy(f => f))}");
            }
            else
            {
                Console.WriteLine($"{string.Join(Environment.NewLine, unscheduledFilms.OrderBy(f => f).Select(f => f))}");
            }
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
