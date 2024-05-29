using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace MetanitReader {
    public class HttpManager {
        private static HttpManager? _instance;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private HttpManager() {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
            _httpClient = CreateHttpClient();
        }

        public static HttpManager Instance {
            get {
                _instance ??= new HttpManager();
                return _instance;
            }
        }

        private HttpClient CreateHttpClient() {
            var httpClientHandler = new HttpClientHandler();
            var proxy = _configuration["Proxy"];
            if (!string.IsNullOrEmpty(proxy)) {
                var proxyComponents = proxy.Split(':');
                if (proxyComponents.Length == 4 && int.TryParse(proxyComponents[1], out int port)) {
                    string address = proxyComponents[0];
                    string username = proxyComponents[2];
                    string password = proxyComponents[3];
                    var webProxy = new WebProxy(address, port) {
                        Credentials = new NetworkCredential(username, password)
                    };
                    httpClientHandler.Proxy = webProxy;
                    httpClientHandler.UseProxy = true;
                }
            }
            var httpClient = new HttpClient(httpClientHandler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            return httpClient;
        }

        public HttpClient GetHttpClient() {
            return _httpClient;
        }
    }
}
