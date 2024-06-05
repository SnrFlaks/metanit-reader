using System.Diagnostics;
using System.Xml;
using PuppeteerSharp;

namespace MetanitReader.FictionBook {
    public class Generator {
        public static async Task<XmlDocument> GenerateDocumentAsync(Content content, List<Content> contentList) {
            XmlDocument doc = new();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(xmlDeclaration);
            XmlElement fB = doc.CreateElement("FictionBook", "http://www.gribuser.ru/xml/fictionbook/2.0");
            XmlAttribute xlink = doc.CreateAttribute("xmlns:l");
            xlink.Value = "http://www.w3.org/1999/xlink";
            fB.Attributes.Append(xlink);
            doc.AppendChild(fB);
            XmlElement description = doc.CreateElement("description");
            fB.AppendChild(description);
            XmlElement titleInfo = doc.CreateElement("title-info");
            description.AppendChild(titleInfo);
            XmlElement bookTitle = doc.CreateElement("book-title");
            bookTitle.InnerXml = content.Name!;
            titleInfo.AppendChild(bookTitle);
            XmlElement author = doc.CreateElement("author");
            titleInfo.AppendChild(author);
            XmlElement firstName = doc.CreateElement("first-name");
            firstName.InnerXml = "METANIT";
            author.AppendChild(firstName);
            XmlElement body = doc.CreateElement("body");
            fB.AppendChild(body);
            var stopwatch = Stopwatch.StartNew();
            using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true }))
            using (var page = await browser.NewPageAsync()) {
                foreach (var c in contentList) {
                    await HtmlParser.ParseHtmlNodeAsync(c, body, fB, doc, page);
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Full time: {stopwatch.ElapsedMilliseconds} ms");
            return doc;
        }
    }
}