using System.Data.Entity;
using System.Net;
using System.Text.RegularExpressions;
using ExperimentalSQLite;
using Markdig;
using Markdig.Extensions.Tables;
using Microsoft.VisualBasic;
using Portfolio.Projects;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Controllers {
    public class ProjectController {
        public readonly PortfolioEndpoint Endpoint;
        public OrganizationTable OrganizationTable => Endpoint.Database.OrganizationTable;
        public ProjectsTable ProjectsTable => Endpoint.Database.ProjectsTable;
        public ProjectController(PortfolioEndpoint endpoint) {
            Endpoint = endpoint;
            Endpoint.TryAddEventCallback(@"^/projects$", Index);
            Endpoint.TryAddEventCallback(@"^/orgs/[^/]+$", OnOrgPageRequested);
            Endpoint.TryAddEventCallback(@"^/orgs/[^/]+/[^/]+$", OnOrgProjectRequested);
            Endpoint.TryAddEventCallback(ImagePathRegex, OnImageRequest);
            
        }

        readonly Regex ImagePathRegex = new Regex(@"^/src/orgs/(?:(?<orgName>[^/]+)/)?(?<projName>[^/.]+)\.(?<imgLabel>[^/.]+)\.webp$", RegexOptions.IgnoreCase);
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
            ProjectsHomeModel model = new ProjectsHomeModel(Endpoint, this) {
                Organizations = OrganizationTable.Query(new WhereClause<bool>(OrganizationTable.Schema.IsPublished, " = ", true)),
                LooseProjects = ProjectsTable.Query(
                    new WhereClause<long?>(ProjectsTable.Schema.OrganizationId, "IS", null),
                    new WhereClause<bool>(ProjectsTable.Schema.IsPublished, "=", true)
                )
            };

            return model.Render();
        }

        public HttpResponse? OnOrgPageRequested(HttpListenerRequest request) {
            string orgName = request.Url!.LocalPath.Trim('/').Split('/').Last(); // Issue is, whatif /org/fdsf/TankiX/Madness, now this is broken
            // Get org from DB
            OrganizationTable table = Endpoint.Database.OrganizationTable;
            OrganizationInfo? org = Endpoint.Database.OrganizationTable.Query(1,
                new WhereClause<string>(table.Schema.UrlName, "=", orgName) // Needs collate no case type of clause
            ).FirstOrDefault();
            if (org == null) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound,
                subtitle: $"The organization with id '{orgName}' was not found"
            ));
            if (!org.IsPublished) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.Forbidden,
                subtitle: $"The '{org.Name}' organization page is not currently unavailable. Check back later"
            ));
            IEnumerable<ProjectInfo> projects = ProjectsTable.Query(
                new WhereClause<long?>(ProjectsTable.Schema.OrganizationId, "=", org.OrganizationId),
                new WhereClause<bool>(ProjectsTable.Schema.IsPublished, "=", true)
            );
            OrganizationLinksTable linksTable = Endpoint.Database.OrganizationLinksTable;
            OrganizationPageModel orgPageModel = new OrganizationPageModel(this) {
                OrganizationInfo = org,
                OrganizationLinks = linksTable.Query(
                    new WhereClause<long>(linksTable.Schema.OrganizationId, "=", org.OrganizationId),
                    new WhereClause<bool>(linksTable.Schema.IsPublished, "=", true)
                ),
                Projects = projects
            };
            if (!Endpoint.TryGetTemplate("/OrgPage/_Layout.html", out string renderedContent, out var statusModel, orgPageModel))
                return Endpoint.GetGenericStatusPage(statusModel);
            return new HttpResponse() {
                StatusCode = HttpStatusCode.OK,
                AllowCaching = true,
                MimeString = "text/html",
                ContentString = renderedContent
            };
        }

        public async Task<HttpResponse?> OnOrgProjectRequested(HttpListenerRequest request) {
            IEnumerable<string> paths = request.Url!.LocalPath.Trim('/').Split('/').Skip(1);
            if (paths.Count() > 2) return null; // Its not a project page
            string orgName = paths.ElementAt(0),
                projName = paths.ElementAt(1);
            OrganizationInfo? org = OrganizationTable.Query(1,
                new WhereClause<string>(OrganizationTable.Schema.UrlName, "=", orgName) // Needs collate no case type of clause
            ).FirstOrDefault();
            if (org == null) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound,
                subtitle: $"The organization with id '{orgName}' was not found"
            ));
            if (!org.IsPublished) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.Forbidden,
                subtitle: $"The '{org.Name}' organization page is not currently unavailable. Check back later"
            ));
            ProjectInfo? proj = ProjectsTable.Query(1,
                new WhereClause<long?>(ProjectsTable.Schema.OrganizationId, "=", org.OrganizationId),
                new WhereClause<string>(ProjectsTable.Schema.UrlName, "=", projName),
                new WhereClause<bool>(ProjectsTable.Schema.IsPublished, "=", true)
            ).FirstOrDefault();
            if (proj == null) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound,
                subtitle: $"The project with id '{projName}' was not found"
            ));
            if (!proj.IsPublished) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.Forbidden,
                subtitle: $"The '{proj.Name}' project page is not currently available"
            ));
            var requestUri = new Uri($"http://{request.Headers["Host"]}{request.Url.PathAndQuery}");
            OrganizationProjectPageModel pageModel = new OrganizationProjectPageModel(this, org, proj, requestUri);
            return new HttpResponse() {
                StatusCode = HttpStatusCode.OK,
                AllowCaching = true,
                ContentString = pageModel.Render(),
                MimeString = "text/html"
            };
        }
    }
}