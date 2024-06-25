using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Http;
using WebServer.Models;
using Portfolio.DevLog.Data;
using ExperimentalSQLite;

namespace Portfolio.DevLog.Models {
    internal class DevLogEntryModel : IPageComponentModel {
        public Dictionary<string, object> Values => new Dictionary<string, object>(Post.Values) {
            { nameof(Tags), Tags }
        };
        public readonly PortfolioEndpoint Resources;
        public readonly DevLogPostInfo Post;
        public readonly IEnumerable<DevLogTagInfo> Tags;

        public DevLogEntryModel(PortfolioEndpoint endpoint, DevLogPostInfo post, IEnumerable<DevLogTagInfo> tags) {
            Resources = endpoint;
            Post = post;
            Tags = tags;
        }

        /// <summary>Creates a model for a DevLog entry, this constructor will automatically query for the tags</summary>
        /// <param name="endpoint">Used to get the rendering template</param>
        /// <param name="post">The post info</param>
        public DevLogEntryModel(PortfolioEndpoint endpoint, DevLogPostInfo post) {
            Resources = endpoint;
            Post = post;
            // Get tags
            DevLogTagBindingsTable bindingsTable = endpoint.Database.DevLogTagBindingsTable;
            var bindings = bindingsTable.Query(
                new WhereClause<long>(bindingsTable.Schema.PostId, "=", post.PostId.Value)
            );
            DevLogTagsTable tagsTable = endpoint.Database.DevLogTagsTable;
            Tags = tagsTable.GetTagsFromBindingsSet(bindings);
        }

        public string Render() => Resources.GetTemplate("/DevLog/DevLogEntry.html", this);
    }
}
