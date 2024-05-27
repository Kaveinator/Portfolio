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
        Dictionary<string, Dictionary<string, Func<HttpListenerRequest, Task<HttpResponse?>>>> HttpCallbacks = new Dictionary<string, Dictionary<string, Func<HttpListenerRequest, Task<HttpResponse?>>>>();
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
            Uri url = null;
            try {
                url = context.Request.Url;
                context.Response.Headers.Add("x-content-type-options: nosniff");
                context.Response.Headers.Add("x-xss-protection:1; mode=block");
                context.Response.Headers.Add("x-frame-options:DENY");
                context.Response.Headers.Add("Server", "WebServer 2024.3.16b3");
                context.Response.Headers.Add("ServerEnv", Program.Mode.ToString());

                // Get Callback stuff
                string host = FormatCallbackKey(context.Request.Url.Host),
                       path = FormatCallbackKey(context.Request.Url.LocalPath);
                HttpResponse response = null;
                bool IsRequestTimedOut = false;
                Timer timer = new Timer(RequestTimeout);
                MethodBase methodUsed = MethodBase.GetCurrentMethod();
                timer.Elapsed += async (sender, e) => {
                    if (IsRequestTimedOut) {
                        timer.Stop();
                        return;
                    }
                    IsRequestTimedOut = true;
                    try {
                        HttpResponse responseObj = new HttpResponse() {
                            StatusCode = HttpStatusCode.ServiceUnavailable,
                            Content = context.Response.ContentEncoding.GetBytes("Service Unavailable! 10 sec timeout reached, request aborted."),
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
                timer.Start();
                #region Event Callbacks

                Dictionary<string, Func<HttpListenerRequest, Task<HttpResponse>>> domainCallbacks = null;
                string domainKey = FormatCallbackKey(host);
                if (HttpCallbacks.ContainsKey(domainKey))
                    domainCallbacks = HttpCallbacks[domainKey];
                else if (HttpCallbacks.ContainsKey(domainKey = FormatCallbackKey(DefaultDomain)))
                    domainCallbacks = HttpCallbacks[domainKey];
                if (IsRequestTimedOut) return;
                //string catchAllKey = path.Substring(0, path.LastIndexOf('/') + 1) + "*"; // Kaveman: create catch all keys, each time going up a directory

                Queue<string> catchAllKeys = new Queue<string>();
                if (string.IsNullOrEmpty(path)) catchAllKeys.Enqueue("/*");
                //else catchAllKeys.Enqueue(path.Substring(0, path.LastIndexOf('/') + 1) + "*");
                else catchAllKeys.Enqueue(path);

                string lastKey = catchAllKeys.Last();
                while (!lastKey.Equals("/*"))
                {
                    string newKey = lastKey.Substring(0, lastKey.Length - 2);
                    newKey = newKey.Substring(0, newKey.LastIndexOf('/') + 1) + "*";
                    if (!newKey.Contains('/') || lastKey.Equals(newKey))
                        break;
                    lastKey = newKey;
                    catchAllKeys.Enqueue(newKey);
                }

                if (domainCallbacks != null)
                {
                    //domainCallbacks.ContainsKey(catchAllKey)
                    while (response is null && catchAllKeys.Count > 0)
                    {
                        string key = catchAllKeys.Dequeue();
                        if (!domainCallbacks.ContainsKey(key))
                            continue;
                        var callback = domainCallbacks[key];
                        if (callback is null) continue;
                        try
                        {
                            methodUsed = callback.Method;
                            response = await callback(context.Request);
                        }
                        catch (Exception ex)
                        {
                            Dictionary<string, object> additionalParams = new Dictionary<string, object>();
                            if (ShowExceptionsOnErrorPages)
                            {
                                additionalParams.Add("Title", ex.GetType().Name.AddSpacesToSentence());
                                additionalParams.Add("Subtitle", $"{ex.Message}<br />{ex.StackTrace.Replace("\n", "<br />")}");
                            }
                            methodUsed = Type.GetMethod(nameof(GetGenericStatusPage));
                            response = GetGenericStatusPage(new StatusPageModel(HttpStatusCode.InternalServerError), host);
                        }

                    }
                }
                // End Get Callback stuff
                if (IsRequestTimedOut) return;
                #endregion
                if (Program.Mode == Mode.Development) {
                    // If no response try getting a static file
                    if (response is null) {
                        methodUsed = Type.GetMethod(nameof(GetStaticFile));
                        response = GetStaticFile(context.Request);
                    }
                    // Since in dev mode, prevent caching
                    context.Response.Headers.Add("cache-control:no-store, no-cache, must-revalidate");
                }
                else { // Production mode (static site)
                    if (response is null) {
                        methodUsed = Type.GetMethod(nameof(GetStaticFile));
                        response = GetStaticFile(context.Request);
                        context.Response.Headers.Add("cache-control: max-age=360000, s-max-age=900, stale-while-revalidate=120, stale-if-error=86400");
                    }
                }
                timer.Stop();

                if (!string.IsNullOrEmpty(response.AccessControlAllowOrigin))
                    context.Response.Headers.Add("Access-Control-Allow-Origin", response.AccessControlAllowOrigin);

                if (IsRequestTimedOut) return;
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
                    context.Response.ContentType = response.MimeString;
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentLength64 = response.Content.Length;
                    await context.Response.OutputStream.WriteAsync(response.Content, 0, response.Content.Length);
                }
                context.Response.Close();
                // Log result
                Action<object, bool?> LogFunc = response.IsSuccessStatusCode ? Logger.LogDebug : Logger.LogWarning;
                LogFunc($"[{(int)response.StatusCode}] '{new UriBuilder(context.Request.Url) { Query = string.Empty }.Uri}'", null);
            } catch (Exception ex) { Logger.LogError($"[Undocumented Error] '{url}'\n{ex}"); }
        }
        
        /*public void AddRequestResponseCallback(string host, Func<HttpListenerRequest, MethodBase, HttpResponse, bool> callback) {
            if (string.IsNullOrEmpty(host) || callback is null) return;
            host = FormatCallbackKey(host);
            if (GeneralDomainCallbacks.ContainsKey(host))
                GeneralDomainCallbacks[host] = callback;
            else GeneralDomainCallbacks.Add(host, callback);
        }

        public void RemoveRequestResponseCallback(string host) {
            if (string.IsNullOrEmpty(host)) return;
            host = FormatCallbackKey(host);
            if (GeneralDomainCallbacks.ContainsKey(host))
                GeneralDomainCallbacks.Remove(host);
        }

        public void RemoveRequestResponseCallback(Func<HttpListenerRequest, MethodBase, HttpResponse, bool> callback) {
            if (callback is null) return;
            string host = GeneralDomainCallbacks.SingleOrDefault(p => p.Value == callback).Key;
            if (host is null) return;
            GeneralDomainCallbacks.Remove(host);
        }*/

        string FormatCallbackKey(string key) {
            if (!string.IsNullOrEmpty(key)) {
                key = key.ToLower();
                if (key.Contains('\\')) key = key.Replace('\\', '/');
                if (key[0] != '/') key = $"/{key}";
                if (key[key.Length - 1] == '/') key = key.Substring(0, key.Length - 1);
            }
            return key;
        }

        List<string> UriFillers = new List<string>() { "index.html", "index.htm" };
        public HttpResponse GetStaticFile(HttpListenerRequest request) {
            DirectoryInfo directory = Program.Mode.HasFlag(Mode.Development) ? HttpTemplates.PublicPath : StaticDomainDirectoryInfo;
            // Works on windows, but on linux, the domain folder will need to be lowercase
            string targetDomain = request.Url.Host.ToLower(),
                   basePath = Path.Combine(directory.FullName, targetDomain);
            bool usingFallbackDomain = !Directory.Exists(basePath);
            if (usingFallbackDomain) { // Only fallback to default if domain folder doesn't exist
                targetDomain = DefaultDomain;
                basePath = Path.Combine(directory.FullName, DefaultDomain);
            }
            string resourceIdentifier = $"{targetDomain}{request.Url.LocalPath}".ToLower().Trim().TrimEnd('/');
            CachedResource resource = CachedResource.GetOrCreate(this, resourceIdentifier);
            if (!resource.NeedsUpdate) {
                resource.StatusCode = HttpStatusCode.OK;
                return resource;
            }
            string relativePath = request.Url.LocalPath.Replace('\\', '/');
            if (relativePath.Length > 0 && relativePath[0] == '/')
                relativePath = relativePath.Substring(1);
            string filePath = Path.Combine(basePath, relativePath);
            if (File.Exists(filePath)) {
                resource.StatusCode = HttpStatusCode.Created;
                resource.MimeString = MimeTypeMap.GetMimeType(Path.GetExtension(filePath).ToLower());
                resource.Content = File.ReadAllBytes(filePath);
                resource.AllowCaching = true; // Since its static allow caching
                if (resource.MimeString.ToLower().Contains("text") && Program.Mode == Mode.Development)
                    resource.ContentString = HttpTemplates.Process(resource.ContentString);
                resource.ClearFlag();
                return resource;
            }
            else {//if (Directory.Exists(filePath)) {
                foreach (string filler in UriFillers) {
                    string filledPath = filler[0] == '*' ? filePath + filler.Substring(1) : Path.Combine(new [] { filePath }.Concat(filler.Split('/')).ToArray());
                    if (File.Exists(filledPath)) {
                        resource.StatusCode = HttpStatusCode.Created;
                        resource.MimeString = MimeTypeMap.GetMimeType(Path.GetExtension(filledPath).ToLower());
                        resource.Content = File.ReadAllBytes(filledPath);
                        resource.AllowCaching = true;
                        if (resource.MimeString.ToLower().Contains("text") && Program.Mode == Mode.Development)
                            resource.ContentString = HttpTemplates.Process(resource.ContentString);
                        resource.ClearFlag();
                        return resource;
                    }
                }
                return GetGenericStatusPage(new StatusPageModel(Directory.Exists(filePath) ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound), host: request.Url.Host);
            }/*
            else if (Directory.Exists(filePath = Path.Combine(filePath, "..")) && filePath.Contains(basePath)) {
                foreach (string filler in UriFillers) {
                    string filledPath = filler[0] == '*' ? filePath + filler.Substring(1) : Path.Combine(filePath, filler);
                    Debug.Log($"{filledPath);
                    if (File.Exists(filledPath)) {
                        resource.StatusCode = HttpStatusCode.Created;
                        resource.MimeString = MimeTypeMap.GetMimeType(Path.GetExtension(filledPath).ToLower());
                        resource.Content = File.ReadAllBytes(filledPath);
                        resource.AllowCaching = true;
                        if (resource.MimeString.ToLower().Contains("text") && Program.Mode == Mode.Development)
                            resource.ContentString = HttpTemplates.Process(resource.ContentString);
                        return resource;
                    }
                }
                //return GetGenericStatusPage(HttpStatusCode.Forbidden, host: request.Url.Host, defaultHost: DefaultDomainName);
            }*/
            return GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound), host: request.Url.Host);
        }

        public HttpResponse ShowIndexOf(string directoryPath) {
            return null;
        }

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

        public bool ContainsEventCallback(Uri uri) {
            if (uri != null && !string.IsNullOrEmpty(uri.Host) && !string.IsNullOrEmpty(uri.LocalPath)) {
                string host = FormatCallbackKey(uri.Host);
                if (HttpCallbacks.ContainsKey(host) &&
                    HttpCallbacks[host].ContainsKey(FormatCallbackKey(uri.LocalPath))
                ) return true;
            }
            return false;
        }

        public HttpServer AddEventCallback(Uri uri, Func<HttpListenerRequest, Task<HttpResponse?>> callback) {
            if (uri != null && !string.IsNullOrEmpty(uri.Host) && !string.IsNullOrEmpty(uri.LocalPath) && callback != null) {
                string host = FormatCallbackKey(uri.Host),
                    path = FormatCallbackKey(uri.LocalPath);

                if (!HttpCallbacks.ContainsKey(host)) {
                    HttpCallbacks.Add(host, new Dictionary<string, Func<HttpListenerRequest, Task<HttpResponse?>>>());
                    // Make a command to view http callbacks
                    //EventLogger.LogDebug($"Added callback for '{uri}'");
                }
                //else EventLogger.LogDebug($"Updated callback for '{uri}'");
                var pathDict = HttpCallbacks[host];
                if (!pathDict.ContainsKey(path))
                    pathDict[path] = callback;
            }
            return this;
        }

        public HttpServer RemoveEventCallback(Uri uri) {
            if (uri != null && !string.IsNullOrEmpty(uri.Host) && !string.IsNullOrEmpty(uri.LocalPath)) {
                string host = FormatCallbackKey(uri.Host),
                    path = FormatCallbackKey(uri.LocalPath);

                if (!HttpCallbacks.ContainsKey(host))
                    HttpCallbacks.Add(host, new Dictionary<string, Func<HttpListenerRequest, Task<HttpResponse>>>());

                var pathDict = HttpCallbacks[host];
                if (!pathDict.ContainsKey(path))
                    pathDict.Remove(path);

                if (pathDict.Count == 0) {
                    HttpCallbacks.Remove(host);
                    Logger.LogDebug($"Removed callback for '{uri}'");
                }
            }
            return this;
        }
    }
    public class HttpResponse {
        public HttpStatusCode StatusCode = HttpStatusCode.NoContent;
        public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
        public string MimeString = MimeTypeMap.GetMimeType(".txt");
        public byte[] Content = Array.Empty<byte>();
        public string ContentString {
            get => Encoding.Default.GetString(Content);
            set => Content = Encoding.Default.GetBytes(value);
        }
        public bool AllowCaching = false;
        public string AccessControlAllowOrigin = null;
    }
}
