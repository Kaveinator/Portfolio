using Portfolio.Orgs.Data;
using Portfolio.Projects.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Homepage.Models {
    public class HomePageModel : IDataModel {
        public Dictionary<string, object> Values => new() {
            { nameof(FeaturedProjects), FeaturedProjects }
        };
        readonly HttpEndpointHandler EndpointProvider;
        public IEnumerable<FeaturedProjectModel> FeaturedProjects;

        public HomePageModel(HttpEndpointHandler endpointProvider, IEnumerable<FeaturedProjectModel> featuredProjects) {
            EndpointProvider = endpointProvider;
            FeaturedProjects = featuredProjects;
        }

        public HttpResponse Render() {
            if (!EndpointProvider.TryGetTemplate("/Homepage/_Layout.html", out string content, out var statusModel, this))
                return EndpointProvider.GetGenericStatusPage(statusModel);
            return new HttpResponse(HttpStatusCode.OK, content, MimeTypeMap.GetMimeType(".html"), false);
        }
    }
}
