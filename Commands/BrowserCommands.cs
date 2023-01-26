using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebServer.Commands {
    public struct BrowserCommands : ICommand {
        static EventLogger Logger => EventLogger.GetOrCreate<BrowserCommands>();
        public string Name => "Browser Commands";
        public string Description => "Opens the browser";
        public string[] Aliases => new[] { "browse", "open", "visit" };

        public void Execute(string[] args) {
            if (args.Length > 1) { Help(args); return; }
            if (Program.Endpoint?.IsListening ?? false)
                OpenUrl($"http://localhost:{Program.Endpoint.Port}");
            else Logger.LogExplicit("Endpoint not ready");
        }

        public static void OpenUrl(string url) {
            try {
                Process.Start(url);
            }
            catch {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", url);
                }
                else {
                    throw;
                }
            }
        }

        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join('/', Aliases)}> [?]\n" +
            $"\nOptions:" +
            $"\n  ? - Help menu";
    }
}
