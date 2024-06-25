using WebServer.Http;
using WebServer.Models;
using Portfolio.Orgs.Data;

namespace Portfolio.Orgs.Models {
    public class OrganizationEntryModel : IPageComponentModel {
        public Dictionary<string, object> Values { get; }
        readonly HttpEndpointHandler EndpointProvider;
        public readonly OrganizationInfo OrganizationRefrence;

        public OrganizationEntryModel(HttpEndpointHandler endpoint, OrganizationInfo org) {
            EndpointProvider = endpoint;
            OrganizationRefrence = org;
            Values = new Dictionary<string, object>(org.Values);
        }

        public string Render() => EndpointProvider.GetTemplate("/Common/OrgEntry.html", this);
    }
}
