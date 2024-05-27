using System.Data;
using ExperimentalSQLite;

namespace Portfolio.Projects {
    public class OrganizationTable : PortfolioDatabase.SQLiteTable<OrganizationTable, OrginizationInfo> {
        public OrganizationTable(PortfolioDatabase database) : base(database, nameof(OrginizationInfo)) { }

        public override OrginizationInfo ConstructRow() => new OrginizationInfo(this);
    }
    public class OrginizationInfo : OrganizationTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { OrginizationId, UrlName, Name, BriefText, OverviewSubheader, OverviewMarkdown, IsPublished };
        public readonly DbPrimaryCell OrginizationId = new DbPrimaryCell(nameof(OrginizationId));
        public override bool IsInDb => OrginizationId.Value > 0;
        public readonly DbCell<string> UrlName = new DbCell<string>(nameof(UrlName), DbType.String, constraints: DbCellFlags.UniqueKey | DbCellFlags.NotNull);
        public readonly DbCell<string> Name = new DbCell<string>(nameof(Name), DbType.String, constraints: DbCellFlags.NotNull);
        //public readonly DbCell<string> FullName = new DbCell<string>(nameof(FullName), DbType.String, constraints: DbCellFlags.UniqueKey);
        public readonly DbCell<string> BriefText = new DbCell<string>(nameof(BriefText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> OverviewSubheader = new DbCell<string>(nameof(OverviewSubheader), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> OverviewMarkdown = new DbCell<string>(nameof(OverviewMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public OrginizationInfo(OrganizationTable table) : base(table) {}
    }
}