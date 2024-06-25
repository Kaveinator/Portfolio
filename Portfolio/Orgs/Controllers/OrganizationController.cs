using System.Net;
using System.Text.RegularExpressions;
using ExperimentalSQLite;
using Markdig;
using Markdig.Extensions.Tables;
using Microsoft.VisualBasic;
using Portfolio.Orgs.Data;
using Portfolio.Orgs.Models;
using Portfolio.Projects.Data;
using Portfolio.Projects.Models;
using Portfolio.Technologies.Data;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Projects.Controllers {
    public class OrganizationController {
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

        public OrganizationController(PortfolioEndpoint endpoint) {
            Endpoint = endpoint;
            Endpoint.TryAddEventCallback(@"^/orgs/[^/]+$", OnOrgPageRequested);
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

        public HttpResponse OnOrgPageRequested(HttpListenerRequest request) {
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

            OrganizationPageModel orgPageModel = new OrganizationPageModel(Endpoint, org);
            return orgPageModel.Render();
        }
    }
}