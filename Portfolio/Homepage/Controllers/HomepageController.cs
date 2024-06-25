using System;
using System.Net;
using Portfolio.Homepage.Models;
using Portfolio.Orgs.Data;
using Portfolio.Projects.Data;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Homepage.Controllers {
    public class HomepageController {
        public readonly PortfolioEndpoint Endpoint;
        public OrganizationTable OrganizationTable => Endpoint.Database.OrganizationTable;
        public ProjectsTable ProjectsTable => Endpoint.Database.ProjectsTable;
        public HomepageController(PortfolioEndpoint endpoint) {
            Endpoint = endpoint;
            Endpoint.TryAddEventCallback(@"^(/|^$|\^/\$$)$", OnHomePageRequested);
        }

        HttpResponse? OnHomePageRequested(HttpListenerRequest request) {
            IEnumerable<ProjectInfo> featuredProjects = ProjectsTable.GetMostRecentProjects(4);
            IEnumerable<OrganizationInfo> relatedOrganizations = OrganizationTable.GetOrgInfoFromProjectInfo(featuredProjects, true);

            var featuredProjectModels = featuredProjects.Join(relatedOrganizations,
                projInfo => projInfo.OrganizationId.Value,
                techUsedInfo => techUsedInfo.OrganizationId.Value,
                (techInfo, techUsedInfo) => new FeaturedProjectModel(Endpoint, techInfo, techUsedInfo)
            );

            return new HomePageModel(Endpoint, featuredProjectModels).Render();
        }
    }
}
