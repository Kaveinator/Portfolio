using Portfolio.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portfolio.Commands {
    public struct SeedDatabase : ICommand {
        static EventLogger Logger => EventLogger.GetOrCreate<SeedDatabase>();
        public string Name => "Stop command";
        public string Description => "Seeds the current database";
        public string[] Aliases => new[] { "seed", "seeddb", "sdb" };

        public void Execute(string[] args) {
            if (args.Length > 1) { Logger.Log(Help(args), true); return; }
            if (Program.Mode != Mode.Development) {
                Logger.LogError("Only able to seed if server set to development mode");
                return;
            }
            PortfolioDatabase database = Program.PortfolioEndpoint.Database;
            // Create orgs
            string dummyMarkdown = "Quaerat neque eveniet aut veritatis laboriosam ab incidunt. Dicta enim mollitia possimus eveniet facilis excepturi qui. Aut quisquam praesentium fugit ea nobis mollitia asperiores." +
                "\n### Header" +
                "\nThis paragraph contains a [link](https://kavemans.dev) and an\\" +
                "\n![image](/favicon.ico)";
            foreach (var org in new (string name, string urlName, string role, string brief, string? subheaderOverride, string overview, bool published)[] {
                ("Tech Academy", "TechAcademy", "Student", "Details about The Tech Academy", "What is it and any did I enroll", dummyMarkdown, true),
                ("Tanki X", "TankiX", "Unity Developer", "Details about the RTX Project", null, dummyMarkdown, true),
                ("Area 51", "Area51", "???", "Not supposed to be available publicly", null, dummyMarkdown, false)
            }) {
                var row = new OrganizationInfo(database.OrganizationTable);
                row.Name.Value = org.name;
                row.UrlName.Value = org.urlName;
                row.Role.Value = org.role;
                row.BriefText.Value = org.brief;
                row.OverviewSubheaderOverride.Value = org.subheaderOverride;
                row.OverviewMarkdown.Value = org.overview;
                row.IsPublished.Value = org.published;
                _ = row.Push();
            }
            // Org Links
            foreach (var link in new(long orgId, string? iconOverride, string? linkTextOverride, string href, bool isPublished)[] {
                (2, null, "Live Site", "https://txrevive.net", true),
                (2, "\\f09b", null, "https://github.com/TXRevive", true),
                (2, "\\f09b", null, "https://discord.com/invite/dqq3RXY", true),
                (2, "\\f09b", null, "https://stats.uptimerobot.com/vk4G6sEJkX", true)
            }) {
                var row = new OrganizationLinkInfo(database.OrganizationLinksTable);
                row.OrganizationId.Value = link.orgId;
                row.IconOverride.Value = link.iconOverride;
                row.LinkTextOverride.Value = link.linkTextOverride;
                row.Href.Value = link.href;
                row.IsPublished.Value = link.isPublished;
                _ = row.Push();
            }

            // Projects
            foreach (var proj in new (long? orgId, string name, string urlName, string role, string briefText, string overview, bool published)[] {
                (null, "SkyChat", "SkyChat", "UI/UX Engineer", "Some info about the discord clone", dummyMarkdown, true),
                (1, "TheaterCMS3", "Fullstack Developer", "C# Programmer", "Info about this ASP.NET project", dummyMarkdown, false),
                (1, "Car Insurance App", "CarInsuranceMVC", "C# Programmer", "Info about this ASP.NET project", dummyMarkdown, true)
            }) {
                var row = new ProjectInfo(database.ProjectsTable);
                row.OrginizationId.Value = proj.orgId;
                row.Name.Value = proj.name;
                row.UrlName.Value = proj.urlName;
                row.Role.Value = proj.role;
                row.BriefText.Value = proj.briefText;
                row.OverviewMarkdown.Value = proj.overview;
                row.IsPublished.Value = proj.published;
                _ = row.Push();
            }
        }
        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join('/', Aliases)}> [?]\n" +
            $"\nOptions:" +
            $"\n  ? - Help menu";
    }
}
