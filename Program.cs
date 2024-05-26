using HtmlAgilityPack;
using MetanitReader;
using Spectre.Console;

const string url = "https://metanit.com/";

AnsiConsole.Write(new FigletText("METANIT Reader\t").Centered().Color(Color.LightSlateGrey));
AnsiConsole.Markup($"[bold]Original Website:[/] [link]{url}[/]\n\n");


HttpClient httpClient = ProxyManager.Instance.CreateHttpClientWithProxy();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

Guide? guide = await GuideSelection.SelectGuideAsync(httpClient);
if (guide != null) {
    AnsiConsole.Markup($"[bold]You selected: [green]{guide.Name}[/][/]\n");
    AnsiConsole.Markup($"[bold]URL:[/] {guide.Url}\n\n");
    if (AnsiConsole.Confirm("Do you want to download the guide as a book?")) {
        var format = AnsiConsole.Prompt(
           new SelectionPrompt<string>()
               .Title("")
               .AddChoices(["PDF", "FB2", "EPUB"]));
        await GuideDownloader.DownloadGuideAsync(guide, format, httpClient);
    }
}
else {
    AnsiConsole.Markup("[bold red]Error occurred while selecting the guide.[/]");
}