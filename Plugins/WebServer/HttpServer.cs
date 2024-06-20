using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SimpleJSON;
using Timer = System.Timers.Timer;
using System.Reflection;
using System.Reflection.Metadata;
using Portfolio;
using WebServer.Models;
using System.Diagnostics.Contracts;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebServer.Http {
    /* Notes
     * Optimization:
     *   - https://www.linkedin.com/pulse/http-inefficiency-dominika-blach/
     */

    public partial class HttpServer {
        public static JSONFile Config = Properties.GetOrCreate<HttpServer>();
        protected HttpListener Socket;
        public readonly DirectoryInfo StaticDomainDirectoryInfo;
        static EventLogger Logger = EventLogger.GetOrCreate<HttpServer>();
        static Type Type = typeof(HttpServer);
        public bool IsListening => Socket?.IsListening ?? false;
        // HttpCallbacks[domainName (lowercase)][path (lowercase)] => Func<in request, out response>
        Dictionary<string, Dictionary<Regex, Func<HttpListenerRequest, Task<HttpResponse?>>>> HttpCallbacks = new();
        public List<HttpEndpointHandler> HttpEndpointsHandlers = new List<HttpEndpointHandler>();
        // So in staticDomainDirectory, it searched for the domain name, if not found, returns
        public string ProductionDirectory => Config.GetValueOrDefault(nameof(ProductionDirectory), "Static");
        public string DefaultDomain => Config.GetValueOrDefault(nameof(DefaultDomain), "localhost");
        public ushort TargetPort => Config.GetValueOrDefault(nameof(Port), 80);
        public ushort MaxRequestsPerAddressPerMinute => Config.GetValueOrDefault(nameof(MaxRequestsPerAddressPerMinute), 30);
        public bool Enabled => Config.GetValueOrDefault(nameof(Enabled), true);
        public bool ShowExceptionsOnErrorPages => Config.GetValueOrDefault(nameof(ShowExceptionsOnErrorPages), false);
        public ushort RequestTimeout => Config.GetValueOrDefault(nameof(RequestTimeout), 10000).AsUShort;
        public uint MaxCacheAge {
            get => Config.GetValueOrDefault(nameof(MaxCacheAge), 3600).AsUInt; // Default: 1h
            set => Config[nameof(MaxCacheAge)] = value;
        }
        public ushort MaxConcurrentRequests => Config.GetValueOrDefault(nameof(MaxConcurrentRequests), 5).AsUShort;
        
        public HttpServer() {
            StaticDomainDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, ProductionDirectory));
            if (!StaticDomainDirectoryInfo.Exists)
                StaticDomainDirectoryInfo.Create();
            DirectoryInfo defaultDomainDirectory = new DirectoryInfo(Path.Combine(StaticDomainDirectoryInfo.FullName, DefaultDomain));
            if (!defaultDomainDirectory.Exists)
                StaticDomainDirectoryInfo.Create();
            Logger.LogDebug($"Default domain '{DefaultDomain}'");
        }

        public ushort Port { get; protected set; }
        CancellationTokenSource ListenToken;
        public async Task Start() {
            await Config.LoadAsync();
            if (!Enabled) {
                Logger.LogWarning("HttpServer.Start() was invoked but the module is disabled!", true);
                return;
            }
            if (Socket?.IsListening == true)
                _ = Stop();
            Socket = new HttpListener() {
                IgnoreWriteExceptions = true
            };
            Port = TargetPort;
            // TODO: Make it start listening... maybe on a seperate thread? (edit: yeah lets do that later, lazy rn XD)
            Socket.Prefixes.Add($"http://+:{TargetPort}/");
            //Socket.Prefixes.Add($"https://+:443/");

            JSONArray uriFillers = Config.GetValueOrDefault("UriFillers", new JSONArray()).AsArray;
            if (uriFillers.Count > 0) UriFillers.Clear();
            foreach (JSONNode item in uriFillers.Values) {
                string filler = item.Value;
                if (string.IsNullOrEmpty(filler)) continue;
                if (!UriFillers.Contains(filler))
                    UriFillers.Add(filler);
            }

            Socket.Start();
            Logger.Log($"Started on port '{TargetPort}'");

            while (Socket.IsListening) {
                await Listen((ListenToken = new CancellationTokenSource()).Token);
            }
        }
        
        public HttpServer Stop() {
            if (!ListenToken.IsCancellationRequested)
                ListenToken.Cancel();
            return this;
        }
        public int TasksCount => Tasks.Count;
        HashSet<Task> Tasks = new HashSet<Task>();
        async Task Listen(CancellationToken token) {
            Tasks = new HashSet<Task>(128);
            for (int i = 0; i < 64; i++) // Create 64 
                Tasks.Add(Socket.GetContextAsync());
            //Logger.Log($"Listening with {Tasks.Count} worker(s)");
            Program.UpdateTitle();
            while (!token.IsCancellationRequested) {
                Task t = await Task.WhenAny(Tasks);
                Tasks.Remove(t);

                if (t is Task<HttpListenerContext> context) {
                    if (Tasks.Count < MaxConcurrentRequests)
                        Tasks.Add(Socket.GetContextAsync());
                    Tasks.Add(ProcessRequestAsync(context.Result));
                }
                Program.UpdateTitle();
            }
            Socket.Stop();
        }

        async Task ProcessRequestAsync(HttpListenerContext context) {
            try {
                context.Response.Headers.Add("x-content-type-options: nosniff");
                context.Response.Headers.Add("x-xss-protection:1; mode=block");
                context.Response.Headers.Add("x-frame-options:DENY");
                context.Response.Headers.Add("server", "WebServer 2024.3.16b3");
                context.Response.Headers.Add("server-environment", Program.Mode.ToString());

                // Get Callback stuff
                string host = (context.Request.Url?.Host ?? DefaultDomain).Trim('/', ' ').ToLowerInvariant(),
                       path = $"/{FormatCallbackKey(context.Request.Url?.LocalPath ?? string.Empty)}";
                HttpResponse? response = null;
                bool requestTimedOut = false;
                Timer timeoutTimer = new Timer(RequestTimeout);
                MethodBase? methodUsed = MethodBase.GetCurrentMethod();

                timeoutTimer.Elapsed += async (sender, e) => {
                    if (requestTimedOut) {
                        timeoutTimer.Stop();
                        return;
                    }
                    requestTimedOut = true;
                    try {
                        HttpResponse responseObj = new HttpResponse() {
                            StatusCode = HttpStatusCode.ServiceUnavailable,
                            Content = (context.Response.ContentEncoding ?? Encoding.UTF8).GetBytes("Service Unavailable! 10 sec timeout reached, request aborted."),
                            ContentString = "text/plain"
                        };
                        context.Response.StatusCode = (int)responseObj.StatusCode;
                        //context.Response.Headers.Add($"content-type: {response.MimeString}; charset=UTF-8");
                        context.Response.ContentType = responseObj.ContentString;
                        context.Response.ContentEncoding = Encoding.UTF8;
                        byte[] errorBytes = responseObj.Content;
                        context.Response.ContentLength64 = errorBytes.Length;
                        await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                        context.Response.Close();
                        //if (GeneralDomainCallbacks.ContainsKey(host) ? GeneralDomainCallbacks[host](context.Request, methodUsed, responseObj) : true)
                        Logger.LogError($"[{responseObj.StatusCode}] ABORT '{context.Request.Url}' [Method: {methodUsed.PrettyPrint()}]");
                    }
                    catch { }
                };
                timeoutTimer.Start();

                #region Event Callbacks
                var domainNames = new[] { host, DefaultDomain }.Distinct();
                var domainCallbackMatches = domainNames.Where(name => HttpCallbacks.ContainsKey(name));
                var regexCallbacks = domainCallbackMatches.SelectMany(name => HttpCallbacks[name]);
                /*var regexCallbacks = new[] { host, DefaultDomain }.Distinct()
                    .Where(domain => HttpCallbacks.ContainsKey(domain))
                    .SelectMany(domain => HttpCallbacks[domain]);*/
                foreach (var kvp in regexCallbacks) {
                    bool match = kvp.Key.IsMatch(path);
                    if (!match) continue;
                    try {
                        response = await kvp.Value(context.Request);
                        methodUsed = kvp.Value.Method;
                    } catch (Exception ex) {
                        response = GetGenericStatusPage(new StatusPageModel(HttpStatusCode.InternalServerError,
                            subtitle: Program.Mode == Mode.Development ? $"<code>{ex.ToString().Replace("\n", "<br />")}</code>" : null
                        ));
                    }
                    if (response != null) break;
                }
                // End Get Callback stuff
                if (requestTimedOut) return;
                timeoutTimer.Stop();
                #endregion
                if (response == null) {
                    methodUsed = MethodBase.GetCurrentMethod();
                    response = GetStaticFile(context.Request);
                    response.AccessControlAllowOrigin = "*";
                }
                context.Response.Headers.Add("cache-control",
                    !response.AllowCaching || Program.Mode == Mode.Development
                    ? "no-store, no-cache, must-revalidate"
                    : "max-age=360000, s-max-age=900, stale-while-revalidate=120, stale-if-error=86400"
                );

                if (!string.IsNullOrEmpty(response.AccessControlAllowOrigin))
                    context.Response.Headers.Add("Access-Control-Allow-Origin", response.AccessControlAllowOrigin);
                
                // Set final headers
                context.Response.StatusCode = (int)response.StatusCode;
                if (response.StatusCode == HttpStatusCode.Redirect) {
                    context.Response.Redirect(response.ContentString);
                }
                bool omitBody = new[] { "HEAD", "PUT", "DELETE" }.Contains(context.Request.HttpMethod.ToUpper()) ||
                    (100 <= (int)response.StatusCode && (int)response.StatusCode <= 199) ||
                    response.StatusCode == HttpStatusCode.NoContent ||
                    response.StatusCode == HttpStatusCode.NotModified;
                if (!omitBody) {
                    context.Response.Headers["Content-Type"] = response.MimeString; // Was: context.Response.ContentType = response.MimeString; but causes errors if it was
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentLength64 = response.Content.Length;
                    await context.Response.OutputStream.WriteAsync(response.Content, 0, response.Content.Length);
                }
                context.Response.Close();
                // Log result
                Action<object, bool?> LogFunc = response.IsSuccessStatusCode ? Logger.LogDebug : Logger.LogWarning;
                LogFunc($"[{(int)response.StatusCode}] '{context.Request.Url}'", null);
                //LogFunc($"[{(int)response.StatusCode}] '{new UriBuilder(context.Request.Url) { Query = string.Empty }.Uri}'", null);
            } catch (Exception ex) { Logger.LogError($"[Undocumented Error] '{context.Request.Url}'\n{ex}"); }
        }

        [Pure] static string FormatCallbackKey(string key)
            => string.IsNullOrEmpty(key) ? string.Empty
                : key.ToLower().Replace('\\', '/').Replace("//", "/").Trim(' ', '/');

        [Pure] static string FormatCallbackKey(string host, string path) => $"{host}/{path.TrimStart('/')}";

        [Pure] static string FormatCallbackKey(Uri? uri) => FormatCallbackKey(uri.Host, uri.LocalPath);

        [Pure] static List<string> GenerateCallbackKeys(string path) {
            var keys = new List<string>();
            var parts = path.Split('/');
            int iterationCount = parts.Length - 1;
            for (int i = 0; i < iterationCount; i++) {
                var key = string.Join("/", parts.Take(i + 1)) + "/**";
                keys.Insert(0, key);
            }
            if (keys.Count > 0) keys[0] = keys[0].TrimEnd('*') + "*";
            keys.Insert(0, path); // Add exact path last to prioritize it over wildcards
            return keys;
        }

        List<string> UriFillers = new List<string>() { "index.html", "index.htm" };
        public HttpResponse GetStaticFile(HttpListenerRequest request) => GetStaticFile(request.Url?.Host, request.Url?.LocalPath);

        public HttpResponse GetStaticFile(string? targetDomain, string? localPath) {
            DirectoryInfo directory = Program.Mode.HasFlag(Mode.Development) ? HttpTemplates.PublicPath : StaticDomainDirectoryInfo;
            // Works on windows, but on linux, the domain folder will need to be lowercase
            targetDomain = targetDomain?.ToLower() ?? DefaultDomain;
            string basePath = Path.Combine(directory.FullName, targetDomain);
            bool usingFallbackDomain = !Directory.Exists(basePath);
            if (usingFallbackDomain) { // Only fallback to default if domain folder doesn't exist
                targetDomain = DefaultDomain;
                basePath = Path.Combine(directory.FullName, DefaultDomain);
            }
            string resourceIdentifier = FormatCallbackKey(localPath ?? string.Empty);
            CachedResource resource = CachedResource.GetOrCreate(this, resourceIdentifier);
            if (!resource.NeedsUpdate) {
                resource.StatusCode = HttpStatusCode.OK;
                return resource;
            }
            string filePath = Path.Combine(basePath, resourceIdentifier);
            if (File.Exists(filePath)) {
                resource.StatusCode = HttpStatusCode.Created;
                resource.MimeString = MimeTypeMap.GetMimeType(Path.GetExtension(filePath).ToLower());
                resource.Content = File.ReadAllBytes(filePath);
                resource.AllowCaching = true; // Since its static allow caching
                if (resource.MimeString.ToLower().Contains("text"))
                    resource.ContentString = HttpTemplates.Process(resource.ContentString);
                resource.ClearFlag();
                return resource;
            }
            string? hitPath = UriFillers.Select(filler => filePath + filler)
                .FirstOrDefault(path => path.Contains(basePath) && File.Exists(path));
            if (hitPath != null) {
                resource.StatusCode = HttpStatusCode.Created;
                resource.MimeString = MimeTypeMap.GetMimeType(Path.GetExtension(hitPath).ToLower());
                resource.Content = File.ReadAllBytes(hitPath);
                resource.AllowCaching = true;
                if (resource.MimeString.ToLower().Contains("text"))
                    resource.ContentString = HttpTemplates.Process(resource.ContentString);
                resource.ClearFlag();
                return resource;
            }
            return GetGenericStatusPage(new StatusPageModel(Directory.Exists(filePath) ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound), host: targetDomain);
        }

        public HttpResponse ShowIndexOf(string directoryPath) => throw new NotImplementedException();

        public bool TryRegisterEndpointHandler<T>(Func<T> handlerInitilizer, out T handler) where T : HttpEndpointHandler {
            handler = handlerInitilizer?.Invoke() ?? default!;
            if (handler == null) return false;
            HttpEndpointsHandlers.Add(handler);
            return true;
        }

        public HttpResponse GetGenericStatusPage(StatusPageModel pageModel, string? host = null) {
            if (!((host != null && HttpTemplates.TryGet($"{host}/ErrorPage.html", out string result, pageModel)) ||
                HttpTemplates.TryGet($"{DefaultDomain}/ErrorPage.html", out result, pageModel) ||
                HttpTemplates.TryGet($"Generic/ErrorPage.html", out result, pageModel)))
                result = HttpTemplates.Format("{?:StatusCode} {?:Title}: {?:Subtitle}", pageModel);

            return new HttpResponse() {
                StatusCode = pageModel.StatusCode,
                MimeString = MimeTypeMap.GetMimeType(".html"),
                ContentString = result
            };
        }

        public bool ContainsEventCallback(Task<HttpResponse?> callback) {
            if (callback == null) return false;
            return HttpCallbacks.Any(domainKvp => domainKvp.Value.Values.Any(v => v.Equals(callback)));
        }

        public bool TryAddEventCallback(string host, Regex regex, Func<HttpListenerRequest, Task<HttpResponse?>> callback) {
            if (string.IsNullOrEmpty(host) || regex == null || callback == null) return false;
            host = FormatCallbackKey(host);
            if (!HttpCallbacks.TryGetValue(host, out var domainCallbacks))
                HttpCallbacks.Add(host, domainCallbacks = new());
            domainCallbacks[regex] = callback;
            return true;
        }

        public bool TryRemoveEventCallback(Func<HttpListenerRequest, Task<HttpResponse?>> method) {
            if (method == null) return false;

            ushort removeCount = 0;
            HttpCallbacks.Values.Do(callbackDict => {
                callbackDict.Where(kvp => kvp.Value == method)
                    .Do(kvp => { callbackDict.Remove(kvp.Key); removeCount++; });
            });
            return removeCount > 0;
        }
    }
    public class HttpResponse {
        public HttpStatusCode StatusCode = HttpStatusCode.NoContent;
        public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
        public string MimeString = MimeTypeMap.GetMimeType(".txt");
        public byte[] Content = Array.Empty<byte>();
        public string ContentString {
            get => Encoding.UTF8.GetString(Content);
            set => Content = Encoding.UTF8.GetBytes(value);
        }
        public bool AllowCaching = false;
        public string AccessControlAllowOrigin = null;

        public HttpResponse() { }

        public HttpResponse(HttpStatusCode statusCode, byte[] content, string mimeString = "text/plain", bool allowCaching = true) {
            StatusCode = statusCode;
            Content = content;
            MimeString = mimeString;
            AllowCaching = allowCaching;
        }

        public HttpResponse(HttpStatusCode statusCode, string content, string mimeString = "text/plain", bool allowCaching = true) {
            StatusCode = statusCode;
            ContentString = content;
            MimeString = mimeString;
            AllowCaching = allowCaching;
        }
    }
}
