using SimpleJSON;
using System.Runtime.CompilerServices;

namespace WebServer {
    public static class Properties {
        public static Dictionary<string, JSONFile> Configs = new Dictionary<string, JSONFile>();
        static EventLogger Logger = EventLogger.GetOrCreate(typeof(Properties));

        public static JSONFile GetOrCreate<T>() => GetOrCreate(typeof(T).Name);
        public static JSONFile GetOrCreate(Type t) => GetOrCreate(t.Name);
        public static JSONFile GetOrCreate(string name) {
            if (string.IsNullOrEmpty(name)) return null;
            if (!Configs.TryGetValue(name, out JSONFile file))
                Configs.Add(name, file = new JSONFile($"configs/{name}.json").Load());
            return file;
        }

        public static bool TryGet(string name, out JSONFile config)
            => Configs.TryGetValue(name, out config);

        public static async void ReloadAll() {
            foreach (KeyValuePair<string, JSONFile> pair in Configs) {
                await pair.Value.LoadAsync();
                Logger.Log($"Reloaded '{pair.Key}'");
            }
        }
        public static async void SaveAll() {
            foreach (KeyValuePair<string, JSONFile> pair in Configs) {
                await pair.Value.SaveAsync();
                Logger.Log($"Reloaded '{pair.Key}'");
            }
        }
    }
}
 