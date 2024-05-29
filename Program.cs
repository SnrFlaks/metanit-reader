using MetanitReader;
using Spectre.Console;

const string url = "https://metanit.com/";

AnsiConsole.Write(new FigletText("METANIT Reader\t").Centered().Color(Color.LightSlateGrey));
AnsiConsole.Markup($"[bold]Original Website:[/] [link]{url}[/]\n\n");


HttpClient httpClient = HttpManager.Instance.GetHttpClient();

// while (true) {
//     if (!AnsiConsole.Confirm("Would you like to select a content for download?")) {
//         break;
//     }

Content? content = await Selection.SelectContentAsync(httpClient);
if (content != null) {
    AnsiConsole.Markup($"[bold]You selected: [green]{content.Name}[/][/]\n");
    AnsiConsole.Markup($"[bold]URL:[/] {content.Url}\n\n");
    if (AnsiConsole.Confirm("Do you want to download the content as a book?")) {
        var format = AnsiConsole.Prompt(
           new SelectionPrompt<string>()
               .Title("")
               .AddChoices(["FB2", "EPUB"]));
        await Downloader.DownloadContentAsync(content, format.ToLower());
    }
}
else {
    AnsiConsole.Markup("[bold red]Error occurred while selecting the content.[/]");
}
// }