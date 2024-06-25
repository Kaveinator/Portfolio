using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;
using ExperimentalSQLite;
using Portfolio.DevLog.Data;
using Portfolio.DevLog.Models;
using Portfolio.DevLog.Models;

namespace Portfolio.DevLog.Controllers {
    public class DevLogController {
        public readonly PortfolioEndpoint Endpoint;
        #region Table Refrences
        public DevLogPostsTable DevLogPostsTable => Endpoint.Database.DevLogPostsTable;
        public DevLogTagsTable DevLogTagsTable => Endpoint.Database.DevLogTagsTable;
        public DevLogTagBindingsTable DevLogTagBindingsTable => Endpoint.Database.DevLogTagBindingsTable;
        public DevLogProjectBindingsTable DevLogProjectBindingsTable => Endpoint.Database.DevLogProjectBindingsTable;
        #endregion
        
        public DevLogController(PortfolioEndpoint endpoint) {
            Endpoint = endpoint;
            Endpoint.TryAddEventCallback(@"^/devlog$", Index);
            Endpoint.TryAddEventCallback(DevLogIdRegex, OnDevLogPostRequested);
        }

        public HttpResponse? Index(HttpListenerRequest request) {
            // Get all posts
            IEnumerable<DevLogPostInfo> posts = DevLogPostsTable.Query(
                new WhereClause<bool>(DevLogPostsTable.Schema.IsPublished, "=", true)
            ).OrderByDescending(post => post.CreatedTimestamp.Value);

            DevLogHomePageModel model = new DevLogHomePageModel(this, posts);
            return model.Render();
        }

        static readonly Regex DevLogIdRegex = new Regex(@"^/devlog/(?<urlName>[^/]+)\/?$", RegexOptions.IgnoreCase);
        public HttpResponse? OnDevLogPostRequested(HttpListenerRequest request) {
            var match = DevLogIdRegex.Match(request.Url?.LocalPath ?? string.Empty);
            string urlName = match.Success ? match.Groups.TryGetValue(nameof(urlName), out var grp) ? grp.Value : string.Empty : string.Empty;

            DevLogPostInfo? post = DevLogPostsTable.Query(1,
                new WhereClause<string>(DevLogPostsTable.Schema.UrlName, "=", urlName)
            ).FirstOrDefault(defaultValue: null);

            if (post == null)
                return Endpoint.GetGenericStatusPage(HttpStatusCode.NotFound);
            else if (!post.IsPublished.Value) return Endpoint.GetGenericStatusPage(
                new StatusPageModel(HttpStatusCode.Forbidden,
                    subtitle: $"This devlog is currently not available"
                )
            );

            var model = new DevLogPageModel(this, post);
            return model.Render();
        }
    }
}
