using HtmlAgilityPack;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Spectre.Console;

namespace MetanitReader {
    public class Downloader {
        private static readonly string booksDirPath = GetBooksDir();

        public static async Task DownloadContentAsync(Content content, string format, HttpClient httpClient) {
            string page = await httpClient.GetStringAsync(content.Url);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(page);
            if (content.Type == ContentType.Article) {
                await DownloadPageAsync(htmlDocument, format);
                return;
            }
            HtmlNodeCollection chapters = htmlDocument.DocumentNode.SelectNodes("//ul[@class='filetree']/li");
            htmlDocument = new();
            HtmlNode chapter = chapters[0];
            HtmlNode chapterNameNode;
            string chapterName;
            HtmlNodeCollection subchapters;
            while (chapter != null) {
                chapterNameNode = chapter.SelectSingleNode(".//span");
                subchapters = chapter.SelectNodes(".//ul/li/span/a");
                if (chapterNameNode == null) {
                    chapter = chapter.NextSibling;
                    continue;
                }
                chapterName = chapterNameNode.InnerText;
                subchapters = chapter.SelectNodes(".//ul/li/span/a");
                double increment = 100.0 / subchapters.Count;
                await AnsiConsole.Progress()
                    .StartAsync(async ctx => {
                        var task = ctx.AddTask($"[bold green]{chapterName}[/]\n\n");
                        AnsiConsole.Cursor.MoveUp(1);
                        while (!ctx.IsFinished) {
                            if (subchapters.Count == 1) {
                                string subchapterUrl = $"https:{subchapters[0].GetAttributeValue("href", "").Trim()}";
                                page = await httpClient.GetStringAsync(subchapterUrl);
                                htmlDocument.LoadHtml(page);
                                await DownloadPageAsync(htmlDocument, format);
                            }
                            else {
                                foreach (var s in subchapters) {
                                    string subchapterUrl = $"https:{s.GetAttributeValue("href", "").Trim()}";
                                    page = await httpClient.GetStringAsync(subchapterUrl);
                                    htmlDocument.LoadHtml(page);
                                    HtmlNode content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='item center menC']");

                                    await (format switch {
                                        "pdf" => DownloadPdf(content, booksDirPath),
                                        "fb2" => DownloadFb2(content, booksDirPath),
                                        "epub" => DownloadEpub(content, booksDirPath),
                                        _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format)),
                                    });
                                    task.Increment(increment);
                                }
                            }
                            task.Value = 100;
                        }
                    });
                chapter = chapter.NextSibling;
            }
        }

        private static async Task DownloadPageAsync(HtmlDocument htmlDocument, string format) {
            await AnsiConsole.Progress()
                .StartAsync(async ctx => {
                    HtmlNode content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='item center menC']");
                    var task = ctx.AddTask($"[bold green]{content.SelectSingleNode(".//h1").InnerText}[/]\n\n");
                    while (!ctx.IsFinished) {
                        await (format switch {
                            "pdf" => DownloadPdf(content, booksDirPath),
                            "fb2" => DownloadFb2(content, booksDirPath),
                            "epub" => DownloadEpub(content, booksDirPath),
                            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format)),
                        });
                        task.Increment(1);
                    }
                    task.Value = 100;
                });
        }

        private static string GetBooksDir() {
            string currentDir = Directory.GetCurrentDirectory();
            string booksDirPath = Path.Combine(currentDir, "books");
            if (!Directory.Exists(booksDirPath)) {
                Directory.CreateDirectory(booksDirPath);
            }
            return booksDirPath;
        }

        private static async Task DownloadPdf(HtmlNode content, string booksDirPath) {

        }

        private static async Task DownloadFb2(HtmlNode content, string booksDirPath) {

        }

        private static async Task DownloadEpub(HtmlNode content, string booksDirPath) {

        }
    }
}