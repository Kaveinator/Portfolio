using ExperimentalSQLite;
using Markdig;
using Portfolio.Orgs.Data;
using Portfolio.Projects.Data;
using Portfolio.Projects.Models;
using Portfolio.Technologies.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Orgs.Models {
    public class OrganizationPageModel : IPageModel {
        public Dictionary<string, object> Values => new Dictionary<string, object>(Organization.Values
            .Update(nameof(Organization.OverviewMarkdown), _ => Markdown.ToHtml(Organization.OverviewMarkdown, PortfolioEndpoint.MarkdownPipeline))
            .UpdateKeys(key => $"{nameof(Organization)}.{key}")
       ) {
            { nameof(OrganizationLinks), !OrganizationLinks.Any() ? string.Empty : $"<span class=\"linksBox centerize\">{string.Join("", OrganizationLinks.Select(link => link.Render()))}</span>" },
            { nameof(OverviewSubtitle), OverviewSubtitle },
            { nameof(Projects), Projects.OrderByDescending(entry => entry.Project.StartTimestamp.Value) }
        };

        readonly PortfolioEndpoint Endpoint;
        public readonly OrganizationInfo Organization;
        public readonly IEnumerable<OrganizationLinkInfo> OrganizationLinks;
        public string OverviewSubtitle => Organization.OverviewSubheaderOverride.Value ?? "A breif history about this project";
        public readonly IEnumerable<ProjectEntryModel> Projects;

        public OrganizationPageModel(PortfolioEndpoint endpoint, OrganizationInfo org) {
            Endpoint = endpoint;
            Organization = org;

            OrganizationLinksTable linksTable = endpoint.Database.OrganizationLinksTable;
            OrganizationLinks = linksTable.Query(
                new WhereClause<long>(linksTable.Schema.OrganizationId, "=", org.OrganizationId),
                new WhereClause<bool>(linksTable.Schema.IsPublished, "=", true)
            );

            ProjectsTable projectsTable = endpoint.Database.ProjectsTable;
            Projects = projectsTable.Query(
                new WhereClause<long?>(projectsTable.Schema.OrganizationId, "=", org.OrganizationId),
                new WhereClause<bool>(projectsTable.Schema.IsPublished, "=", true)
            ).Select(project => new ProjectEntryModel(endpoint, project, org));
        }

        public HttpResponse Render() {
            if (!Endpoint.TryGetTemplate("/OrgPage/_Layout.html", out string renderedContent, out var statusModel, this))
                return Endpoint.GetGenericStatusPage(statusModel);
            return new HttpResponse() {
                StatusCode = HttpStatusCode.OK,
                AllowCaching = true,
                MimeString = "text/html",
                ContentString = renderedContent
            };
        }
    }
}
