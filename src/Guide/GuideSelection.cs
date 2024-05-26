using HtmlAgilityPack;
using Spectre.Console;

namespace MetanitReader {
    public class GuideSelection {
        const string url = "https://metanit.com/";

        public static async Task<Guide?> SelectGuideAsync(HttpClient httpClient) {
            try {
                string page = await httpClient.GetStringAsync(url);
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(page);
                HtmlNodeCollection sections = htmlDocument.DocumentNode.SelectNodes("//div[@class='navmenu']/a");
                (string section, string sectionUrl) = SelectCollectionElement("section", sections);
                page = await httpClient.GetStringAsync($"https:{sectionUrl}");
                htmlDocument.LoadHtml(page);
                if (section == "MongoDB" || section == "Swift") {
                    string name = htmlDocument.DocumentNode.SelectSingleNode("//h1").InnerText;
                    return new Guide(name, $"https:{sectionUrl}");
                }
                else if (section == "C#") {
                    HtmlNodeCollection subsections = htmlDocument.DocumentNode.SelectNodes("//div[@class='centerRight']/h3");
                    (string subsection, string subsectionUrl) = SelectCollectionElement("subsection", subsections);
                    HtmlNode? subsectionNode = subsections.SingleOrDefault(h => h.InnerText.Trim() == subsection);
                    if (subsectionNode != null) {
                        HtmlNode guidesContainer = HtmlNode.CreateNode("<div></div>"); ;
                        HtmlNode node = subsectionNode.NextSibling;
                        while (node != null && node.Name != "h3") {
                            if (node.Name == "p") {
                                guidesContainer.AppendChild(node.SelectSingleNode(".//a"));
                            }
                            node = node.NextSibling;
                        }
                        HtmlNodeCollection guides = guidesContainer.ChildNodes;
                        (string guide, string guideUrl) = SelectCollectionElement("guide", guides);
                        return new Guide(guide, $"https:{sectionUrl}{guideUrl}");
                    }
                }
                else {
                    HtmlNodeCollection guides = htmlDocument.DocumentNode.SelectNodes("//div[@class='navmenu']/a");
                    (string guide, string guideUrl) = SelectCollectionElement("guide", guides);
                    return new Guide(guide, $"https:{guideUrl}");
                }
            }
            catch (HttpRequestException e) {
                AnsiConsole.Markup($"[bold red]Error: \t{e.Message}[/]\n\n");
            }
            catch (Exception ex) {
                AnsiConsole.Markup($"[bold red]Unexpected Error:[/] {ex.Message}\n\n");
            }
            return null;
        }

        static (string, string) SelectCollectionElement(string type, HtmlNodeCollection collection) {
            Dictionary<string, string> collectionInfo = GetInfoFromCollection(collection);
            var element = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Which [bold blue]{type}[/] do you want to open?")
                    .MoreChoicesText($"[grey](Move up and down to reveal more {type}s)[/]")
                    .AddChoices(collectionInfo.Keys)
                );
            return (element, collectionInfo[element]);
        }

        public static Dictionary<string, string> GetInfoFromCollection(HtmlNodeCollection collection) {
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