using System.Xml;

namespace MetanitReader.Fb2 {
    public class FictionBook {
        public XmlDocument Document { get; }
        public XmlElement Fb { get; }
        public XmlElement Body { get; }

        public FictionBook(XmlDocument doc, XmlElement fB, XmlElement body) {
            Document = doc;
            Fb = fB;
            Body = body;
            Document.AppendChild(Fb);
            Fb.AppendChild(Body);
        }

        public static FictionBook Generate(string title) {
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
            bookTitle.InnerXml = title;
            titleInfo.AppendChild(bookTitle);
            XmlElement author = doc.CreateElement("author");
            titleInfo.AppendChild(author);
            XmlElement firstName = doc.CreateElement("first-name");
            firstName.InnerXml = "METANIT";
            author.AppendChild(firstName);
            XmlElement body = doc.CreateElement("body");
            fB.AppendChild(body);
            return new FictionBook(doc, fB, body);
        }
    }
}