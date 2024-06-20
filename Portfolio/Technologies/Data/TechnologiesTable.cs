using System.Data;
using System.Data.SQLite;
using System.Linq;
using ExperimentalSQLite;
using Portfolio.Projects.Data;

namespace Portfolio.Technologies.Data {
    public class TechnologiesTable : PortfolioDatabase.SQLiteTable<TechnologiesTable, TechnologyInfo> {
        public TechnologiesTable(PortfolioDatabase database) : base(database, nameof(TechnologyInfo)) { }

        public override TechnologyInfo ConstructRow() => new TechnologyInfo(this);

        public IEnumerable<TechnologyInfo> GetTechInfoSetFromTechUsedSet(IEnumerable<TechnologyUsedInfo> techUsed) {
            // Since the values are all ints, I won't use SQL parameters here
            if (!techUsed.Any()) return Array.Empty<TechnologyInfo>();
            string whereClause = string.Join(" OR ", techUsed.Select(techItem => $"`{Schema.TechId.ColumnName}` = {techItem.TechId.Value}"));
            using (SQLiteCommand cmd = new($"SELECT * FROM `{TableName}` WHERE {whereClause};", Database.Connection)) {
                return ReadFromReader(cmd.ExecuteReader());
            }
        }
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