using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace WebServer.Http {
    public class CachedResource : HttpResponse, IDisposable {

        public static List<CachedResource> Instances { get; private set; } = new List<CachedResource>();
        static EventLogger Logger => EventLogger.GetOrCreate<CachedResource>();

        public static ushort RaiseAllUpdateFlags() {
            ushort flagsRaised = 0;
            foreach (CachedResource resource in Instances) {
                if (resource.NeedsUpdate) continue;
                flagsRaised++;
                resource.RaiseUpdateFlag();
                //Logger.LogDebug($"Raised Update Flag for '{resource.Name}'");
            }
            return flagsRaised;
        }


        public static CachedResource GetOrDefault(HttpServer server, string name, Func<CachedResource> defaultResource) {
            name = name.ToLower();
            return Instances.SingleOrDefault(
                x => x.Server == server && x.Name == name,
                null
            ) ?? defaultResource();
        }
        public static CachedResource GetOrCreate(HttpServer server, string name)
            => GetOrDefault(server, name, () => new CachedResource(server, name));
        public static bool TryGet(HttpServer server, string name, out CachedResource resource)
            => (resource = GetOrDefault(server, name, null)) != null;

        public readonly string Name;
        Stopwatch TimeSinceLastUpdate;
        public readonly HttpServer Server;
        public long ResourceLifespan = 0;
        bool UpdateFlagRaised = true;
        public CachedResource(HttpServer server, string name = "", long resourceLifespan = 0) {
            Name = name ?? string.Empty;
            TimeSinceLastUpdate = Stopwatch.StartNew();
            ResourceLifespan = resourceLifespan;
            Server = server;
            Instances.Add(this);
        }

        public void RaiseUpdateFlag() => UpdateFlagRaised = true;

        public void ClearFlag() {
            UpdateFlagRaised = false;
            TimeSinceLastUpdate = Stopwatch.StartNew();
        }

        public void Dispose() {
            TimeSinceLastUpdate.Stop();
            Instances.Remove(this);
        }

        public bool NeedsUpdate
            => UpdateFlagRaised // Something requested an update or if the resource lifespan elapsed
            || ((ResourceLifespan == 0 ? Server.MaxCacheAge : ResourceLifespan) < TimeSinceLastUpdate.ElapsedMilliseconds / 1000)
            || !AllowCaching // Or if caching is disabled
            || Program.Mode == Mode.Development; // Or if cache is in development mode
    }
}