using HtmlAgilityPack;
using Spectre.Console;

namespace MetanitReader {
    public class Selection {
        const string url = "https://metanit.com/";

        public static async Task<Content?> SelectContentAsync(HttpClient httpClient) {
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
                    return new Content(name, $"https:{sectionUrl}", ContentType.Guide);
                }
                else if (section == "C#") {
                    HtmlNodeCollection subsections = htmlDocument.DocumentNode.SelectNodes("//div[@class='centerRight']/h3");
                    (string subsection, _) = SelectCollectionElement("subsection", subsections);
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
                        return new Content(guide, $"https:{sectionUrl}{guideUrl}", ContentType.Guide);
                    }
                }
                else {
                    HtmlNodeCollection guides = htmlDocument.DocumentNode.SelectNodes("//div[@class='navmenu']/a");
                    (string guide, string guideUrl) = SelectCollectionElement("guide", guides);
                    if (guide == "Ассемблер. Статьи.") {
                        page = await httpClient.GetStringAsync($"https:{guideUrl}");
                        htmlDocument.LoadHtml(page);
                        HtmlNodeCollection articles = htmlDocument.DocumentNode.SelectNodes(".//ul[@class='contpage']/li/p/a");
                        (string article, string articleUrl) = SelectCollectionElement("articles", articles);
                        return new Content(article, $"https:{guideUrl}{articleUrl}", ContentType.Article);
                    }
                    return new Content(guide, $"https:{guideUrl}", ContentType.Guide);
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
                HtmlAttribute hrefAttribute = c.Attributes["href"];
                string url = hrefAttribute != null ? hrefAttribute.Value : string.Empty;
                if (!string.IsNullOrEmpty(name) && !info.ContainsKey(name)) {
                    info.Add(name, url);
                }
            }
            return info;
        }
    }
}