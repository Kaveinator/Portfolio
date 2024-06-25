using Portfolio.Orgs.Data;
using Portfolio.Projects.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Homepage.Models {
    public class FeaturedProjectModel : IPageComponentModel {
        public readonly Dictionary<string, object> Values;
        Dictionary<string, object> IDataModel.Values => Values;
        readonly HttpEndpointHandler EndpointProvider;
        public readonly ProjectInfo ProjectInfo;
        public readonly OrganizationInfo OrganizationInfo;

        public FeaturedProjectModel(HttpEndpointHandler endpointProvider, ProjectInfo projInfo, OrganizationInfo orgInfo) {
            EndpointProvider = endpointProvider;
            ProjectInfo = projInfo;
            OrganizationInfo = orgInfo;
            Values = projInfo.Values.UpdateKeys(key => $"{nameof(projInfo)}.{key}")
                .Concat(orgInfo.Values.UpdateKeys(key => $"{nameof(orgInfo)}.{key}"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public string Render() {
            if (!EndpointProvider.TryGetTemplate($"/Homepage/RecentProjectEntry.html", out string content, out var statusModel, this))
                return statusModel.Details;
            return content;
        }
    }
}
