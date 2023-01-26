using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;

namespace WebServer.Commands {
    public struct BuildCommand : ICommand {
        static EventLogger Logger = EventLogger.GetOrCreate<BuildCommand>();
        public string Name => "Site Compiler";
        public string Description => "Build/Rebuild/Clear compiled/static site data";
        public string[] Aliases => new[] { "build", "rebuild", "b" };

        const string PurgeFlag = "-purge";
        public void Execute(string[] args) {
            if (args.Length > 1 && Enum.TryParse<Mode>(args[1], true, out Mode targetMode))
                Program.Mode = targetMode;
            bool purgeFlagRaised = args.Contains(PurgeFlag);
            if (purgeFlagRaised) Purge();
            //if ((purgeFlagRaised && args[0].ToLower() == "rebuild") || (!purgeFlagRaised && targetMode == Mode.Production))
                Build();
        }

        public static void Purge() {
            foreach (DirectoryInfo dir in Program.Endpoint.StaticDomainDirectoryInfo.GetDirectories())
                dir.Delete(true);
            foreach (FileInfo file in Program.Endpoint.StaticDomainDirectoryInfo.GetFiles())
                file.Delete();
            //Program.Endpoint.StaticDomainDirectoryInfo.Delete(recursive: true);
        }

        static readonly Encoding Encoding = Encoding.UTF8;
        public static void Build(string relativeDir = "") {
            // Get all files from current dir
            DirectoryInfo currentDirectory = new DirectoryInfo(Path.Combine(HttpTemplates.PublicPath.FullName, relativeDir));
            DirectoryInfo targetDirectory = new DirectoryInfo(Path.Combine(Program.Endpoint.StaticDomainDirectoryInfo.FullName, relativeDir));
            targetDirectory.Create();
            foreach (FileInfo file in currentDirectory.GetFiles()) {
                bool needsCompiling = MimeTypeMap.GetMimeType(Path.GetExtension(file.Name).ToLower()).ToLower().Contains("text");
                byte[] buffer;
                using (FileStream stream = file.OpenRead()) {
                    buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                }
                string assetName = Path.Combine(relativeDir, file.Name);
                if (needsCompiling) {
                    string contents = Encoding.GetString(buffer);
                    contents = HttpTemplates.Process(contents);
                    buffer = Encoding.GetBytes(contents);
                    Logger.Log($"Compiled '{assetName}'");
                }
                else Logger.Log($"Copied '{assetName}'");
                File.WriteAllBytes(Path.Combine(targetDirectory.FullName, file.Name), buffer);
            }
            foreach (DirectoryInfo dir in currentDirectory.GetDirectories()) {
                Build(Path.Combine(relativeDir, dir.Name));
            }
        }

        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join("/", Aliases)}> <Mode> [-s] [?]\n" +
            $"\nOptions:" +
            $"\n  <Mode> - Values: {string.Join("/", Enum.GetNames<Mode>())}" +
            $"\n  {PurgeFlag} - Build/rebuild static/production folder (only effective if <Action> parameter is set to 'Production'" +
            $"\n  ? - Help menu" +
            $"" +
            $"\nExamples:" +
            $"\n > {args[0]} {Mode.Development}" +
            $"\n > {args[0]} {Mode.Production} -b";
    }
}
