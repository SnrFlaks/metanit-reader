using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace MetanitReader {
    class Program {
        const string url = "https://metanit.com/";

        static async Task Main() {
            AnsiConsole.Write(new FigletText("METANIT Reader\t").Centered().Color(Color.LightSlateGrey));
            AnsiConsole.Markup($"[bold]Original Website:[/] [link]{url}[/]\n\n");

            await FetchPageContent();
        }

        static async Task FetchPageContent() {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            var proxyManager = new ProxyManager(configuration);
            var httpClient = proxyManager.CreateHttpClientWithProxy();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            try {
                var page = await httpClient.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(page);
                var sections = htmlDocument.DocumentNode.SelectNodes("//div[@class='navmenu']/a");
                (var section, var sectionUrl) = SelectCollectionElement("section", sections);
                page = await httpClient.GetStringAsync($"https:{sectionUrl}");
                htmlDocument.LoadHtml(page);
                if (section != "C#") {
                    var guides = htmlDocument.DocumentNode.SelectNodes("//div[@class='navmenu']/a");
                    (var guide, var guideUrl) = SelectCollectionElement("guide", guides);
                    AnsiConsole.Markup($"[bold green]You selected: {section} | {guide}[/]\n");
                    AnsiConsole.Markup($"URL: https:{sectionUrl}{guideUrl}\n");
                }
                else {
                    var content = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='centerRight']");
                    var subsections = content.SelectNodes(".//h3");
                    var subsectionChoices = subsections.Select(h => h.InnerText.Trim());
                    var subsection = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Which [bold blue]subsection[/] do you want to open?")
                            .AddChoices(subsectionChoices)
                    );
                    var subsectionNode = subsections.FirstOrDefault(h => h.InnerText.Trim() == subsection);
                    if (subsectionNode != null) {
                        var guidesContainer = HtmlNode.CreateNode("<div></div>"); ;
                        var node = subsectionNode.NextSibling;
                        while (node != null && node.Name != "h3") {
                            if (node.Name == "p") {
                                guidesContainer.AppendChild(node.SelectSingleNode(".//a"));
                            }
                            node = node.NextSibling;
                        }
                        var guides = guidesContainer.ChildNodes;
                        (var guide, var guideUrl) = SelectCollectionElement("guide", guides);
                        AnsiConsole.Markup($"[bold green]You selected: {section} | {subsection} | {guide}[/]\n");
                        AnsiConsole.Markup($"URL: https:{sectionUrl}{guideUrl}\n");
                    }
                }
            }
            catch (HttpRequestException e) {
                AnsiConsole.Markup($"[bold red]Error: \t{e.Message}[/]\n\n");
            }
        }

        static (string, string) SelectCollectionElement(string type, HtmlNodeCollection collection) {
            var collectionInfo = GetInfoFromCollection(collection);
            var element = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Which [bold blue]{type}[/] do you want to open?")
                    .AddChoices(collectionInfo.Keys)
                );
            return (element, collectionInfo[element]);
        }

        static Dictionary<string, string> GetInfoFromCollection(HtmlNodeCollection collection) {
            Dictionary<string, string> info = [];
            foreach (var c in collection) {
                string name = c.InnerText.Trim();
                string url = c.GetAttributeValue("href", "").Trim();
                info.Add(name, url);
            }
            return info;
        }
    }
}