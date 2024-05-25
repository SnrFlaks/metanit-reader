using HtmlAgilityPack;
using Spectre.Console;

const string url = "https://metanit.com/";

AnsiConsole.Write(new FigletText("METANIT Reader\t").Centered().Color(Color.LightSlateGrey));
AnsiConsole.Markup($"[bold]Original Website:[/] [link]{url}[/]\n\n");

await FetchPageContent();

static async Task FetchPageContent() {
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
    try {
        var page = await httpClient.GetStringAsync(url);
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(page);
        var sections = htmlDocument.DocumentNode.SelectNodes("//ul[@class='mainmenu']/li");
        if (sections != null && sections.Count > 0) {
            sections[^1].Remove();
            sections = htmlDocument.DocumentNode.SelectNodes("//ul[@class='mainmenu']/li/a");
            List<(string Name, string Url)> sectionInfo = [];
            List<string> sectionNames = [];
            foreach (var s in sections) {
                string name = s.InnerText.Trim();
                string url = s.GetAttributeValue("href", "").Trim();
                sectionInfo.Add((name, url));
                sectionNames.Add(name);
            }
            var section = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which [bold blue]section[/] do you want to open?")
                    .AddChoices(sectionNames)
                );
            string sectionUrl = sectionInfo.Find(x => x.Name == section).Url;
            if (section != "C#") {
                page = await httpClient.GetStringAsync("https:" + sectionUrl);
                htmlDocument.LoadHtml(page);
                var guides = htmlDocument.DocumentNode.SelectNodes("//div[@class='navmenu']/a");
                List<(string Name, string Url)> guideInfo = [];
                List<string> guideNames = [];
                foreach (var g in guides) {
                    string name = g.InnerText.Trim();
                    string url = g.GetAttributeValue("href", "").Trim();
                    guideInfo.Add((name, url));
                    guideNames.Add(name);
                }
                var guide = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Which [bold blue]guide[/] do you want to open?")
                        .AddChoices(guideNames)
                    );
                string guideUrl = guideInfo.Find(x => x.Name == guide).Url;
                AnsiConsole.Markup($"[bold green]You selected: {section} | {guide}[/]\n");
                AnsiConsole.Markup($"URL: {guideUrl}\n");
            }
        }
        else {
            Console.WriteLine("No list items found.");
        }
    }
    catch (HttpRequestException e) {
        AnsiConsole.Markup("[bold red]Error fetching page content:[/]\n");
        AnsiConsole.WriteException(e);
    }
}