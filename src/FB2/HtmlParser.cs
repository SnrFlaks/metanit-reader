using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using PuppeteerSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MetanitReader.Fb2 {
    public partial class HtmlParser {
        public static async Task ParseHtmlNodeAsync(Content content, FictionBook fB, IPage page) {
            HtmlNode node = content.Node!;
            XmlElement body = fB.Body;
            if (node.Name == "h1" || node.Name == "h2" || node.Name == "h3") {
                XmlElement title = CreateTitleElement(node, fB, node.Name);
                body.AppendChild(title);
            }
            else if (node.Name == "p") {
                XmlElement p = CreateParagraphElement(node, fB);
                body.AppendChild(p);
            }
            else if (node.Name == "ul") {
                XmlElement list = CreateListElement(node, fB);
                body.AppendChild(list);
            }
            else if (node.Name == "pre") {
                if (node.HasClass("browser") || node.InnerHtml.Count(c => c == '\n') < 20) {
                    XmlElement code = CreateCodeElement(node, fB);
                    body.AppendChild(code);
                }
                else {
                    foreach (var img in await CreateCodeImageElementsAsync(node, fB, page)) {
                        body.AppendChild(img);
                    }
                }
            }
            else if (node.Name == "img") {
                XmlElement img = await CreateImageElementAsync(content, fB);
                body.AppendChild(img);
            }
            else {
                foreach (var childNode in node.ChildNodes) {
                    await ParseHtmlNodeAsync(new Content(content.Url, childNode), fB, page);
                }
            }
        }

        private static XmlElement CreateTitleElement(HtmlNode node, FictionBook fB, string level) {
            XmlElement title;
            XmlDocument doc = fB.Document;
            XmlElement p = doc.CreateElement("p");
            if (level == "h1") {
                title = doc.CreateElement("title");
            }
            else {
                title = doc.CreateElement("subtitle");
                XmlElement strong = doc.CreateElement("strong");
                strong.InnerXml = node.InnerText.Replace("&", "&amp;");
                p.AppendChild(strong);
                title.AppendChild(p);
                return title;
            }
            p.InnerXml = node.InnerText.Replace("&", "&amp;");
            title.AppendChild(p);
            return title;
        }

        private static XmlElement CreateParagraphElement(HtmlNode node, FictionBook fB) {
            XmlDocument doc = fB.Document;
            XmlElement p = doc.CreateElement("p");
            foreach (var childNode in node.ChildNodes) {
                if (childNode.Name == "a") {
                    p.InnerXml += childNode.InnerText.Replace("&", "&amp;");
                }
                else if (childNode.Name == "class=\"b\"" || (childNode.Name == "span" && childNode.Attributes["class"]?.Value == "b")) {
                    XmlElement strongElement = doc.CreateElement("strong");
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
            return p;
        }

        private static XmlElement CreateListElement(HtmlNode node, FictionBook fB) {
            XmlDocument doc = fB.Document;
            XmlElement list = doc.CreateElement("ul");
            foreach (var liNode in node.SelectNodes("li")) {
                XmlElement li = doc.CreateElement("li");
                li.InnerXml = liNode.InnerText.Replace("&", "&amp;");
                list.AppendChild(li);
            }
            return list;
        }

        private static readonly string[] separators = ["\r\n", "\r", "\n"];

        [GeneratedRegex(@"(?:<[^ \/>][^>]*>[^<]*<\/[a-zA-Z]+>)|(?<B>[<>])")]
        private static partial Regex gtLtRegex();

        [GeneratedRegex(@"&(?!([a-zA-Z]+;))")]
        private static partial Regex exclXmlCodesRegex();

        private static XmlElement CreateCodeElement(HtmlNode node, FictionBook fB) {
            XmlDocument doc = fB.Document;
            XmlElement codeblock = doc.CreateElement("codeblock");
            var lines = node.InnerHtml.Split(separators, StringSplitOptions.None);
            foreach (var line in lines) {
                XmlElement p = doc.CreateElement("p");
                XmlElement code = doc.CreateElement("code");
                string input = exclXmlCodesRegex().Replace(line, "&amp;");
                string processedLine = gtLtRegex().Replace(input, match => {
                    if (match.Groups["B"].Success) {
                        return match.Groups["B"].Value == ">" ? "&gt;" : "&lt;";
                    }
                    return match.Value;
                }).Replace("\t", "\u00A0\u00A0\u00A0\u00A0");
                if (string.IsNullOrWhiteSpace(processedLine)) {
                    XmlElement el = doc.CreateElement("empty-line");
                    codeblock.AppendChild(el);
                    continue;
                }
                code.InnerXml = processedLine;
                p.AppendChild(code);
                codeblock.AppendChild(p);
            }
            return codeblock;
        }

        private static async Task<List<XmlElement>> CreateCodeImageElementsAsync(HtmlNode node, FictionBook fB, IPage page) {
            XmlDocument doc = fB.Document;
            string decodedHtml = WebUtility.HtmlDecode(node.InnerHtml);
            string carbonUrl = $"https://carbon.now.sh/?bg=rgba%28171%2C+184%2C+195%2C+1%29&t=seti&wt=none&l=auto&width=800&ds=false&dsyoff=20px&dsblur=68px&wc=false&wa=false&pv=56px&ph=56px&ln=false&fl=1&fm=Hack&fs=10px&lh=110%25&si=false&es=2x&wm=false&code={Uri.EscapeDataString(decodedHtml)}";
            await page.GoToAsync(carbonUrl);
            IElementHandle codeElement = await page.WaitForSelectorAsync(".CodeMirror-code");
            BoundingBox boundingBox = await codeElement.BoundingBoxAsync();
            const int chunkSize = 20;
            const int lineHeight = 11;
            int boxWidth = (int)boundingBox.Width;
            int boxHeight = (int)boundingBox.Height;
            List<XmlElement> imageElements = [];
            byte[] imageData = await page.ScreenshotDataAsync(new() {
                Clip = new() {
                    X = boundingBox.X,
                    Y = boundingBox.Y,
                    Width = boxWidth,
                    Height = boxHeight
                },
                OptimizeForSpeed = true
            });
            using Image<Rgba32> image = Image.Load<Rgba32>(imageData);
            using MemoryStream ms = new();
            int totalLines = decodedHtml.Count(c => c == '\n') + 1;
            for (int i = 0; i < totalLines; i += chunkSize) {
                int height = i + chunkSize > totalLines ? lineHeight * -i + boxHeight : chunkSize * lineHeight;
                Image<Rgba32> targetImage = new(boxWidth, height);
                image.ProcessPixelRows(targetImage, (sourceAccessor, targetAccessor) => {
                    for (int j = 0; j < height; j++) {
                        Span<Rgba32> sourceRow = sourceAccessor.GetRowSpan(i * lineHeight + j);
                        Span<Rgba32> targetRow = targetAccessor.GetRowSpan(j);
                        sourceRow[..boxWidth].CopyTo(targetRow);
                    }
                });
                ms.SetLength(0);
                targetImage.SaveAsJpeg(ms, new() {
                    Quality = 90
                });
                byte[] imageBytes = ms.ToArray();
                XmlElement img = GetXmlImage(imageBytes, fB);
                imageElements.Add(img);
            }
            return imageElements;
        }

        private static async Task<XmlElement> CreateImageElementAsync(Content content, FictionBook fB) {
            HtmlNode node = content.Node!;
            string src = node.GetAttributeValue("src", "");
            Uri relativeUri = new(new(content.Url), src);
            string imageUrl = relativeUri.AbsoluteUri;
            HttpClient httpClient = HttpManager.Instance.GetHttpClient();
            byte[] imageData = await httpClient.GetByteArrayAsync(imageUrl);
            XmlElement img = GetXmlImage(imageData, fB);
            return img;
        }

        private static XmlElement GetXmlImage(byte[] imageData, FictionBook fB) {
            XmlDocument doc = fB.Document;
            string binaryId = $"img_{Guid.NewGuid()}";
            XmlElement binary = doc.CreateElement("binary");
            binary.SetAttribute("id", binaryId);
            binary.SetAttribute("content-type", "image/jpeg");
            binary.InnerText = Convert.ToBase64String(imageData);
            fB.Fb.AppendChild(binary);
            XmlElement img = doc.CreateElement("image");
            XmlAttribute hrefAttr = doc.CreateAttribute("l", "href", "http://www.w3.org/1999/xlink");
            hrefAttr.Value = $"#{binaryId}";
            img.Attributes.Append(hrefAttr);
            return img;
        }
    }
}