using System.Data;
using ExperimentalSQLite;
using WebServer.Models;

namespace Portfolio.Projects {
    public class OrganizationTable : PortfolioDatabase.SQLiteTable<OrganizationTable, OrganizationInfo> {
        public OrganizationTable(PortfolioDatabase database) : base(database, nameof(OrganizationInfo)) { }

        public override OrganizationInfo ConstructRow() => new OrganizationInfo(this);
    }
    public class OrganizationInfo : OrganizationTable.SQLiteRow, IPageModel {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { OrginizationId, UrlName, Name, Role, BriefText, OverviewSubheaderOverride, OverviewMarkdown, IsPublished };
        public Dictionary<string, object> Values => Fields.Omit(OrginizationId, IsPublished).ToDictionary(cell => cell.ColumnName, cell => cell.Value);
        public readonly DbPrimaryCell OrginizationId = new DbPrimaryCell(nameof(OrginizationId));
        public override bool IsInDb => OrginizationId.Value > 0;
        public readonly DbStringCell UrlName = new DbStringCell(nameof(UrlName), constraints: DbCellFlags.UniqueKey | DbCellFlags.NotNull, collation: StringCollation.NOCASE);
        public readonly DbCell<string> Name = new DbCell<string>(nameof(Name), DbType.String, constraints: DbCellFlags.NotNull);
        //public readonly DbCell<string> FullName = new DbCell<string>(nameof(FullName), DbType.String, constraints: DbCellFlags.UniqueKey);
        public readonly DbCell<string> Role = new DbCell<string>(nameof(Role), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> BriefText = new DbCell<string>(nameof(BriefText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string?> OverviewSubheaderOverride = new DbCell<string?>(nameof(OverviewSubheaderOverride), DbType.String);
        public readonly DbCell<string> OverviewMarkdown = new DbCell<string>(nameof(OverviewMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public OrganizationInfo(OrganizationTable table) : base(table) { }
    }
}