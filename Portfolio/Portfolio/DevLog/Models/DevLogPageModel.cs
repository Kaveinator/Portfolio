using Portfolio.Common.Models;
using Portfolio.DevLog.Controllers;
using Portfolio.DevLog.Data;
using Portfolio.DevLog.Models;
using Portfolio.Projects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.DevLog.Models {
    public class DevLogPageModel : IPageModel {
        Dictionary<string, object> Values => new Dictionary<string, object>(Post.Values
            .Update(nameof(Post.ContentMarkdown), _ => Markdig.Markdown.ToHtml(Post.ContentMarkdown.Value, PortfolioEndpoint.MarkdownPipeline))
            .UpdateKeys(key => $"{nameof(Post)}.{key}")
        ) {
            { nameof(Tags), Tags },
            { nameof(ProjectsContainer), ProjectsContainer },
            { nameof(OtherPosts), OtherPosts }
        };
        Dictionary<string, object> IDataModel.Values => Values;

        readonly PortfolioEndpoint Resources;
        readonly DevLogPostInfo Post;
        public ContainerModel ProjectsContainer;
        public ContainerModel OtherPosts;
        readonly IEnumerable<DevLogTagInfo> Tags;

        public DevLogPageModel(DevLogController controller, DevLogPostInfo post) {
            Resources = controller.Endpoint;
            Post = post;
            // Get all TagBindings
            DevLogTagBindingsTable tagBindingsTable = controller.DevLogTagBindingsTable;
            DevLogTagsTable tagsTable = controller.DevLogTagsTable;
            var bindings = tagBindingsTable.GetTagBindingsForPost(post);
            Tags = tagsTable.GetTagsFromBindingsSet(bindings);

            var projects = Resources.Database.DevLogProjectBindingsTable.GetProjects(post, false);
            if (projects.Any()) {
                ProjectsContainer = new ContainerModel("Refrenced Projects", "Project this post referred to");
                ProjectsContainer.ClassList.Add("hr");
                ProjectsContainer.Content = projects.Select(project => new ProjectEntryModel(controller.Endpoint, project));
            }

        }

        public HttpResponse Render() {
            // This resolves tags with the post
            if (!Resources.TryGetTemplate("/DevLog/_EntryLayout.html", out string content, out var statusModel, this))
                return Resources.GetGenericStatusPage(statusModel);
            return new HttpResponse(System.Net.HttpStatusCode.OK, content, MimeTypeMap.GetMimeType(".html"), true);
        }
    }
}
