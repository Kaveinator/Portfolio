using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;

namespace WebServer.Commands {
    internal struct CacheControl : ICommand {
        static EventLogger Logger = EventLogger.GetOrCreate<CacheControl>(true);
        enum Action { Purge, View, MaxAge }

        public string Name => "Cache Control";
        public string Description => "Mofify cache policies";
        public string[] Aliases => new[] { "cache", "c" };

        public void Execute(string[] args) {
            if (args.Length == 1) { Help(args); return; }
            if (!Enum.TryParse<Action>(args[1], true, out Action action)) {
                Logger.LogWarning($"Unable to parse '{args[1]}' as Action");
                return;
            }
            switch (action) {
                case Action.Purge:
                    Logger.LogExplicit($"Purged {CachedResource.RaiseAllUpdateFlags()} resource(s)");
                    break;
                case Action.View:
                    StringBuilder sb = new StringBuilder($"Listing {CachedResource.Instances.Count} resource(s)");
                    
                    foreach (var resource in CachedResource.Instances) {
                        sb.Append($"\n{(resource.NeedsUpdate ? "EXPIRED" : "ACTIVE ")} : {resource.Name}");
                    }
                    Logger.LogExplicit(sb);
                    break;
                case Action.MaxAge:
                    bool valueChanged = false;
                    if (args.Length >= 3) {
                        TimeSpan newT = TimeSpan.Zero;
                        foreach (string input in args.Skip(2)) {
                            int index;
                            for (index = 0; index < input.Length; index++)
                                if (char.IsLetter(input[index]))
                                    break;
                            string type = input.Substring(index);
                            if (!double.TryParse(input.Substring(0, index), out double value))
                                continue;
                            newT += type switch {
                                "w" => TimeSpan.FromDays(value * 7),
                                "d" => TimeSpan.FromDays(value),
                                "h" => TimeSpan.FromHours(value),
                                "m" => TimeSpan.FromMinutes(value),
                                "s" => TimeSpan.FromSeconds(value),
                                "ms" => TimeSpan.FromMilliseconds(value),
                                _ => TimeSpan.Zero
                            };
                        }
                        if (valueChanged = newT != TimeSpan.Zero)
                            Program.Endpoint.MaxCacheAge = (uint)newT.TotalSeconds;
                    }
                    TimeSpan t = TimeSpan.FromSeconds(Program.Endpoint.MaxCacheAge);
                    Logger.LogExplicit($"Cache lifespan {(valueChanged ? "changed to" : "is")} {string.Format("{0:D1} day(s) {1:D1} hour(s) {2:D1} minute(s) {3:D1} second(s)", (int)t.TotalDays, t.Hours, t.Minutes, t.Seconds)}");
                    if (valueChanged && Program.Mode == Mode.Development)
                        Logger.LogWarning($"Currently in development mode! Caching is disbled during dev mode", true);
                    break;
            }
        }

        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join("/", Aliases)}> <Action> [...] [?]\n" +
            $"\nActions:" +
            $"\n  {Action.Purge} {args} - Static resources are cached" + // TODO: Make a filter param, only from this domain/resource that has string in it
            $"\n  {Action.View} {args} - Shows list of cached resources, and their status" + // TODO: Make a filter, ex: only from this domain
            $"\n  {Action.MaxAge} <string[]: timespan> - Sets the value of age, if no age parameter is passed, shows the current age" +
            $"\n         - timespan units: (w)eek, (d)ay, (h)our, (m)inute, (s)econd, ms => millisecond" +
            $"\n           Ex: 1w 1d 2h " +
            $"\n  ? - Help menu" +
            $"\n" +
            $"\nExamples:" +
            $"\n > {args[0]} {Action.Purge}" +
            $"\n > {args[0]} {Action.MaxAge} 1h";
    }
}
