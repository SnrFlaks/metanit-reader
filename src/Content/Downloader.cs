using HtmlAgilityPack;
using Spectre.Console;

namespace MetanitReader {
    public class Downloader {
        private static readonly string booksDirPath = GetBooksDir();

        public static async Task DownloadContentAsync(Content content, string format, HttpClient httpClient) {
            if (content.Type == ContentType.Article) {
                await AnsiConsole.Progress()
                    .StartAsync(async ctx => {
                        var task = ctx.AddTask($"[bold green]{content.Name}[/]\n\n");
                        await DownloadPageAsync(content.Url, format, httpClient);
                        task.Increment(100);
                    });
                return;
            }
            string page = await httpClient.GetStringAsync(content.Url);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(page);
            HtmlNodeCollection chapters = htmlDocument.DocumentNode.SelectNodes("//ul[@class='filetree']/li");
            foreach (var chapter in chapters) {
                string chapterName = chapter.SelectSingleNode(".//span").InnerText;
                if (chapter.ChildNodes.Count > 1) {
                    await AnsiConsole.Progress()
                        .StartAsync(async ctx => {
                            var task = ctx.AddTask($"[bold green]{chapterName}[/]\n\n");
                            AnsiConsole.Cursor.MoveUp(1);
                            HtmlNodeCollection subchapters = chapter.SelectNodes(".//ul/li/span/a");
                            double increment = 100 / subchapters.Count;
                            foreach (var s in subchapters) {
                                string subchapterUrl = $"https:{s.GetAttributeValue("href", "").Trim()}";
                                await DownloadPageAsync(subchapterUrl, format, httpClient);
                                task.Increment(increment);
                            }
                            task.Value = 100;
                        });
                }
                else {
                    HtmlNode subchapter = chapter.SelectSingleNode(".//span/a");
                    await AnsiConsole.Progress()
                        .StartAsync(async ctx => {
                            var task = ctx.AddTask($"[bold green]{subchapter.InnerText}[/]\n\n");
                            AnsiConsole.Cursor.MoveUp(1);
                            string subchapterUrl = $"https:{subchapter.GetAttributeValue("href", "").Trim()}";
                            await DownloadPageAsync(subchapterUrl, format, httpClient);
                            task.Increment(100);
                        });
                }
            }
        }

        private static async Task DownloadPageAsync(string url, string format, HttpClient httpClient) {
            string page = await httpClient.GetStringAsync(url);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(page);
            HtmlNode content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='item center menC']");
            await (format switch {
                "pdf" => DownloadPdf(content, booksDirPath),
                "fb2" => DownloadFb2(content, booksDirPath),
                "epub" => DownloadEpub(content, booksDirPath),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format)),
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