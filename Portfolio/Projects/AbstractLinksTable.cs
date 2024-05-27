using System.Data;
using ExperimentalSQLite;

namespace Portfolio.Projects {
    public abstract class AbstractLinksTable<TTable, TRow> : PortfolioDatabase.SQLiteTable<TTable, TRow>
        where TTable : AbstractLinksTable<TTable, TRow>
        where TRow : AbstractLinksTable<TTable, TRow>.Row {
        protected AbstractLinksTable(PortfolioDatabase db, string tableName) : base(db, tableName) { }

        public class Row : SQLiteRow {
            public override IEnumerable<IDbCell> Fields => new IDbCell[] { LinkId, IconUnicodeOverride, LinkTextOverride };
            public override bool IsInDb => 0 < LinkId;
            public readonly DbPrimaryCell LinkId = new DbPrimaryCell(nameof(LinkId));
            public readonly DbCell<string> IconUnicodeOverride = new DbCell<string>(nameof(IconUnicodeOverride), DbType.String, null);
            public readonly DbCell<string> LinkTextOverride = new DbCell<string>(nameof(LinkTextOverride), DbType.String, null);
            public readonly DbCell<string> LinkRefrence = new DbCell<string>(nameof(LinkRefrence), DbType.String, constraints: DbCellFlags.NotNull);
            //public readonly DbCell<bool> IsLiveDemo = new DbCell<bool>(nameof(IsLiveDemo), DbType.Boolean, false, DbCellFlags.NotNull);
            public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

            public Row(TTable table) : base(table) { }
        }
    }
}