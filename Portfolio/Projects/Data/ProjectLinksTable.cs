using System.Data;
using ExperimentalSQLite;

namespace Portfolio.Projects.Data {
    public class ProjectLinksTable : AbstractLinksTable<ProjectLinksTable, ProjectLinkInfo> {
        public ProjectLinksTable(PortfolioDatabase db) : base(db, nameof(ProjectLinkInfo)) { }

        public override ProjectLinkInfo ConstructRow() => new ProjectLinkInfo(this);

        /// <summary>Returns a list of Links for the project</summary>
        /// <param name="project">The project info refrence</param>
        /// <param name="includePrivate">Should the result include unpublished links?</param>
        /// <returns>A enumerable of link info for the project</returns>
        public IEnumerable<ProjectLinkInfo> GetLinksForProject(ProjectInfo project, bool includePrivate = false) {
            if (project == null || !project.IsInDb)
                return Enumerable.Empty<ProjectLinkInfo>();

            var projectQuery = new WhereClause<long>(Schema.ProjectId, "=", project.ProjectId);
            return !includePrivate ? Query(projectQuery)
                : Query(projectQuery, new WhereClause<bool>(Schema.IsPublished, "=", true));
        }

        /// <summary>Returns true if any published links are labeled as LiveDemo links</summary>
        /// <param name="project">The project refrence</param>
        /// <returns>True if any published links are labeled as a LiveDemo link</returns>
        public bool HasLiveProjectLink(ProjectInfo project) {
            if (!(project?.IsInDb ?? false)) return false;
            return Query(1,
                new WhereClause<long>(Schema.ProjectId, "=", project.ProjectId),
                new WhereClause<bool>(Schema.IsPublished, "=", true),
                new WhereClause<bool>(Schema.IsLiveDemo, "=", true)
            ).Any();
        }
    }
    public class ProjectLinkInfo : ProjectLinksTable.Row {
        public override IEnumerable<IDbCell> Fields => base.Fields.Include(ProjectId, IsLiveDemo);

        public readonly DbForeignCell<long> ProjectId;
        public readonly DbCell<bool> IsLiveDemo = new DbCell<bool>(nameof(IsLiveDemo), DbType.Boolean, false, DbCellFlags.NotNull);

        public ProjectLinkInfo(ProjectLinksTable table) : base(table) {
            ProjectsTable projects = table.Database.ProjectsTable;
            ProjectId = new DbForeignCell<long>(nameof(ProjectId), projects, projects.Schema.ProjectId, constraints: DbCellFlags.NotNull);
        }
    }
}