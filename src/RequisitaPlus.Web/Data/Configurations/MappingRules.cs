using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RequisitaPlus.Web.Data.Configurations;

internal static class MappingRules
{
    // SQL datetime2 does not preserve DateTime.Kind. Reject non-UTC writes and
    // restore Kind on reads without changing the recorded clock value.
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => RequireUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static PropertyBuilder<T> Utc<T>(this PropertyBuilder<T> property) =>
        property.HasColumnType("datetime2").HasConversion(UtcConverter);

    private static DateTime RequireUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value : throw new InvalidOperationException("Instantes persistidos devem representar UTC.");

    // SQL equality ignores trailing spaces even with BIN2: compare byte lengths too.
    public static string ExactValues(string column, params string[] values) =>
        "(" + string.Join(" OR ", values.Select(value =>
            $"([{column}] COLLATE Latin1_General_100_BIN2 = N'{value}' AND DATALENGTH([{column}]) = {value.Length * 2})")) + ")";

    public static string NonBlank(string column)
    {
        // The same Unicode whitespace set used by .NET's required-text validation.
        var expression = $"[{column}] COLLATE Latin1_General_100_BIN2";
        foreach (var character in "\u0009\u000a\u000b\u000c\u000d\u0020\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000")
            expression = $"REPLACE({expression}, NCHAR({(int)character}), N'')";
        return $"DATALENGTH({expression}) > 0";
    }

    // DATALENGTH counts trailing spaces and surrogate pairs as UTF-16 code units.
    public static string Text(string column, int maximum) =>
        $"DATALENGTH([{column}]) <= {maximum * 2} AND {NonBlank(column)}";
}
