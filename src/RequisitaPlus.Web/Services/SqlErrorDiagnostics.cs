using Microsoft.Data.SqlClient;

namespace RequisitaPlus.Web.Services;

/// <summary>Never pass provider exception messages, Data, entities or credentials to a sink.</summary>
public static class SqlErrorDiagnostics
{
    public static int? Code(Exception error)
    {
        for (Exception? current = error; current != null; current = current.InnerException)
            if (current is SqlException sql) return sql.Number;
        return null;
    }

    public static void Log(ILogger logger, int code)
    {
        var category = code switch { 2601 or 2627 => "Unique", 547 or 515 or 2628 or 8152 => "Integrity", _ => "SqlFailure" };
        logger.LogError(new EventId(1001, "SqlFailure"), "Falha SQL: código {SqlCode}; categoria {SqlCategory}.", code, category);
    }
}
