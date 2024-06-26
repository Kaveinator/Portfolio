﻿
using ExperimentalSQLite;
using Portfolio.Projects.Data;
using System.Data.SQLite;
using System.Linq;

namespace Portfolio.DevLog.Data
{
    public class DevLogProjectBindingsTable : PortfolioDatabase.SQLiteTable<DevLogProjectBindingsTable, DevLogProjectBindingInfo> {
        internal readonly DevLogPostsTable PostsTableRefrence;
        internal readonly ProjectsTable ProjectsTableRefrence;

        public DevLogProjectBindingsTable(PortfolioDatabase database, DevLogPostsTable postsRefrence, ProjectsTable projectsTable)
            : base(database, "DevLogProjectBindings") {
            PostsTableRefrence = postsRefrence;
            ProjectsTableRefrence = projectsTable;
        }

        public override DevLogProjectBindingInfo ConstructRow() => new DevLogProjectBindingInfo(this);

        public IEnumerable<ProjectInfo> GetProjects(DevLogPostInfo post, bool includePrivate = false) {
            if (post == null)
                return Enumerable.Empty<ProjectInfo>();
            IEnumerable<DevLogProjectBindingInfo> bindingInfo = Query(
                new WhereClause<long>(Schema.PostId, '=', post.PostId)
            );
            if (!bindingInfo.Any()) return Enumerable.Empty<ProjectInfo>();

            ProjectsTable projects = Database.ProjectsTable;
            IEnumerable<WhereClause> filters = bindingInfo.DistinctBy(bind => bind.ProjectId.Value)
                .Select(bindInfo => new WhereClause<long>(projects.Schema.ProjectId, '=', bindInfo.ProjectId)
            );
            if (!includePrivate)
                filters = filters.Append(new WhereClause<bool>(projects.Schema.IsPublished, '=', true));

            return projects.Query(filters.ToArray());
        }

        public IEnumerable<DevLogPostInfo> GetPosts(ProjectInfo project, bool includePrivate = false) {
            if (project == null)
                return Enumerable.Empty<DevLogPostInfo>();

            IEnumerable<DevLogProjectBindingInfo> bindingInfo = Query(
                new WhereClause<long>(Schema.ProjectId, '=', project.ProjectId)
            );
            if (!bindingInfo.Any()) return Enumerable.Empty<DevLogPostInfo>();

            DevLogPostsTable posts = Database.DevLogPostsTable;
            byte index = 0;
            List<WhereClause> filters = bindingInfo.DistinctBy(bind => bind.PostId.Value)
                .Select(bindInfo => new WhereClause<long>(posts.Schema.PostId, '=', bindInfo.PostId, ++index) as WhereClause
            ).ToList();

            string whereClause = '(' + string.Join(" OR ", filters.Select(clause => clause.ToString())) + ')';
            if (!includePrivate) {
                var publishedFilter = new WhereClause<bool>(posts.Schema.IsPublished, '=', true, ++index);
                whereClause += $" AND {publishedFilter}";
                filters.Add(publishedFilter);
            }

            using (SQLiteCommand cmd = new($"SELECT * FROM `{posts.TableName}` WHERE {whereClause};", Database.Connection)) {
                filters.ForEach(filter => cmd.Parameters.Add(filter.ToParameter()));
                return posts.ReadFromReader(cmd.ExecuteReader());
            }
        }
    }
    public class DevLogProjectBindingInfo : DevLogProjectBindingsTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { BindingId, PostId, ProjectId };
        public override bool IsInDb => BindingId.Value > 0;
        public readonly DbPrimaryCell BindingId = new DbPrimaryCell(nameof(BindingId));
        public readonly DbForeignCell<long> PostId;
        public readonly DbForeignCell<long> ProjectId;

        public DevLogProjectBindingInfo(DevLogProjectBindingsTable table) : base(table) {
            PostId = new DbForeignCell<long>(nameof(PostId), table.PostsTableRefrence, table.PostsTableRefrence.Schema.PostId, constraints: DbCellFlags.NotNull);
            ProjectId = new DbForeignCell<long>(nameof(ProjectId), table.ProjectsTableRefrence, table.ProjectsTableRefrence.Schema.ProjectId, constraints: DbCellFlags.NotNull);
        }
    }
}
