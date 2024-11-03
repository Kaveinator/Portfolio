using Portfolio.Projects.Data;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.Projects.Models {
    public class ProjectMediaModel : IPageComponentModel {
        public Dictionary<string, object> Values => new() {
            { nameof(MediaTiles), MediaTiles }
        };
        readonly PortfolioEndpoint Endpoint;
        public readonly IEnumerable<AdapterComponent> MediaTiles;

        public ProjectMediaModel(PortfolioEndpoint endpoint, IEnumerable<ProjectMediaInfo> media) {
            Endpoint = endpoint;
            MediaTiles = media.Select(
                info => new AdapterComponent(endpoint, "/ProjectPage/MediaContainer.Tile.html", info)
            );
        }

        public ProjectMediaModel(PortfolioEndpoint endpoint, ProjectInfo project)
            : this(endpoint, endpoint.Database.ProjectMediaTable.GetFromProject(project)) { }

        public string Render() => MediaTiles.Any() ? Endpoint.GetTemplate("/ProjectPage/MediaContainer.html", this) : string.Empty;
    }

    // Something I wanted to experiment with, there are better ways to make this though
    public class AdapterComponent : IPageComponentModel {
        Dictionary<string, object> IDataModel.Values => DataModel.Values;
        readonly HttpEndpointHandler? ViewProvider;
        readonly string ViewPath;
        readonly IDataModel DataModel;

        public AdapterComponent(HttpEndpointHandler viewProvider, string viewPath, IDataModel dataModel) {
            ViewProvider = viewProvider;
            ViewPath = viewPath;
            DataModel = dataModel;
        }

        public AdapterComponent(string viewPath, IDataModel dataModel) {
            ViewPath = viewPath;
            DataModel = dataModel;
        }

        public string Render() => ViewProvider?.GetTemplate(ViewPath, DataModel)
            ?? HttpTemplates.Get(ViewPath, DataModel);
    }
}
