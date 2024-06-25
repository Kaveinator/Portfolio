using WebServer.Http;
using Portfolio.DevLog.Controllers;
using Markdig;
using Portfolio.Contact.Controllers;
using Portfolio.Projects.Controllers;
using Portfolio.Homepage.Controllers;

namespace Portfolio
{
    public class PortfolioEndpoint : HttpEndpointHandler {
        public static MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
                .UseEmphasisExtras()    // Adds strikethough and stuff
                .UseGenericAttributes() // Includes GenericAttributesExtension
                .Build();
        public readonly PortfolioDatabase Database;
        public readonly HomepageController HomepageController;
        public readonly ContactController ContactController;
        public readonly ProjectController ProjectController;
        public readonly OrganizationController OrganizationController;
        public readonly DevLogController DevLogController;

        public PortfolioEndpoint(HttpServer server) : base("kavemans.dev", server) {
            Database = PortfolioDatabase.GetOrCreate();
            HomepageController = new HomepageController(this);
            ContactController = new ContactController(this);
            ProjectController = new ProjectController(this);
            OrganizationController = new OrganizationController(this);
            DevLogController = new DevLogController(this);
        }
    }
}