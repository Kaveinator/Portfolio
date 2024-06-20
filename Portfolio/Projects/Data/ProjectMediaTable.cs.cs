using System.Data;
using ExperimentalSQLite;
using Markdig;
using WebServer.Models;

namespace Portfolio.Projects.Data {
    public class ProjectMediaTable : PortfolioDatabase.SQLiteTable<ProjectMediaTable, ProjectMediaInfo> {
        public ProjectMediaTable(PortfolioDatabase database) : base(database, nameof(ProjectMediaInfo)) { }

        public override ProjectMediaInfo ConstructRow() => new ProjectMediaInfo(this);
    }
    public class ProjectMediaInfo : ProjectMediaTable.SQLiteRow, IPageModel {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { EntryId, ProjectId, CaptionMarkdown, ImageSource, IsPublished };
        Dictionary<string, object> IPageModel.Values => Fields.Omit(EntryId, ProjectId, IsPublished).ToDictionary(field => field.ColumnName, field => field.Value)
            .Update(nameof(CaptionMarkdown), _ => Markdown.ToHtml(CaptionMarkdown.Value, PortfolioEndpoint.MarkdownPipeline));
        public readonly DbPrimaryCell EntryId = new DbPrimaryCell(nameof(EntryId));
        public override bool IsInDb => EntryId.Value > 0;
        public readonly DbForeignCell<long> ProjectId;
        public readonly DbCell<string> ImageSource = new DbCell<string>(nameof(ImageSource), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> CaptionMarkdown = new DbCell<string>(nameof(CaptionMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public ProjectMediaInfo(ProjectMediaTable table) : base(table) {
            ProjectsTable projectsTable = table.Database.ProjectsTable;
            ProjectId = new DbForeignCell<long>(nameof(ProjectId), projectsTable, projectsTable.Schema.ProjectId, constraints: DbCellFlags.NotNull);
        }
    }
}