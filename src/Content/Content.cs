using HtmlAgilityPack;

namespace MetanitReader {
    public enum ContentType {
        Guide,
        Article
    }
    public class Content {
        public string? Name { get; set; }
        public string Url { get; set; }
        public HtmlNode? Node { get; set; }
        public ContentType? Type { get; set; }

        public Content(string url, HtmlNode node) {
            Url = url;
            Node = node;
        }

        public Content(string name, string url, ContentType type) {
            Name = name;
            Url = url;
            Type = type;
        }
    }
}