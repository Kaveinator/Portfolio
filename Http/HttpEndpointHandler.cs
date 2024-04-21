using System.Net;

namespace WebServer.Http {
    public abstract class HttpEndpointHandler {
        public readonly string Host;
        public readonly HttpServer HttpServer;

        protected HttpEndpointHandler(string host, HttpServer server) {
            Host = host;
            HttpServer = server;
        }

        public HttpResponse GetGenericStatusPage(HttpStatusCode statusCode, Dictionary<string, object> additionalParameters = null)
            => HttpServer.GetGenericStatusPage(statusCode, additionalParameters, Host);

        public HttpServer AddEventCallback(string path, Func<HttpListenerRequest, Task<HttpResponse>> callback)
            => HttpServer.AddEventCallback(BuildUri(path), callback);

        protected Uri BuildUri(string path) {
            string uriString = $"https://{Host}/";
            if (string.IsNullOrEmpty(path))
                return new Uri(uriString);
            path = path.Replace('\\', '/');
            if (path[0] == '/')
                path = path.Substring(1);
            uriString += path;
            return new Uri(uriString);
        }
    }
}