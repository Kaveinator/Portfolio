using System.Data;
using ExperimentalSQLite;
using Portfolio.Technologies.Data;
using WebServer.Models;
using Markdig;

namespace Portfolio.Projects.Data {
    public class TechnologiesUsedTable : PortfolioDatabase.SQLiteTable<TechnologiesUsedTable, TechnologyUsedInfo> {
        public TechnologiesUsedTable(PortfolioDatabase database) : base(database, nameof(TechnologyUsedInfo)) { }

        public override TechnologyUsedInfo ConstructRow() => new TechnologyUsedInfo(this);

        public IEnumerable<TechnologyUsedInfo> GetTechUsedFromProject(ProjectInfo projectInfo) {
            if (projectInfo == null || !projectInfo.IsInDb)
                return Enumerable.Empty<TechnologyUsedInfo>();
            return Query(
                new WhereClause<long>(Schema.ProjectId, "=", projectInfo.ProjectId.Value)
            );
        }
    }
    public class TechnologyUsedInfo : TechnologiesUsedTable.SQLiteRow, IDataModel {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { EntryId, ProjectId, TechId, BadgeTextOverride, DetailsMarkdown };
        public Dictionary<string, object> Values => Fields.ToDictionary(field => field.ColumnName, field => field.Value)
            .Update(DetailsMarkdown.ColumnName, _ => Markdown.ToHtml(DetailsMarkdown.Value, PortfolioEndpoint.MarkdownPipeline));
        public readonly DbPrimaryCell EntryId = new DbPrimaryCell(nameof(EntryId));
        public override bool IsInDb => EntryId.Value > 0;
        public readonly DbForeignCell<long> ProjectId;
        public readonly DbForeignCell<long> TechId;
        public readonly DbCell<string?> BadgeTextOverride = new DbCell<string?>(nameof(BadgeTextOverride), DbType.String, null);
        public readonly DbCell<string> DetailsMarkdown = new DbCell<string>(nameof(DetailsMarkdown), DbType.String, constraints: DbCellFlags.NotNull);

        public TechnologyUsedInfo(TechnologiesUsedTable table) : base(table) {
            ProjectsTable projectsTable = table.Database.ProjectsTable;
            ProjectId = new DbForeignCell<long>(nameof(ProjectId), projectsTable, projectsTable.Schema.ProjectId);
            TechnologiesTable techtable = table.Database.TechnologiesTable;
            TechId = new DbForeignCell<long>(nameof(TechId), techtable, techtable.Schema.TechId);
        }
    }
}