using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using PuppeteerSharp;

namespace MetanitReader {
    partial class Fb2Generator {
        public static async Task<XmlDocument> GenerateFb2(Content content, List<Content> contentList) {
            XmlDocument doc = new();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(xmlDeclaration);
            XmlElement fB = doc.CreateElement("FictionBook");
            doc.AppendChild(fB);
            XmlElement description = doc.CreateElement("description");
            fB.AppendChild(description);
            XmlElement titleInfo = doc.CreateElement("title-info");
            description.AppendChild(titleInfo);
            if (content.Name != null) {
                XmlElement bookTitle = doc.CreateElement("book-title");
                bookTitle.InnerXml = content.Name;
                titleInfo.AppendChild(bookTitle);
            }
            XmlElement author = doc.CreateElement("author");
            titleInfo.AppendChild(author);
            XmlElement firstName = doc.CreateElement("first-name");
            firstName.InnerXml = "METANIT";
            author.AppendChild(firstName);
            XmlElement body = doc.CreateElement("body");
            fB.AppendChild(body);
            var stopwatch = Stopwatch.StartNew();
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            foreach (var c in contentList) {
                await ParseHtmlNodeAsync(c, body, fB, doc, page);
            }
            await browser.CloseAsync();
            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} milliseconds");
            return doc;
        }

        private static async Task ParseHtmlNodeAsync(Content content, XmlElement body, XmlElement fB, XmlDocument doc, IPage page) {
            if (content.Node == null) {
                return;
            }
            HtmlNode node = content.Node;
            if (node.Name == "h1" || node.Name == "h2" || node.Name == "h3") {
                AddTitle(node, body, doc, node.Name);
            }
            else if (node.Name == "p") {
                AddParagraph(node, body, doc);
            }
            else if (node.Name == "ul") {
                AddList(node, body, doc);
            }
            else if (node.Name == "pre") {
                if (node.HasClass("browser")) {
                    AddCode(content, node, body, doc);
                }
                else {
                    await AddCodeImageAsync(node, body, fB, doc, page);
                }
            }
            else if (node.Name == "img") {
                await AddImageAsync(content, body, fB, doc);
            }
            else {
                foreach (var childNode in node.ChildNodes) {
                    await ParseHtmlNodeAsync(new Content(content.Url, childNode), body, fB, doc, page);
                }
            }
        }

        private static void AddTitle(HtmlNode node, XmlElement body, XmlDocument doc, string level) {
            XmlElement element;
            XmlElement p = doc.CreateElement("p");
            if (level == "h1") {
                element = doc.CreateElement("title");
            }
            else {
                element = doc.CreateElement("subtitle");
                XmlElement strong = doc.CreateElement("strong");
                strong.InnerXml = node.InnerText.Replace("&", "&amp;");
                p.AppendChild(strong);
                element.AppendChild(p);
                body.AppendChild(element);
                return;
            }
            p.InnerXml = node.InnerText.Replace("&", "&amp;");
            element.AppendChild(p);
            body.AppendChild(element);
        }

        private static void AddParagraph(HtmlNode node, XmlElement body, XmlDocument doc) {
            var p = doc.CreateElement("p");
            foreach (var childNode in node.ChildNodes) {
                try {
                    if (childNode.Name == "a") {
                        p.InnerXml += childNode.InnerText.Replace("&", "&amp;");
                    }
                    else if (childNode.Name == "class=\"b\"" || (childNode.Name == "span" && childNode.Attributes["class"]?.Value == "b")) {
                        var strongElement = doc.CreateElement("strong");
                        strongElement.InnerXml = childNode.InnerText.Replace("&", "&amp;");
                        p.AppendChild(strongElement);
                    }
                    else if (childNode.Name == "br") {
                        continue;
                    }
                    else {
                        p.InnerXml += childNode.OuterHtml.Replace("&", "&amp;");
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Paragraph adding ERROR: {ex.Message}");
                    Console.WriteLine($"Node name: {childNode.Name}");
                    Console.WriteLine($"Node content: {childNode.OuterHtml}\n");
                }
            }
            body.AppendChild(p);
        }

        private static void AddList(HtmlNode node, XmlElement body, XmlDocument doc) {
            XmlElement list = doc.CreateElement("ul");
            foreach (var liNode in node.SelectNodes("li")) {
                XmlElement li = doc.CreateElement("li");
                li.InnerXml = liNode.InnerText.Replace("&", "&amp;");
                list.AppendChild(li);
            }
            body.AppendChild(list);
        }

        private static readonly string[] separators = ["\r\n", "\r", "\n"];

        [GeneratedRegex(@"(?:<[^ \/>][^>]*>[^<]*<\/[a-zA-Z]+>)|(?<B>[<>])")]
        private static partial Regex gtLtRegex();

        [GeneratedRegex(@"&(?!([a-zA-Z]+;))")]
        private static partial Regex exclXmlCodesRegex();

        private static void AddCode(Content content, HtmlNode node, XmlElement body, XmlDocument doc) {
            var lines = node.InnerHtml.Split(separators, StringSplitOptions.None);
            foreach (var line in lines) {
                XmlElement p = doc.CreateElement("p");
                XmlElement code = doc.CreateElement("code");
                XmlElement codeblock = doc.CreateElement("codeblock");
                string input = exclXmlCodesRegex().Replace(line, "&amp;");
                string processedLine = gtLtRegex().Replace(input, match => {
                    if (match.Groups["B"].Success) {
                        return match.Groups["B"].Value == ">" ? "&gt;" : "&lt;";
                    }
                    return match.Value;
                });
                try {
                    codeblock.InnerXml = processedLine;
                }
                catch (XmlException ex) {
                    Console.WriteLine($"Code adding ERROR {ex.Message}");
                    Console.WriteLine($"Code: {line}");
                    Console.WriteLine($"Processed Code: {processedLine}");
                    Console.WriteLine($"Node content: {content.Node?.OuterHtml}\n");
                    codeblock.InnerText = "<!-- Error: Failed to add code block -->";
                }
                code.AppendChild(codeblock);
                p.AppendChild(code);
                body.AppendChild(p);
            }
        }

        private static async Task AddCodeImageAsync(HtmlNode node, XmlElement body, XmlElement fB, XmlDocument doc, IPage page) {
            string decodedHtml = WebUtility.HtmlDecode(node.InnerHtml);
            string carbonUrl = $"https://carbon.now.sh/?bg=rgba%28171%2C+184%2C+195%2C+1%29&t=seti&wt=none&l=auto&width=800&ds=false&dsyoff=20px&dsblur=68px&wc=false&wa=false&pv=56px&ph=56px&ln=false&fl=1&fm=Hack&fs=10px&lh=105%25&si=false&es=2x&wm=false&code={Uri.EscapeDataString(decodedHtml)}";
            await page.GoToAsync(carbonUrl);
            IElementHandle codeElement = await page.WaitForSelectorAsync(".CodeMirror-code");
            BoundingBox boundingBox = await codeElement.BoundingBoxAsync();
            const int chunkSize = 10;
            const decimal lineHeight = 10.5m;
            int totalLines = decodedHtml.Count(c => c == '\n');
            for (int i = 0; i < totalLines; i += chunkSize) {
                int lineCount = Math.Min(chunkSize, totalLines - i);
                int yOffset = (int)(boundingBox.Y + (i * lineHeight));
                decimal height;
                if (i + chunkSize > totalLines) {
                    height = lineHeight * (lineCount - totalLines) + boundingBox.Height;
                }
                else {
                    height = lineCount * lineHeight;
                }
                ScreenshotOptions ssOp = new() {
                    Clip = new() {
                        X = boundingBox.X,
                        Y = yOffset,
                        Width = boundingBox.Width,
                        Height = height
                    },
                    OptimizeForSpeed = true
                };
                string binaryId = $"img_{Guid.NewGuid()}";
                XmlElement binary = doc.CreateElement("binary");
                binary.SetAttribute("id", binaryId);
                binary.SetAttribute("content-type", "image/png");
                binary.InnerText = await page.ScreenshotBase64Async(ssOp);
                fB.AppendChild(binary);
                XmlElement img = doc.CreateElement("image");
                XmlAttribute hrefAttr = doc.CreateAttribute("l", "href", "http://www.w3.org/1999/xlink");
                hrefAttr.Value = $"#{binaryId}";
                img.Attributes.Append(hrefAttr);
                body.AppendChild(img);
            }
        }

        private static async Task AddImageAsync(Content content, XmlElement body, XmlElement fB, XmlDocument doc) {
            if (content.Node == null) {
                return;
            }
            HtmlNode node = content.Node;
            string src = node.GetAttributeValue("src", string.Empty);
            Uri baseUri = new(content.Url);
            Uri relativeUri = new(baseUri, src);
            string imageUrl = relativeUri.AbsoluteUri;
            try {
                HttpClient httpClient = HttpManager.Instance.GetHttpClient();
                byte[] imageData = await httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
                string binaryId = $"img_{Guid.NewGuid()}";
                XmlElement binary = doc.CreateElement("binary");
                binary.SetAttribute("id", binaryId);
                binary.SetAttribute("content-type", "image/jpeg");
                binary.InnerText = Convert.ToBase64String(imageData);
                fB.AppendChild(binary);
                XmlElement img = doc.CreateElement("image");
                XmlAttribute hrefAttr = doc.CreateAttribute("l", "href", "http://www.w3.org/1999/xlink");
                hrefAttr.Value = $"#{binaryId}";
                img.Attributes.Append(hrefAttr);
                body.AppendChild(img);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error downloading image from {imageUrl}: {ex.Message}\n");
            }
        }
    }
}
