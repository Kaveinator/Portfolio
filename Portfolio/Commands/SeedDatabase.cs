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
                (1, null, null, "https://learncodinganywhere.com/", true),
                (1, "\\f09b", null, "https://github.com/Kaveinator/Tech-Academy-Projects", true),
                (2, null, null, "https://txrevive.net", true),
                (2, "\\f09b", null, "https://github.com/TXRevive", true),
                (2, "\\f392", null, "https://discord.com/invite/dqq3RXY", true),
                (2, "\\f012", null, "https://stats.uptimerobot.com/vk4G6sEJkX", true)
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
                (1, "Car Insurance App", "CarInsuranceMVC", "C# Programmer", "This project utilized ASP.NET MVC to make CRUD pages and Entity Framework as the Database transport.", dummyMarkdown, true)
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

            // Project Links
            foreach (var link in new (long projectId, string? iconOverride, string? linkTextOverride, string href, bool isPublished, bool isLiveDemo)[] {
                (3, null, "Live Demo", "/orgs/TechAcademy/CarInsuranceMVC/demo", true, true),
                (3, "\\f09b", "Source Code", "https://github.com/Kaveinator/Tech-Academy-CSharp-Projects/tree/master/Assignments/CarInsuranceMVC", true, false)
            }) {
                var row = new ProjectLinkInfo(database.ProjectLinksTable);
                row.ProjectId.Value = link.projectId;
                row.IconOverride.Value = link.iconOverride;
                row.LinkTextOverride.Value = link.linkTextOverride;
                row.Href.Value = link.href;
                row.IsPublished.Value = link.isPublished;
                row.IsLiveDemo.Value = link.isLiveDemo;
                _ = row.Push();
            }

            // Technologies
            foreach (var tech in new (string enumName, string defaultBadgeText, string titleMarkdown, string contentMarkdown)[] {
                ("HTML", "HTML", "[Hypertext Markup Language (HTML)](https://developer.mozilla.org/en-US/docs/Web/HTML){target=\"_blank\"}", "HTML is used to define the general hierarchy and relationships between elements on a webpage. HTML itself, usually doesn't look very presentable and looks like a something you'd find in the 90s, so you'll need to use CSS to add color. "),
                ("CSS", "CSS", "[Cascading Style Sheets (CSS)](https://developer.mozilla.org/en-US/docs/Web/CSS){target=\"_blank\"}", "CSS is used in combination with HTML, it is used to \"paint\" the canvas that was built with HTML. However, CSS is not a programming language and does not provide any programmable functionality. "),
                ("CSharp", "C#", "[C# Programming Language](https://dotnet.microsoft.com/en-us/languages/csharp){target=\"_blank\"}", "C# _(pronounced Sea-Sharp)_ is an [Object Oriented Programming _(OOP)_](https://www.techtarget.com/searchapparchitecture/definition/object-oriented-programming-OOP){target=\"_blank\"} language that was developed by Microsoft in 1999. The Unity3D engine can be programmed using C# targetting .NET Framework 4.7.2. In this project, I built several console applications that served as utilities and tools for the project.")
            }) {
                var row = new TechnologyInfo(database.TechnologiesTable);
                row.EnumName.Value = tech.enumName;
                row.DefaultBadgeText.Value = tech.defaultBadgeText;
                row.TitleMarkdown.Value = tech.titleMarkdown;
                row.ContentMarkdown.Value = tech.contentMarkdown;
                _ = row.Push();
            }

            // Bind Technologies to projects
            foreach (var techBind in new (long projectId, long techId, string? badgeTextOverride, string detailsMarkdown)[] {
                (3, 1, null, string.Empty),
                (3, 2, null, string.Empty),
                (3, 3, "C# with ASP.NET", "I utilized C# in this project by writing ..\n - .. writing two CSHTML Views\n - .. writing a Controller that handles and supplied date to the Views\n - .. wrote a ADO.NET implemenetation for storage\n - .. wrote a Entity Framework implementation for storage")
            }) {
                var row = new TechnologyUsedInfo(database.TechnologiesUsedTable);
                row.ProjectId.Value = techBind.projectId;
                row.TechId.Value = techBind.techId;
                row.BadgeTextOverride.Value = techBind.badgeTextOverride;
                row.DetailsMarkdown.Value = techBind.detailsMarkdown;
                _ = row.Push();
            }
        }
        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join('/', Aliases)}> [?]\n" +
            $"\nOptions:" +
            $"\n  ? - Help menu";
    }
}
