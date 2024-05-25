using System.Net;
using Microsoft.Extensions.Configuration;

namespace MetanitReader {
    public class ProxyManager(IConfiguration configuration) {
        private readonly IConfiguration _configuration = configuration;

        public HttpClient CreateHttpClientWithProxy(string? proxy = null) {
            proxy ??= _configuration["Proxy"];
            if (!string.IsNullOrEmpty(proxy)) {
                var proxyComponents = proxy.Split(':');
                if (proxyComponents.Length == 4 && int.TryParse(proxyComponents[1], out int port)) {
                    var address = proxyComponents[0];
                    var username = proxyComponents[2];
                    var password = proxyComponents[3];
                    var webProxy = new WebProxy(address, port) {
                        Credentials = new NetworkCredential(username, password)
                    };
                    var httpClientHandler = new HttpClientHandler {
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