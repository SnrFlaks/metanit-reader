using System.Text;
using System.Xml;
using HtmlAgilityPack;

namespace MetanitReader {
    class Fb2Generator {
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
                bookTitle.InnerText = content.Name;
                titleInfo.AppendChild(bookTitle);
            }
            XmlElement author = doc.CreateElement("author");
            titleInfo.AppendChild(author);
            XmlElement firstName = doc.CreateElement("first-name");
            firstName.InnerText = "METANIT";
            author.AppendChild(firstName);
            XmlElement body = doc.CreateElement("body");
            fB.AppendChild(body);
            foreach (var c in contentList) {
                await ParseHtmlNode(c, body, fB, doc);
            }
            return doc;
        }

        private static async Task ParseHtmlNode(Content content, XmlElement body, XmlElement fB, XmlDocument doc) {
            if (content.Node == null) {
                return;
            }
            HtmlNode node = content.Node;
            if (node.Name == "h1" || node.Name == "h2" || node.Name == "h3") {
                AddTitle(node, body, doc);
            }
            else if (node.Name == "p") {
                AddParagraph(node, body, doc);
            }
            else if (node.Name == "ul") {
                AddList(node, body, doc);
            }
            else if (node.Name == "pre") {
                AddCode(node, body, doc);
            }
            else if (node.Name == "img") {
                await AddImage(content, body, fB, doc);
            }
            else {
                foreach (var childNode in node.ChildNodes) {
                    await ParseHtmlNode(new Content(content.Url, childNode), body, fB, doc);
                }
            }
        }

        private static void AddTitle(HtmlNode node, XmlElement body, XmlDocument doc) {
            XmlElement title = doc.CreateElement("title");
            XmlElement p = doc.CreateElement("p");
            p.InnerText = node.InnerText;
            title.AppendChild(p);
            body.AppendChild(title);
        }

        private static void AddParagraph(HtmlNode node, XmlElement body, XmlDocument doc) {
            var p = doc.CreateElement("p");
            foreach (var childNode in node.ChildNodes) {
                try {
                    if (childNode.Name == "a") {
                        p.InnerText += childNode.InnerText.Replace("&", "&amp;");
                    }
                    else if (childNode.Name == "b" || (childNode.Name == "span" && childNode.Attributes["class"]?.Value == "b")) {
                        var strongElement = doc.CreateElement("strong");
                        strongElement.InnerText = childNode.InnerText.Replace("&", "&amp;");
                        p.AppendChild(strongElement);
                    }
                    else if (childNode.Name == "sup") {
                        var supElement = doc.CreateElement("sup");
                        supElement.InnerText = childNode.InnerText.Replace("&", "&amp;");
                        p.AppendChild(supElement);
                    }
                    else {
                        p.InnerXml += childNode.OuterHtml.Replace("&", "&amp;");
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error occurred while processing node: {ex.Message}");
                    Console.WriteLine($"Node content: {childNode.OuterHtml}");
                }
            }
            body.AppendChild(p);
        }

        private static void AddList(HtmlNode node, XmlElement body, XmlDocument doc) {
            XmlElement list = doc.CreateElement("ul");
            foreach (var liNode in node.SelectNodes("li")) {
                XmlElement li = doc.CreateElement("li");
                li.InnerText = System.Security.SecurityElement.Escape(liNode.InnerText);
                list.AppendChild(li);
            }
            body.AppendChild(list);
        }

        private static readonly string[] separators = ["\r\n", "\r", "\n"];

        private static void AddCode(HtmlNode node, XmlElement body, XmlDocument doc) {
            var lines = node.InnerHtml.Split(separators, StringSplitOptions.None);
            foreach (var line in lines) {
                XmlElement p = doc.CreateElement("p");
                XmlElement code = doc.CreateElement("code");
                if (!string.IsNullOrWhiteSpace(line)) {
                    code.InnerText = line.Replace(" ", "\u00A0").Replace("\t", "\u00A0\u00A0\u00A0\u00A0");
                }
                p.AppendChild(code);
                body.AppendChild(p);
            }
        }

        private static async Task AddImage(Content content, XmlElement body, XmlElement fB, XmlDocument doc) {
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
                img.SetAttribute("href", $"#{binaryId}");
                body.AppendChild(img);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error downloading image from {imageUrl}: {ex.Message}");
            }
        }
    }
}
