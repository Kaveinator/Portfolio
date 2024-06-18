using WebServer.Http;
using Portfolio.Controllers;
using Portfolio.Portfolio.Controllers;

namespace Portfolio {
    public class PortfolioEndpoint : HttpEndpointHandler {
        public readonly PortfolioDatabase Database;
        public readonly HomepageController HomepageController;
        public readonly ContactController ContactController;
        public readonly ProjectController ProjectController;
        public PortfolioEndpoint(HttpServer server) : base("kavemans.dev", server) {
            Database = PortfolioDatabase.GetOrCreate();
            HomepageController = new HomepageController(this);
            ContactController = new ContactController(this);
            ProjectController = new ProjectController(this);
        }
    }
}