using Portfolio.Orgs.Data;
using Portfolio.Orgs.Models;
using Portfolio.Projects.Controllers;
using Portfolio.Projects.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Projects.Models {
    // Considering to make this a part of homepage, since it it a home page for orgs/projects
    public class ProjectHomePageModel : IPageModel {
        public Dictionary<string, object> Values => new() {
            { nameof(Organizations), Organizations },
            { nameof(LooseProjects), LooseProjects }
        };
        readonly HttpEndpointHandler EndpointProvider;
        public readonly IEnumerable<OrganizationEntryModel> Organizations;
        public readonly IEnumerable<ProjectEntryModel> LooseProjects;

        public ProjectHomePageModel(PortfolioEndpoint endpoint, IEnumerable<OrganizationInfo> orgs, IEnumerable<ProjectInfo> looseProjects) {
            EndpointProvider = endpoint;
            Organizations = orgs.OrderByDescending(org => org.StartTimestamp.Value)
                .Select(org => new OrganizationEntryModel(EndpointProvider, org));
            LooseProjects = looseProjects.OrderByDescending(project => project.StartTimestamp.Value)
                .Select(project => new ProjectEntryModel(endpoint, project));
        }

        public HttpResponse Render() {
            if (!EndpointProvider.TryGetTemplate("/Projects.html", out string content, out var statusModel, this))
                return EndpointProvider.GetGenericStatusPage(statusModel);

            return new HttpResponse(HttpStatusCode.OK, content, MimeTypeMap.GetMimeType(".html"), true);
        }
    }
}
