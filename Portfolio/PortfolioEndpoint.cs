using System.Net;
using System.Web;
using System.Collections.Specialized;
using WebServer.Http;
using SimpleJSON;
using Portfolio.Commands;
using System.Text.RegularExpressions;
using System.Text;
using Portfolio.Controllers;

namespace Portfolio {
    public class PortfolioEndpoint : HttpEndpointHandler {
        public readonly PortfolioDatabase Database;
        public readonly ContactController ContactController;
        public readonly ProjectController ProjectController;
        public PortfolioEndpoint(HttpServer server) : base("kavemans.dev", server) {
            Database = PortfolioDatabase.GetOrCreate();
            ContactController = new ContactController(this);
            ProjectController = new ProjectController(this);
        }
    }
}