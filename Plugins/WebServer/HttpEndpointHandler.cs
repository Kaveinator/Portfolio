using System.Net;

namespace WebServer.Http {
    public abstract class HttpEndpointHandler {
        public readonly string Host;
        public readonly HttpServer HttpServer;

        protected HttpEndpointHandler(string host, HttpServer server) {
            Host = host;
            HttpServer = server;
        }

        public HttpResponse GetGenericStatusPage(HttpStatusCode statusCode, Dictionary<string, object>? additionalParameters = null)
            => HttpServer.GetGenericStatusPage(statusCode, additionalParameters, Host);

        public string GetTemplate(string path, Dictionary<string, object>? parameters = null, bool keepParamNamesIfKeyNotFound = false)
            => HttpTemplates.Get(Host + path, parameters, keepParamNamesIfKeyNotFound);

        public bool TryGetTemplate(string path, out string content, Dictionary<string, object>? parameters = null)
            => HttpTemplates.TryGet(Host + path, out content, parameters);

        public HttpServer AddEventCallback(string path, Func<HttpListenerRequest, Task<HttpResponse?>> callback)
            => HttpServer.AddEventCallback(BuildUri(path), callback);

        public HttpServer AddEventCallback(string path, Func<HttpListenerRequest, HttpResponse?> callback)
            => HttpServer.AddEventCallback(BuildUri(path), async ctx => callback(ctx));

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

        protected Uri BuildUri(HttpListenerRequest request, string path) {
            string uriString = $"{(request.IsSecureConnection ? "https" : "http")}://{request.UserHostName}/";
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