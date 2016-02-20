using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HtmlAgilityPack;
using NostroyParsingLib.DataTypes;

namespace NostroyParsingLib
{
    public static class Initialize
    {
        public static int DOP { private get; set; }

        private static Task<List<string>> GetPages
        {
            get
            {
                return Task.Run(async () =>
                {
                    var pages = new List<string>();
                    try
                    {
                        var doc = new HtmlDocument();
                        var resp = await new WebClient().DownloadStringTaskAsync("http://reestr.nostroy.ru/reestr");
                        doc.LoadHtml(resp);

                        var lastStr =
                            doc.DocumentNode.Descendants("ul")
                                .First(
                                    x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "pagination")
                                .Descendants("li")
                                .Last()
                                .Descendants("a")
                                .First(
                                    x =>
                                        Regex.IsMatch(x.GetAttributeValue("href", string.Empty),
                                            @"\/reestr\?.+page\=\d+"))
                                .GetAttributeValue("href", string.Empty);

                        var lastPage = int.Parse(Regex.Match(lastStr, @"\d+$").Value);

                        //Parsing pages

                        for (; lastPage > 0; lastPage--)
                            pages.Add($"http://reestr.nostroy.ru/reestr?page={lastPage}");
                    }
                    catch (Exception ex)
                    {
                        Informer.RaiseOnResultReceived(ex);
                    }

                    return pages;
                });
            }
        }

        public static async Task MainCycle()
        {
            var pages = await GetPages;
            if (pages.Count == 0)
            {
                Informer.RaiseOnResultReceived("Something going wrong");
                return;
            }

            //Parsing pages
            var allCount = 0;
            var count = pages.Count;
            await pages.Where(x => !string.IsNullOrEmpty(x)).ForEachAsync(DOP, async page =>
            {
                var list = await GetRows(page);

                await list.Where(x => !string.IsNullOrEmpty(x)).ForEachAsync(list.Count, async row =>
                {
                    var result = await ParsingRows(row);
                    QueueHelper.CollectionSaver = new List<MainType> {result};
                });

                allCount += list.Count;
                Informer.RaiseOnResultReceived($"Left {--count} pages / processed => {allCount} rows");
            });

            //Serialized MenuCollection
            var mi = new MainCollection {MainTypeList = QueueHelper.CollectionSaver};
            using (var fs = new FileStream("MainCollection.xml", FileMode.OpenOrCreate))
                new XmlSerializer(typeof (MainCollection)).Serialize(fs, mi);

            //Serialized ErrorPage          
            if (QueueHelper.PageError.Any())
                using (var fs = new FileStream("ErrorPage.xml", FileMode.OpenOrCreate))
                    new XmlSerializer(typeof (List<string>)).Serialize(fs, QueueHelper.PageError);

            //Serialized ErrorRow
            if (QueueHelper.RowError.Any())
                using (var fs = new FileStream("ErrorRow.xml", FileMode.OpenOrCreate))
                    new XmlSerializer(typeof (MainCollection)).Serialize(fs, QueueHelper.RowError);
        }

        private static async Task<MainType> ParsingRows(string link)
        {
            try
            {
                var doc = new HtmlDocument();
                var respBytes = await new WebClient().DownloadDataTaskAsync(link);
                var resp = Encoding.UTF8.GetString(respBytes);
                doc.LoadHtml(resp);

                var table =
                    doc.DocumentNode.Descendants("table")
                        .First(x => x.Attributes.Contains("class") &&
                                    x.Attributes["class"].Value == "items table")
                        .Descendants("td").ToList();

                var ExDay = table[7].InnerText;
                var Address = table[13].InnerText.Replace("&quot;", "\"");
                var RegDay = table[6].InnerText.Replace("&quot;", "\"");
                var SRO = table[0].InnerText.Replace("&quot;", "\"");
                var ShortName = table[3].InnerText.Replace("&quot;", "\"");
                var INN = table[10].InnerText.Replace("&quot;", "\"");
                var OldPhone = table[12].InnerText.Replace("&quot;", "\"");
                var Phone = FormatTel(OldPhone);
                var FIO = table[14].InnerText.Replace("&quot;", "\"");
                var Position = GetPosition(ref FIO);
                var status = table[4].InnerText.Replace("&quot;", "\"") == "Является членом"
                    ? Status.Member
                    : Status.Exclude;

                return new MainType
                {
                    FIO = FIO,
                    INN = INN,
                    Phone = Phone,
                    ShortName = ShortName,
                    SRO = SRO,
                    Status = status,
                    ExDate = ExDay,
                    RegDat = RegDay,
                    Position = Position,
                    OldPhone = OldPhone,
                    Address = Address
                };
            }
            catch
            {
                QueueHelper.RowError.Add(link);
                return new MainType();
            }
        }

        private static string FormatTel(string t)
        {
            var newTel = string.Empty;
            t = Regex.Replace(t.Replace(" добавочный 01", ",").Replace(";", ","), @"[\(\)\-\s(тел.)(тел./факс:)]",
                string.Empty);
            var arr = Regex.Matches(t, @"\+?\d+");

            foreach (var y in from object z in arr select z.ToString())
            {
                var x = y;
                if (Regex.IsMatch(x, @"^\+?\d{8,}"))
                {
                    x = Regex.Replace(x, "^8", "+7");
                    if (!x.StartsWith("+7"))
                        x = $"+7{x}";
                }

                newTel += $"{x},";
            }

            return Regex.Replace(newTel, @"\,$", string.Empty);
        }

        private static string GetPosition(ref string FIO)
        {
            var list = new List<string>
            {
                "генеральный директор",
                "исполнительный директор",
                "ген. директор",
                "директор",
                "председатель",
                "начальник",
                "ген.директор",
                "конкурсный управляющий",
                "президент"
            };

            foreach (var x in list)
            {
                if (!Regex.IsMatch(FIO, x, RegexOptions.IgnoreCase)) continue;

                FIO = Regex.Replace(FIO, $"{x} ", string.Empty, RegexOptions.IgnoreCase);
                return x;
            }
            return string.Empty;
        }

        private static async Task<List<string>> GetRows(string link)
        {
            try
            {
                var doc = new HtmlDocument();
                var respBytes = await new WebClient().DownloadDataTaskAsync(link);
                var resp = Encoding.UTF8.GetString(respBytes);
                doc.LoadHtml(resp);

                var list = doc.DocumentNode.Descendants("tr")
                    .Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "sro-link")
                    .Select(x => $"http://reestr.nostroy.ru{x.GetAttributeValue("rel", string.Empty)}");
                return list.Distinct().ToList();
            }
            catch (Exception)
            {
                QueueHelper.PageError.Add(link);
                return new List<string>();
            }
        }
    }
}