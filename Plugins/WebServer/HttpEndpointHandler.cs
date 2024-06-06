using System.Net;
using System.Text.RegularExpressions;
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

        public bool TryAddEventCallback(Regex regex, Func<HttpListenerRequest, Task<HttpResponse?>> callback)
            => HttpServer.TryAddEventCallback(Host, regex, callback);

        public bool TryAddEventCallback(Regex regex, Func<HttpListenerRequest, HttpResponse?> callback)
            => HttpServer.TryAddEventCallback(Host, regex, async ctx => callback(ctx));

        public bool TryAddEventCallback(string regex, Func<HttpListenerRequest, Task<HttpResponse?>> callback)
            => TryAddEventCallback(new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant), callback);

        public bool TryAddEventCallback(string regex, Func<HttpListenerRequest, HttpResponse?> callback)
            => TryAddEventCallback(new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant), callback);

        // Now obsolete since path matching now uses Regex, could still use it to build hrefs though
        public Uri BuildUri(string path) {
            string uriString = $"https://{Host}/";
            if (string.IsNullOrEmpty(path))
                return new Uri(uriString);
            path = path.Replace('\\', '/');
            if (path[0] == '/')
                path = path.Substring(1);
            uriString += path;
            return new Uri(uriString);
        }

        public Uri BuildUri(HttpListenerRequest request, string path) {
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