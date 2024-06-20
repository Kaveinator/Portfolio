using System.Net;
using ExperimentalSQLite;
using WebServer.Http;
using WebServer.Models;
using Markdig;
using Portfolio.Projects.Data;
using Portfolio.Technologies.Data;
using Portfolio.Orgs.Data;
using Portfolio.Projects.Controllers;

namespace Portfolio.Projects.Models {
    public class ProjectsHomeModel : IPageModel {
        public Dictionary<string, object> Values => new() {
            { nameof(Organizations), string.Join(string.Empty, Organizations?.OrderByDescending(org => org.StartTimestamp.Value).Select(org => EndpointProvider.GetTemplate("/Projects.OrgItem.html", org)) ?? Array.Empty<string>()) },
            { nameof(LooseProjects), string.Join(string.Empty, LooseProjects?.Select(project => EndpointProvider.GetTemplate("/Projects.ProjectItem.html", project)) ?? Array.Empty<string>()) }
        };
        readonly ProjectController Controller;
        readonly HttpEndpointHandler EndpointProvider;
        public IEnumerable<OrganizationInfo> Organizations; // Assume this is assigned
        public IEnumerable<ProjectInfo> LooseProjects; // Assume this is assigned

        public ProjectsHomeModel(HttpEndpointHandler endpointProvider, ProjectController controller) {
            EndpointProvider = endpointProvider;
            Controller = controller;
        }

        public HttpResponse Render() {
            if (!EndpointProvider.TryGetTemplate("/Projects.html", out string content, out var statusModel, this))
                return EndpointProvider.GetGenericStatusPage(statusModel);

            return new HttpResponse(HttpStatusCode.OK, content, MimeTypeMap.GetMimeType(".html"), true);
        }
    }
    public class OrganizationPageModel : IPageModel {
        public Dictionary<string, object> Values => new Dictionary<string, object>(OrganizationInfo.Values) {
            { nameof(OrganizationLinks), !OrganizationLinks.Any() ? string.Empty : $"<span class=\"linksBox centerize\">{string.Join("", OrganizationLinks.Select(RenderLink))}</span>" },
            { nameof(OverviewSubtitle), OverviewSubtitle },
            { nameof(Projects), string.Join("", Projects.OrderByDescending(proj => proj.StartTimestamp.Value).Select(proj => new ProjectEntryPageModel(this, proj).Render())) }
        }.Update(nameof(OrganizationInfo.OverviewMarkdown), _ => Markdown.ToHtml(OrganizationInfo.OverviewMarkdown, PortfolioEndpoint.MarkdownPipeline));

        readonly ProjectController Controller;
        public OrganizationInfo OrganizationInfo;
        public IEnumerable<OrganizationLinkInfo> OrganizationLinks;
        public string OverviewSubtitle => OrganizationInfo.OverviewSubheaderOverride.Value ?? "A breif history about this project";
        public IEnumerable<ProjectInfo> Projects;

        public OrganizationPageModel(ProjectController controller) {
            Controller = controller;
        }

        string RenderLink(OrganizationLinkInfo linkInfo) {
            // Need to create UriDbCell that also has a ConvertValueToSqlString() that will parse it as such
            Uri uri = new Uri(linkInfo.Href);
            string html = $"<a href=\"{uri}\" target=\"_blank\"";
            if (linkInfo.IconOverride.Value != null)
                html += $" style=\"--icon: '{linkInfo.IconOverride.Value}';\"";
            html += $">{(linkInfo.LinkTextOverride.Value != null ? linkInfo.LinkTextOverride.Value : $"{(uri.Host + uri.LocalPath).TrimEnd('/')}")}</a>";
            return html;
        }

        public class ProjectEntryPageModel : IPageModel {
            Dictionary<string, object> IPageModel.Values => new Dictionary<string, object>(ProjectInfo.Values) {
                { nameof(OrgUrlName), OrgUrlName },
                { nameof(OrgName), OrgName },
                { nameof(RenderBadges), RenderBadges() }
            }.Update(nameof(ProjectInfo.ThumbnailOverrideHref), value => value ?? $"/src/sprites/orgs/{OrgUrlName}/{ProjectInfo.UrlName}/_Thumbnail.webp");

            ProjectController Controller;
            OrganizationInfo OrganizationInfo;
            ProjectInfo ProjectInfo;
            public readonly IEnumerable<TechnologyUsedInfo> TechnologiesUsed;
            public readonly bool HasLiveDemo = false;

            string RenderBadges() {
                List<ProjectBadge> badges = TechnologiesUsed.Select(tech => new ProjectBadge(this, tech)).ToList();
                if (HasLiveDemo) badges.Add(new LiveProjectBadge(this));
                return string.Join('\n', badges.Select(badge => badge.Render()));
            }

            string OrgUrlName => OrganizationInfo.UrlName;
            string OrgName => OrganizationInfo.Name;

            public ProjectEntryPageModel(OrganizationPageModel parentPageModel, ProjectInfo projectInfo) {
                Controller = parentPageModel.Controller;
                OrganizationInfo = parentPageModel.OrganizationInfo;
                ProjectInfo = projectInfo;
                TechnologiesUsedTable technologiesUsedTable = parentPageModel.Controller.Endpoint.Database.TechnologiesUsedTable;
                TechnologiesUsed = technologiesUsedTable.Query(
                    new WhereClause<long>(technologiesUsedTable.Schema.ProjectId, "=", ProjectInfo.ProjectId)
                );
                ProjectLinksTable projectLinksTable = parentPageModel.Controller.Endpoint.Database.ProjectLinksTable;
                HasLiveDemo = projectLinksTable.Query(1,
                    new WhereClause<long>(projectLinksTable.Schema.ProjectId, "=", ProjectInfo.ProjectId),
                    new WhereClause<bool>(projectLinksTable.Schema.IsLiveDemo, "=", true),
                    new WhereClause<bool>(projectLinksTable.Schema.IsPublished, "=", true)
                ).Count() > 0;
            }

            public string Render()
                => Controller.Endpoint.GetTemplate("/OrgPage/ProjectEntry.html", this);

            class ProjectBadge : IPageModel {
                public virtual Dictionary<string, object> Values => new[] { ProjectTechnologyInfo.Fields, TechnologyInfo.Fields }.SelectMany(row => row)
                    .DistinctBy(field => field.ColumnName).ToDictionary(field => field.ColumnName, field => field.Value)
                    .Update(nameof(ProjectTechnologyInfo.BadgeTextOverride), value => value ?? TechnologyInfo.DefaultBadgeText);
                public readonly ProjectController Controller;
                public readonly TechnologyUsedInfo ProjectTechnologyInfo;
                public readonly TechnologyInfo TechnologyInfo;
                protected ProjectBadge(ProjectEntryPageModel parentPageModel) {
                    Controller = parentPageModel.Controller;
                }
                public ProjectBadge(ProjectEntryPageModel parentPageModel, TechnologyUsedInfo techUsedInfo) : this(parentPageModel) {
                    ProjectTechnologyInfo = techUsedInfo;
                    TechnologiesTable techTable = Controller.Endpoint.Database.TechnologiesTable;
                    TechnologyInfo = techTable.Query(1,
                        new WhereClause<long>(techTable.Schema.TechId, "=", techUsedInfo.TechId)
                    ).First();
                }

                public string Render() => Controller.Endpoint.GetTemplate("/OrgPage/ProjectBadge.html", this);
            }
            class LiveProjectBadge : ProjectBadge {
                public override Dictionary<string, object> Values => new() {
                    { "EnumName", "live" },
                    { "DefaultBadgeText", "Live Project Badge" },
                    { "BadgeTextOverride", "Live Demo Available" }
                };

                public LiveProjectBadge(ProjectEntryPageModel parentPageModel) : base(parentPageModel) { }
            }
        }
    }
    public class OrganizationProjectPageModel : IPageModel {
        Dictionary<string, object> IPageModel.Values => new Dictionary<string, object>(ProjectInfo.Values) {
            { nameof(OrgName), OrgName },
            { nameof(OrgUrlName), OrgUrlName },
            { nameof(RenderLinks), RenderLinks() },
            { nameof(RenderMediaContainer), RenderMediaContainer() },
            { nameof(TechnologiesUsed), RenderTechnologies() }
        }.Update(nameof(ProjectInfo.OverviewMarkdown), _ => Markdown.ToHtml(ProjectInfo.OverviewMarkdown, PortfolioEndpoint.MarkdownPipeline))
        .Update(nameof(ProjectInfo.HeaderVerticalAnchorOverride), currentValue => currentValue ?? 40);

        const string BaseTemplatePath = "/OrgProjectPage";
        string OrgUrlName => OrganizationInfo.UrlName;
        string OrgName => OrganizationInfo.Name;
        public readonly ProjectController Controller;
        public readonly OrganizationInfo OrganizationInfo;
        public readonly ProjectInfo ProjectInfo;
        public readonly IEnumerable<ProjectLinkInfo> Links;
        public readonly IEnumerable<TechnologyUsedInfo> TechnologiesUsed;
        public readonly IEnumerable<ProjectMediaInfo> MediaTiles;
        public readonly Uri RequestLocation;

        public OrganizationProjectPageModel(ProjectController controller, OrganizationInfo orgInfo, ProjectInfo projInfo, Uri requestLocation) {
            Controller = controller;
            OrganizationInfo = orgInfo;
            ProjectInfo = projInfo;
            ProjectLinksTable linksTable = controller.Endpoint.Database.ProjectLinksTable;
            Links = linksTable.Query(
                new WhereClause<long>(linksTable.Schema.ProjectId, "=", ProjectInfo.ProjectId),
                new WhereClause<bool>(linksTable.Schema.IsPublished, "=", true)
            );
            TechnologiesUsedTable technologiesUsedTable = controller.Endpoint.Database.TechnologiesUsedTable;
            TechnologiesUsed = technologiesUsedTable.Query(
                new WhereClause<long>(technologiesUsedTable.Schema.ProjectId, "=", ProjectInfo.ProjectId)
            );
            ProjectMediaTable mediaTable = controller.Endpoint.Database.ProjectMediaTable;
            MediaTiles = mediaTable.Query(
                new WhereClause<long>(mediaTable.Schema.ProjectId, "=", ProjectInfo.ProjectId),
                new WhereClause<bool>(mediaTable.Schema.IsPublished, "=", true)
            );
            RequestLocation = requestLocation;
        }

        string RenderTechnologies() {
            TechnologiesTable techTable = Controller.Endpoint.Database.TechnologiesTable;
            var techInfo = techTable.GetTechInfoSetFromTechUsedSet(TechnologiesUsed);

            var technologies = techInfo.Join(TechnologiesUsed,
                techInfo => techInfo.TechId.Value,
                techUsedInfo => techUsedInfo.TechId.Value,
                (techInfo, techUsedInfo) => new KeyValuePair<TechnologyInfo, TechnologyUsedInfo>(techInfo, techUsedInfo)
            );

            return string.Join('\n', technologies.Select(tech => new TechUsedModel(Controller.Endpoint, tech.Key, tech.Value).Render()));
        }

        string RenderLinks() => string.Join('\n', Links.Select(linkInfo => {
            Uri uri = Uri.IsWellFormedUriString(linkInfo.Href, UriKind.Absolute) ? new Uri(linkInfo.Href)
                : new Uri(RequestLocation, linkInfo.Href);
            string html = $"<a href=\"{uri}\" target=\"_blank\"";
            if (linkInfo.IconOverride.Value != null)
                html += $" style=\"--icon: '{linkInfo.IconOverride.Value}';\"";
            html += $">{(linkInfo.LinkTextOverride.Value != null ? linkInfo.LinkTextOverride.Value : $"{(uri.Host + uri.LocalPath).TrimEnd('/')}")}</a>";
            return html;
        }));

        Uri BuildUri(Uri requestLocation, string href) => Uri.IsWellFormedUriString(href, UriKind.Absolute) ? new Uri(href)
                : new Uri(requestLocation, href);

        string RenderMediaContainer() => MediaTiles.Any() ? Controller.Endpoint.GetTemplate($"{BaseTemplatePath}/MediaContainer.html", new DynamicPageModel().Add(
            nameof(MediaTiles), string.Join('\n', MediaTiles.Select(tileInfo => Controller.Endpoint.GetTemplate($"{BaseTemplatePath}/MediaContainer.Tile.html", tileInfo))
        ))) : string.Empty;

        public string Render() => Controller.Endpoint.GetTemplate($"{BaseTemplatePath}/_Layout.html", this);

        class TechUsedModel : IPageModel {
            Dictionary<string, object> IPageModel.Values => new[] { ProjectTechnologyInfo.Fields, TechnologyInfo.Fields }.SelectMany(row => row)
                .DistinctBy(field => field.ColumnName).ToDictionary(field => field.ColumnName, field => field.Value)
                .Update(nameof(TechnologyInfo.TitleMarkdown), _ => Markdown.ToHtml(TechnologyInfo.TitleMarkdown, PortfolioEndpoint.MarkdownPipeline))
                .Update(nameof(TechnologyInfo.ContentMarkdown), _ => Markdown.ToHtml(TechnologyInfo.ContentMarkdown, PortfolioEndpoint.MarkdownPipeline))
                .Update(nameof(ProjectTechnologyInfo.DetailsMarkdown), _ => Markdown.ToHtml(ProjectTechnologyInfo.DetailsMarkdown, PortfolioEndpoint.MarkdownPipeline));
            public readonly HttpEndpointHandler ResourceProvider;
            public readonly TechnologyUsedInfo ProjectTechnologyInfo;
            public readonly TechnologyInfo TechnologyInfo;
            public TechUsedModel(HttpEndpointHandler resourceProvider, TechnologyInfo techInfo, TechnologyUsedInfo techUsedInfo) {
                ResourceProvider = resourceProvider;
                TechnologyInfo = techInfo;
                ProjectTechnologyInfo = techUsedInfo;
            }

            public string Render() => ResourceProvider.GetTemplate($"{BaseTemplatePath}/TechItem.html", this);
        }
    }
}