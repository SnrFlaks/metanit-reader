using HtmlAgilityPack;
using Spectre.Console;

namespace MetanitReader {
    public class Downloader {
        private static readonly string booksDirPath = GetBooksDir();

        public static async Task DownloadContentAsync(Content content, string format, HttpClient httpClient) {
            List<HtmlNode> contentList = [];
            await AnsiConsole.Status()
                .StartAsync("Download chapters...", async ctx => {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    if (content.Type == ContentType.Article) {
                        AnsiConsole.MarkupLine($"[bold green]{content.Name}[/]");
                        HtmlNode pageContent = await GetPageContentAsync(content.Url, httpClient);
                        contentList.Add(pageContent);
                    }
                    else {
                        string page = await httpClient.GetStringAsync(content.Url);
                        HtmlDocument htmlDocument = new();
                        htmlDocument.LoadHtml(page);
                        HtmlNodeCollection chapters = htmlDocument.DocumentNode.SelectNodes("//ul[@class='filetree']/li");
                        foreach (var chapter in chapters) {
                            string chapterName = chapter.SelectSingleNode(".//span").InnerText;
                            if (chapter.ChildNodes.Count > 1) {
                                AnsiConsole.MarkupLine($"[bold green]{chapterName}[/]");
                                HtmlNodeCollection subchapters = chapter.SelectNodes(".//ul/li/span/a");
                                double increment = 100 / subchapters.Count;
                                foreach (var s in subchapters) {
                                    string subchapterUrl = $"https:{s.Attributes["href"].Value}";
                                    HtmlNode pageContent = await GetPageContentAsync(subchapterUrl, httpClient);
                                    contentList.Add(pageContent);
                                }
                            }
                            else {
                                HtmlNode subchapter = chapter.SelectSingleNode(".//span/a");
                                AnsiConsole.MarkupLine($"[bold green]{chapterName}[/]");
                                string subchapterUrl = $"https:{subchapter.Attributes["href"].Value}";
                                HtmlNode pageContent = await GetPageContentAsync(subchapterUrl, httpClient);
                                contentList.Add(pageContent);
                            }
                        }
                    }
                });
            await (format switch {
                "pdf" => DownloadPdf(contentList, booksDirPath),
                "fb2" => DownloadFb2(contentList, booksDirPath),
                "epub" => DownloadEpub(contentList, booksDirPath),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format)),
            });
        }

        private static async Task<HtmlNode> GetPageContentAsync(string title, string url, HttpClient httpClient) {
            string page = await httpClient.GetStringAsync(url);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(page);
            HtmlNode content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='item center menC']");
            return content;
        }

        private static async Task<HtmlNode> GetPageContentAsync(string url, HttpClient httpClient) {
            string page = await httpClient.GetStringAsync(url);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(page);
            HtmlNode content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='item center menC']");
            return content;
        }

        private static string GetBooksDir() {
            string currentDir = Directory.GetCurrentDirectory();
            string booksDirPath = Path.Combine(currentDir, "books");
            if (!Directory.Exists(booksDirPath)) {
                Directory.CreateDirectory(booksDirPath);
            }
            return booksDirPath;
        }

        private static async Task DownloadPdf(List<HtmlNode> contentList, string booksDirPath) {

        }

        private static async Task DownloadFb2(List<HtmlNode> contentList, string booksDirPath) {

        }

        private static async Task DownloadEpub(List<HtmlNode> contentList, string booksDirPath) {

        }
    }
}