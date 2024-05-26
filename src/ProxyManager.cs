using System.Net;
using Microsoft.Extensions.Configuration;

namespace MetanitReader {
    public class ProxyManager {
        private static ProxyManager? _instance;
        private readonly IConfiguration _configuration;

        private ProxyManager() {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static ProxyManager Instance {
            get {
                _instance ??= new ProxyManager();
                return _instance;
            }
        }

        public HttpClient CreateHttpClientWithProxy(string? proxy = null) {
            proxy ??= _configuration["Proxy"];
            if (!string.IsNullOrEmpty(proxy)) {
                var proxyComponents = proxy.Split(':');
                if (proxyComponents.Length == 4 && int.TryParse(proxyComponents[1], out int port)) {
                    string address = proxyComponents[0];
                    string username = proxyComponents[2];
                    string password = proxyComponents[3];
                    WebProxy webProxy = new(address, port) {
                        Credentials = new NetworkCredential(username, password)
                    };
                    HttpClientHandler httpClientHandler = new() {
                        Proxy = webProxy,
                        UseProxy = true
                    };
                    return new HttpClient(httpClientHandler);
                }
            }
            return new HttpClient();
        }
    }
}