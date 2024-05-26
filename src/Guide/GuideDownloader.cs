using HtmlAgilityPack;

namespace MetanitReader {
    public class GuideDownloader {
        private static readonly string booksDirPath = GetBooksDir();

        public static async Task DownloadGuideAsync(Guide guide, string format, HttpClient httpClient) {
            string page = await httpClient.GetStringAsync(guide.Url);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(page);
            HtmlNodeCollection chapters = htmlDocument.DocumentNode.SelectNodes("//ul[@class='filetree']/li");
            htmlDocument = new();
            HtmlNode chapter = chapters[0];
            while (chapter != null) {
                HtmlNodeCollection subchapters = chapter.SelectNodes(".//ul/li/span/a");
                if (subchapters != null) {
                    foreach (var s in subchapters) {
                        string subchapterUrl = $"https:{s.GetAttributeValue("href", "").Trim()}";
                        page = await httpClient.GetStringAsync(subchapterUrl);
                        htmlDocument.LoadHtml(page);
                        HtmlNode content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='item center menC']");
                        await (format.ToLower() switch {
                            "pdf" => DownloadPdf(content, booksDirPath),
                            "fb2" => DownloadFb2(content, booksDirPath),
                            "epub" => DownloadEpub(content, booksDirPath),
                            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format)),
                        });
                    }
                    Console.WriteLine("\n");
                }
                chapter = chapter.NextSibling;
            }
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