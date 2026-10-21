using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RequisitaPlus.Web.Data;
using Xunit;

namespace RequisitaPlus.Tests.Integration.SqlServer;

public static class SqlSchemaAssertions
{
    public static Task VerifyAsync(ApplicationDbContext db) => SqlSafety.ProtectAsync(async () =>
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        foreach (var entity in db.GetService<IDesignTimeModel>().Model.GetEntityTypes())
        {
            var table = "dbo." + entity.GetTableName();
            var store = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            var properties = entity.GetProperties().ToArray();
            var columns = await Rows(connection, "SELECT c.name,t.name,c.max_length,c.is_nullable,c.precision,c.scale,c.is_identity,c.is_computed,c.generated_always_type FROM sys.columns c JOIN sys.types t ON c.user_type_id=t.user_type_id WHERE c.object_id=OBJECT_ID(@table) ORDER BY c.column_id", table);
            Assert.Equal(properties.Length, columns.Count);
            foreach (var property in properties)
            {
                var name = property.GetColumnName(store)!;
                var column = Assert.Single(columns, c => Equals(c[0], name));
                var type = property.GetColumnType()!;
                var baseType = type.Split('(')[0];
                Assert.Equal(baseType == "rowversion" ? "timestamp" : baseType, column[1]);
                Assert.Equal(property.IsNullable, column[3]);
                if (baseType == "nvarchar") Assert.Equal(type == "nvarchar(max)" ? -1 : property.GetMaxLength()!.Value * 2, Convert.ToInt32(column[2]));
                if (baseType == "decimal") { Assert.Equal(property.GetPrecision(), Convert.ToInt32(column[4])); Assert.Equal(property.GetScale(), Convert.ToInt32(column[5])); }
                if (baseType is "datetime2" or "datetimeoffset") Assert.Equal(7, Convert.ToInt32(column[5]));
                if (baseType == "rowversion") { Assert.Equal(8, Convert.ToInt32(column[2])); Assert.True(property.IsConcurrencyToken); }
                Assert.Equal(property.ClrType == typeof(int) && property.ValueGenerated == ValueGenerated.OnAdd, column[6]);
                Assert.False((bool)column[7]); Assert.Equal(0, Convert.ToInt32(column[8]));
            }
            var pk = entity.FindPrimaryKey()!;
            await VerifyIndex(connection, table, pk.GetName()!, pk.Properties.Select(x => x.GetColumnName(store)!).ToArray(), true, null, true, null);
            foreach (var index in entity.GetIndexes())
                await VerifyIndex(connection, table, index.GetDatabaseName()!, index.Properties.Select(x => x.GetColumnName(store)!).ToArray(), index.IsUnique, index.IsDescending, false, index.GetFilter());
            Assert.Equal(entity.GetIndexes().Count() + 1, (await Rows(connection, "SELECT name FROM sys.indexes WHERE object_id=OBJECT_ID(@table) AND index_id>0 AND is_hypothetical=0", table)).Count);

            var checks = await Rows(connection, "SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(@table)", table);
            Assert.Equal(entity.GetCheckConstraints().Count(), checks.Count);
            foreach (var check in entity.GetCheckConstraints())
            {
                var row = Assert.Single(checks, c => Equals(c[0], check.Name));
                Assert.False((bool)row[2]); Assert.False((bool)row[3]);
                Assert.True(CheckExpression.Canonical(check.Sql) == CheckExpression.Canonical((string)row[1]), $"CHECK definition mismatch: {check.Name}");
            }
            var fks = await Rows(connection, "SELECT f.name,OBJECT_SCHEMA_NAME(f.referenced_object_id),OBJECT_NAME(f.referenced_object_id),pc.name,rc.name,fc.constraint_column_id,f.delete_referential_action,f.update_referential_action,f.is_disabled,f.is_not_trusted FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id JOIN sys.columns pc ON pc.object_id=f.parent_object_id AND pc.column_id=fc.parent_column_id JOIN sys.columns rc ON rc.object_id=f.referenced_object_id AND rc.column_id=fc.referenced_column_id WHERE f.parent_object_id=OBJECT_ID(@table) ORDER BY f.name,fc.constraint_column_id", table);
            Assert.Equal(entity.GetForeignKeys().Count(), fks.Select(x => x[0]).Distinct().Count());
            foreach (var fk in entity.GetForeignKeys())
            {
                var name = fk.GetConstraintName()!;
                var rows = fks.Where(x => Equals(x[0], name)).ToArray();
                Assert.Equal(fk.Properties.Count, rows.Length);
                var principal = StoreObjectIdentifier.Table(fk.PrincipalEntityType.GetTableName()!, fk.PrincipalEntityType.GetSchema());
                for (var i = 0; i < rows.Length; i++)
                {
                    Assert.Equal("dbo", rows[i][1]); Assert.Equal(principal.Name, rows[i][2]);
                    Assert.Equal(fk.Properties[i].GetColumnName(store), rows[i][3]);
                    Assert.Equal(fk.PrincipalKey.Properties[i].GetColumnName(principal), rows[i][4]);
                    Assert.Equal(i + 1, Convert.ToInt32(rows[i][5]));
                    Assert.Equal(fk.DeleteBehavior == DeleteBehavior.Cascade ? 1 : 0, Convert.ToInt32(rows[i][6]));
                    Assert.Equal(0, Convert.ToInt32(rows[i][7]));
                    Assert.False((bool)rows[i][8]); Assert.False((bool)rows[i][9]);
                }
            }
        }
    }, SqlStage.CatalogQuery);

    private static async Task VerifyIndex(SqlConnection connection, string table, string name, string[] columns, bool unique,
        IReadOnlyList<bool>? descending, bool primary, string? filter)
    {
        var rows = await Rows(connection, "SELECT c.name,ic.key_ordinal,ic.is_descending_key,ic.is_included_column,i.is_unique,i.is_primary_key,i.is_disabled,i.ignore_dup_key,i.filter_definition FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID(@table) AND i.name=@name ORDER BY ic.key_ordinal", table, name);
        Assert.Equal(columns.Length, rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            Assert.Equal(columns[i], rows[i][0]); Assert.Equal(i + 1, Convert.ToInt32(rows[i][1]));
            Assert.Equal(descending != null && (descending.Count == 0 || descending[i]), rows[i][2]);
            Assert.False((bool)rows[i][3]); Assert.Equal(unique, rows[i][4]); Assert.Equal(primary, rows[i][5]);
            Assert.False((bool)rows[i][6]); Assert.False((bool)rows[i][7]);
            var actualFilter = rows[i][8] is DBNull ? null : (string)rows[i][8];
            Assert.True(filter == null ? actualFilter == null : actualFilter != null && CheckExpression.Canonical(filter) == CheckExpression.Canonical(actualFilter), $"Index filter mismatch: {name}");
        }
    }

    private static async Task<List<object[]>> Rows(SqlConnection connection, string sql, string table, string name = "")
    {
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@table", table); command.Parameters.AddWithValue("@name", name);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<object[]>();
        while (await reader.ReadAsync()) { var row = new object[reader.FieldCount]; reader.GetValues(row); result.Add(row); }
        return result;
    }
}
