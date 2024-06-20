using Portfolio.DevLog.Controllers;
using Portfolio.DevLog.Data;
using Portfolio.DevLog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;

namespace Portfolio.DevLog.Models {
    public class DevLogPageModel : IPageModel {
        readonly Dictionary<string, object> Values;
        Dictionary<string, object> IPageModel.Values => Values;

        readonly PortfolioEndpoint Resources;
        readonly DevLogPostInfo Post;
        readonly IEnumerable<DevLogTagInfo> Tags;

        public DevLogPageModel(DevLogController controller, DevLogPostInfo post) {
            Resources = controller.Endpoint;
            Post = post;
            // Get all TagBindings
            DevLogTagBindingsTable tagBindingsTable = controller.DevLogTagBindingsTable;
            DevLogTagsTable tagsTable = controller.DevLogTagsTable;
            var bindings = tagBindingsTable.GetTagBindingsForPost(post);
            Tags = tagsTable.GetTagsFromBindingsSet(bindings);

            Values = new Dictionary<string, object>(post.Values) {
                { nameof(Tags), Tags.ToHtml() }
            }.Update(nameof(post.ContentMarkdown), _ => Markdig.Markdown.ToHtml(post.ContentMarkdown.Value, PortfolioEndpoint.MarkdownPipeline));
        }

        public HttpResponse Render() {
            // This resolves tags with the post
            if (!Resources.TryGetTemplate("/DevLog/_EntryLayout.html", out string content, out var statusModel, this))
                return Resources.GetGenericStatusPage(statusModel);
            return new HttpResponse(System.Net.HttpStatusCode.OK, content, MimeTypeMap.GetMimeType(".html"), true);
        }
    }
}
