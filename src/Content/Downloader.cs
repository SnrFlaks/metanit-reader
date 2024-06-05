using System.Xml;
using HtmlAgilityPack;
using MetanitReader.FictionBook;
using Spectre.Console;

namespace MetanitReader {
    public class Downloader {
        private static readonly string booksDirPath = GetBooksDir();

        public static async Task DownloadContentAsync(Content content, string format) {
            HttpClient httpClient = HttpManager.Instance.GetHttpClient();
            List<Content> contentList = [];
            await AnsiConsole.Status()
                .StartAsync("Download chapters...", async ctx => {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    if (content.Type == ContentType.Article) {
                        AnsiConsole.MarkupLine($"[bold green]{content.Name}[/]");
                        HtmlNode pageContent = await GetPageContentAsync(content.Url, httpClient);
                        contentList.Add(new(content.Url, pageContent));
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
                                foreach (var s in subchapters) {
                                    string subchapterUrl = $"https:{s.Attributes["href"].Value}";
                                    HtmlNode pageContent = await GetPageContentAsync(subchapterUrl, httpClient);
                                    contentList.Add(new(content.Url, pageContent));
                                }
                                continue;
                            }
                            else {
                                HtmlNode subchapter = chapter.SelectSingleNode(".//span/a");
                                AnsiConsole.MarkupLine($"[bold green]{chapterName}[/]");
                                string subchapterUrl = $"https:{subchapter.Attributes["href"].Value}";
                                HtmlNode pageContent = await GetPageContentAsync(subchapterUrl, httpClient);
                                contentList.Add(new(content.Url, pageContent));
                            }
                        }
                    }
                });
            // await AnsiConsole.Status()
            //     .StartAsync("Preparing your fb2 book...", async ctx => {
            //         ctx.Spinner(Spinner.Known.Dots);
            //         ctx.SpinnerStyle(Style.Parse("green"));
            await (format switch {
                "fb2" => DownloadFb2(content, contentList, booksDirPath),
                "epub" => DownloadEpub(contentList, booksDirPath),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format)),
            });
            // });
        }

        private static async Task<HtmlNode> GetPageContentAsync(string url, HttpClient httpClient) {
            var page = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(page);
            var nodesToRemove = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'socBlock') or contains(@class, 'nav')]");
            if (nodesToRemove != null) {
                foreach (var node in nodesToRemove) {
                    node.Remove();
                }
            }
            var content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='item center menC']");
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

        private static async Task DownloadFb2(Content content, List<Content> contentList, string booksDirPath) {
            XmlDocument doc = await FictionBook.Generator.GenerateDocumentAsync(content, contentList);
            string filePath = Path.Combine(booksDirPath, $"{content.Name}.fb2");
            doc.Save(filePath);
        }

        private static Task DownloadEpub(List<Content> contentList, string booksDirPath) {
            return Task.CompletedTask;
        }
    }
}