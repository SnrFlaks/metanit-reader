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
                if (sections != null && sections.Count > 0) {
                    var sectionInfo = GetInfoFromCollection(sections);
                    var section = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Which [bold blue]section[/] do you want to open?")
                            .AddChoices(sectionInfo.Keys)
                        );
                    string sectionUrl = sectionInfo[section];
                    if (section != "C#") {
                        page = await httpClient.GetStringAsync("https:" + sectionUrl);
                        htmlDocument.LoadHtml(page);
                        var guides = htmlDocument.DocumentNode.SelectNodes("//div[@class='navmenu']/a");
                        var guideInfo = GetInfoFromCollection(guides);
                        var guide = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Which [bold blue]guide[/] do you want to open?")
                                .AddChoices(guideInfo.Keys)
                            );
                        string guideUrl = guideInfo[guide];
                        AnsiConsole.Markup($"[bold green]You selected: {section} | {guide}[/]\n");
                        AnsiConsole.Markup($"URL: {guideUrl}\n");
                    }
                    else {

                    }
                }
                else {
                    Console.WriteLine("No list items found.");
                }
            }
            catch (HttpRequestException e) {
                AnsiConsole.Markup($"[bold red]Error: \t{e.Message}[/]\n\n");
            }
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