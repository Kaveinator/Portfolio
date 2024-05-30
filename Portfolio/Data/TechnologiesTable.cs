using System.Data;
using ExperimentalSQLite;

namespace Portfolio.Projects {
    public class TechnologiesTable : PortfolioDatabase.SQLiteTable<TechnologiesTable, TechnologyInfo> {
        public TechnologiesTable(PortfolioDatabase database) : base(database, nameof(TechnologyInfo)) { }

        public override TechnologyInfo ConstructRow() => new TechnologyInfo(this);
    }
    public class TechnologyInfo : TechnologiesTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { TechId, EnumName, DefaultBadgeText, TitleMarkdown, ContentMarkdown };
        public readonly DbPrimaryCell TechId = new DbPrimaryCell(nameof(TechId));
        public override bool IsInDb => TechId.Value > 0;
        public readonly DbCell<string> EnumName = new DbCell<string>(nameof(EnumName), DbType.String, constraints: DbCellFlags.NotNull | DbCellFlags.UniqueKey);
        public readonly DbCell<string> DefaultBadgeText = new DbCell<string>(nameof(DefaultBadgeText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> TitleMarkdown = new DbCell<string>(nameof(TitleMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> ContentMarkdown = new DbCell<string>(nameof(ContentMarkdown), DbType.String, constraints: DbCellFlags.NotNull);

        public TechnologyInfo(TechnologiesTable table) : base(table) { }
    }
}