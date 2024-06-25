using Portfolio.Projects.Data;
using Portfolio.Technologies.Data;
using WebServer.Models;

namespace Portfolio.Projects.Models {
    public class TechUsedModel : IPageComponentModel {
        public Dictionary<string, object> Values { get; } = new Dictionary<string, object>();
        readonly PortfolioEndpoint Endpoint;
        public readonly TechnologyUsedInfo TechnologyUsedInfo;
        public readonly TechnologyInfo LinkedTechnologyInfo;

        public TechUsedModel(PortfolioEndpoint endpoint, TechnologyUsedInfo techUsedInfo, TechnologyInfo techInfo) {
            Endpoint = endpoint;
            TechnologyUsedInfo = techUsedInfo;
            LinkedTechnologyInfo = techInfo;

            techUsedInfo.Values.UpdateKeys(key => $"{nameof(techUsedInfo)}.{key}")
                .Do(kvp => Values.Add(kvp.Key, kvp.Value));
            techInfo.Values.UpdateKeys(key => $"{nameof(techInfo)}.{key}")
                .Do(kvp => Values.Add(kvp.Key, kvp.Value));
        }

        public TechUsedModel(PortfolioEndpoint endpoint, TechnologyUsedInfo techUsedInfo)
            : this(endpoint, techUsedInfo, endpoint.Database.TechnologiesTable.GetTechInfo(techUsedInfo)) { }

        public string Render() => Endpoint.GetTemplate("/ProjectPage/TechUsed.html", this);
    }
}
