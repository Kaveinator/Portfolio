using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Markdig;
using Portfolio.Projects;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Portfolio.Controllers {
    public class HomepageController {
        public readonly PortfolioEndpoint Endpoint;
        public static MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
                .UseGenericAttributes() // Includes GenericAttributesExtension
                .Build();
        public OrganizationTable OrganizationTable => Endpoint.Database.OrganizationTable;
        public ProjectsTable ProjectsTable => Endpoint.Database.ProjectsTable;
        public HomepageController(PortfolioEndpoint endpoint) {
            Endpoint = endpoint;
            Endpoint.TryAddEventCallback(@"^(/|^$|\^/\$$)$", OnHomePageRequested);
        }

        HttpResponse? OnHomePageRequested(HttpListenerRequest request) {
            IEnumerable<ProjectInfo> featuredProjects = ProjectsTable.GetMostRecentProjects(4);
            IEnumerable<OrganizationInfo> relatedOrganizations = OrganizationTable.GetOrgInfoFromProjectInfo(featuredProjects, true);

            var projectsWithOrgInfo = featuredProjects.Join(relatedOrganizations,
                projInfo => projInfo.OrganizationId.Value,
                techUsedInfo => techUsedInfo.OrganizationId.Value,
                (techInfo, techUsedInfo) => new KeyValuePair<ProjectInfo, OrganizationInfo>(techInfo, techUsedInfo)
            );

            return new HomePageModel(Endpoint, projectsWithOrgInfo).Render();
        }

        public class HomePageModel : IPageModel {
            public const string BasePath = "/Homepage";
            public Dictionary<string, object> Values => new() {
                { nameof(FeaturedProjects), string.Join('\n', FeaturedProjects.Select(model => model.Render())) }
            };
            readonly HttpEndpointHandler EndpointProvider;
            public readonly List<RecentProjectModel> FeaturedProjects = new List<RecentProjectModel>();

            public HomePageModel(HttpEndpointHandler endpointProvider, IEnumerable<KeyValuePair<ProjectInfo, OrganizationInfo>> featuredProjects) {
                EndpointProvider = endpointProvider;
                foreach (var kvp in featuredProjects) {
                    FeaturedProjects.Add(new RecentProjectModel(endpointProvider, kvp.Key, kvp.Value));
                }
            }

            public HttpResponse Render() {
                if (!EndpointProvider.TryGetTemplate($"{BasePath}/_Layout.html", out string content, out var statusModel, this))
                    return EndpointProvider.GetGenericStatusPage(statusModel);
                return new HttpResponse(HttpStatusCode.OK, content, MimeTypeMap.GetMimeType(".html"), false);
            }

            public class RecentProjectModel : IPageModel {
                public readonly Dictionary<string, object> Values;
                Dictionary<string, object> IPageModel.Values => Values;
                readonly HttpEndpointHandler EndpointProvider;
                public readonly ProjectInfo ProjectInfo;
                public readonly OrganizationInfo OrganizationInfo;

                public RecentProjectModel(HttpEndpointHandler endpointProvider, ProjectInfo projInfo, OrganizationInfo orgInfo) {
                    EndpointProvider = endpointProvider;
                    ProjectInfo = projInfo;
                    OrganizationInfo = orgInfo;
                    Values = projInfo.Values.UpdateKeys(key => $"{nameof(projInfo)}.{key}")
                        .Concat(orgInfo.Values.UpdateKeys(key => $"{nameof(orgInfo)}.{key}"))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }

                public string Render() {
                    if (!EndpointProvider.TryGetTemplate($"{BasePath}/RecentProjectEntry.html", out string content, out var statusModel, this))
                        return statusModel.Details;
                    return content;
                }
            }
        }
    }
}
