namespace MetanitReader {
    public enum ContentType {
        Guide,
        Article
    }
    public class Content(string name, string url, ContentType type) {
        public string Name { get; set; } = name;
        public string Url { get; set; } = url;
        public ContentType Type { get; set; } = type;
    }
}