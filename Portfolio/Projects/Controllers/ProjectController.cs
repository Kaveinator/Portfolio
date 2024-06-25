using System.Net;
using System.Text.RegularExpressions;
using ExperimentalSQLite;
using Portfolio.Orgs.Data;
using Portfolio.Projects.Data;
using Portfolio.Projects.Models;
using Portfolio.Technologies.Data;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Projects.Controllers {
    public class ProjectController {
        public readonly PortfolioEndpoint Endpoint;
        #region Relevant Tables
        public OrganizationTable OrganizationTable => Endpoint.Database.OrganizationTable;
        public OrganizationLinksTable OrganizationLinksTable => Endpoint.Database.OrganizationLinksTable;

        public ProjectsTable ProjectsTable => Endpoint.Database.ProjectsTable;
        public ProjectLinksTable ProjectLinksTable => Endpoint.Database.ProjectLinksTable;
        public TechnologiesTable TechnologiesTable => Endpoint.Database.TechnologiesTable;
        public TechnologiesUsedTable TechnologiesUsedTable => Endpoint.Database.TechnologiesUsedTable;
        public ProjectMediaTable ProjectMediaTable => Endpoint.Database.ProjectMediaTable;
        #endregion

        public ProjectController(PortfolioEndpoint endpoint) {
            Endpoint = endpoint;
            Endpoint.TryAddEventCallback(@"^/projects$", Index);
            Endpoint.TryAddEventCallback(@"^/projects/[^/]+$", OnProjectRequested);
            Endpoint.TryAddEventCallback(@"^/orgs/[^/]+/[^/]+$", OnOrgProjectRequested);
            Endpoint.TryAddEventCallback(ImagePathRegex, OnImageRequest);

        }

        readonly Regex ImagePathRegex = new Regex(@"^/src/projects/(?<projName>[^/.]+)\.(?<imgLabel>[^/.]+)\.webp$", RegexOptions.IgnoreCase);
        public HttpResponse? OnImageRequest(HttpListenerRequest request) {
            HttpResponse staticResponse = Endpoint.HttpServer.GetStaticFile(request);
            if (staticResponse.IsSuccessStatusCode)
                return staticResponse;
            var match = ImagePathRegex.Match(request.Url?.LocalPath ?? string.Empty);
            if (match.Success) {
                string? orgName = match.Groups.TryGetValue(nameof(orgName), out var grp) ? grp.Value : null,
                    projName = match.Groups.TryGetValue(nameof(projName), out grp) ? grp.Value : null,
                    imgLabel = match.Groups.TryGetValue(nameof(imgLabel), out grp) ? grp.Value : null;
                // Currently routes to a single image, but will need to look at org banners => then default banners
                return "Default".IsOfAny(orgName, projName) ? staticResponse : Endpoint.HttpServer.GetStaticFile(request.Url.Host, $"/src/orgs/Default.{imgLabel}.webp");
            }

            return staticResponse;
        }

        public HttpResponse? Index(HttpListenerRequest request) {
            ProjectHomePageModel model = new ProjectHomePageModel(Endpoint, 
                OrganizationTable.Query(new WhereClause<bool>(OrganizationTable.Schema.IsPublished, "=", true)),
                ProjectsTable.Query(
                    new WhereClause<long?>(ProjectsTable.Schema.OrganizationId, "IS", null),
                    new WhereClause<bool>(ProjectsTable.Schema.IsPublished, "=", true)
                )
            );
            return model.Render();
        }

        public HttpResponse? OnProjectRequested(HttpListenerRequest request) {
            string projectName = request.Url!.LocalPath.Trim('/').Split('/').Last(); // Issue is, whatif /org/fdsf/TankiX/Madness, now this is broken
            // Get org from DB
            ProjectsTable table = Endpoint.Database.ProjectsTable;
            ProjectInfo? project = table.Query(1,
                new WhereClause<string>(table.Schema.UrlName, "=", projectName), // Needs collate no case type of clause
                new WhereClause<long?>(table.Schema.OrganizationId, "IS", null) // Ensure org is null
            ).FirstOrDefault(defaultValue: null);
            if (project == null) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound,
                subtitle: $"The project with id '{projectName}' was not found"
            ));
            if (!project.IsPublished) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.Forbidden,
                subtitle: $"The '{project.Name}' project is not currently unavailable. Check back later"
            ));

            ProjectPageModel pageModel = new ProjectPageModel(Endpoint, project, null);
            return pageModel.Render();
        }

        public HttpResponse? OnOrgProjectRequested(HttpListenerRequest request) {
            IEnumerable<string> paths = request.Url!.LocalPath.Trim('/').Split('/').Skip(1);
            if (paths.Count() > 2) return null; // Its not a project page
            string orgName = paths.ElementAt(0),
                projName = paths.ElementAt(1);
            OrganizationInfo? org = OrganizationTable.Query(1,
                new WhereClause<string>(OrganizationTable.Schema.UrlName, "=", orgName) // Needs collate no case type of clause
            ).FirstOrDefault(defaultValue: null);
            if (org == null) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound,
                subtitle: $"The organization with id '{orgName}' was not found"
            ));
            if (!org.IsPublished) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.Forbidden,
                subtitle: $"The '{org.Name}' organization page is not currently unavailable. Check back later"
            ));
            ProjectInfo? project = ProjectsTable.Query(1,
                new WhereClause<long?>(ProjectsTable.Schema.OrganizationId, "=", org.OrganizationId),
                new WhereClause<string>(ProjectsTable.Schema.UrlName, "=", projName)
            ).FirstOrDefault(defaultValue: null);
            if (project == null) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound,
                subtitle: $"The project with id '{projName}' was not found"
            ));
            if (!project.IsPublished) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.Forbidden,
                subtitle: $"The '{project.Name}' project page is not currently available"
            ));
            ProjectPageModel pageModel = new ProjectPageModel(Endpoint, project, org);
            return pageModel.Render();
        }
    }
}