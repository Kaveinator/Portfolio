using System.Net;
using WebServer.Models;

namespace WebServer.Http {
    public abstract class HttpEndpointHandler {
        public readonly string Host;
        public readonly HttpServer HttpServer;

        protected HttpEndpointHandler(string host, HttpServer server) {
            Host = host;
            HttpServer = server;
        }

        public HttpResponse GetGenericStatusPage(HttpStatusCode statusCode) => GetGenericStatusPage(new StatusPageModel(statusCode));

        public HttpResponse GetGenericStatusPage(StatusPageModel statusModel)
            => HttpServer.GetGenericStatusPage(statusModel, Host);

        public string GetTemplate(string path, IPageModel? pageModel = null)
            => HttpTemplates.Get(Host + path, pageModel);

        public bool TryGetTemplate(string path, out string content, out StatusPageModel statusModel, IPageModel? pageModel = null) {
            path = Host + path;
            bool success = HttpTemplates.TryGet(path, out content, pageModel);
            statusModel = !success ? new(
                HttpStatusCode.InternalServerError,
                subtitle: $"The '{path}' View was not found"
            ) : new StatusPageModel(HttpStatusCode.OK);
            return success;
        }

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