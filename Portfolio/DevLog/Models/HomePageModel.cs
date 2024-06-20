using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;
using Portfolio.DevLog.Controllers;
using Portfolio.DevLog.Data;

namespace Portfolio.DevLog.Models {
    public class HomePageModel : IPageModel {
        Dictionary<string, object> Values = new();
        Dictionary<string, object> IPageModel.Values => Values;

        readonly PortfolioEndpoint Resources;
        readonly IEnumerable<DevLogPostInfo> Posts;
        readonly IEnumerable<DevLogTagBindingInfo> TagBindings;
        readonly IEnumerable<DevLogTagInfo> Tags;

        public HomePageModel(DevLogController controller, IEnumerable<DevLogPostInfo> posts) {
            Resources = controller.Endpoint;
            Posts = posts.Where(post => post.IsPublished.Value)
                .OrderByDescending(post => post.CreatedTimestamp.Value);

            // Get all TagBindings
            DevLogTagBindingsTable tagBindingsTable = controller.DevLogTagBindingsTable;
            TagBindings = tagBindingsTable.GetAllTagsForPosts(posts); // This only gets tags bindings for specified posts

            // Get all Tags (from bindings)
            DevLogTagsTable tagsTable = controller.DevLogTagsTable;
            Tags = tagsTable.GetTagsFromBindingsSet(TagBindings); // This only grabs tags refrenced by TagBindings
        }

        public HttpResponse Render() {
            // This resolves tags with the post
            var postsWithTags = Posts.Select(post =>
                new KeyValuePair<DevLogPostInfo, IEnumerable<DevLogTagInfo>>(
                    post,
                    TagBindings.Where(bind => bind.PostId.Value == post.PostId.Value)
                    .Join(Tags,
                        bind => bind.TagId.Value,
                        tag => tag.TagId.Value,
                        (tb, tag) => tag
                    )
                )
            );
            Values[nameof(Posts)] = string.Join('\n', postsWithTags.Select(kvp => new DevLogEntryModel(Resources, kvp.Key, kvp.Value).Render()));
            if (!Resources.TryGetTemplate("/DevLog/_Layout.html", out string content, out var statusModel, this))
                return Resources.GetGenericStatusPage(statusModel);
            return new HttpResponse(System.Net.HttpStatusCode.OK, content, MimeTypeMap.GetMimeType(".html"), true);
        }
    }
}
