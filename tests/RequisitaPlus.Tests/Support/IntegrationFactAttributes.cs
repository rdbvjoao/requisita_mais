using System.Runtime.CompilerServices;
using Xunit;

namespace RequisitaPlus.Tests.Support;

// Future real integrations must use these attributes plus the appropriate
// [Trait("Category", "SqlServer")] or [Trait("Category", "Ollama")].
// Runtime guards in IntegrationGate remain mandatory before any fixture I/O.
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute([CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
    {
        if (!IntegrationGate.IsSqlTestEnabled())
            Skip = "SQL integration requires explicit opt-in and a disposable test database.";
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute([CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
    {
        if (!IntegrationGate.IsSqlTestEnabled())
            Skip = "SQL integration requires explicit opt-in and a disposable test database.";
    }
}

public sealed class OllamaFactAttribute : FactAttribute
{
    public OllamaFactAttribute([CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
    {
        if (!IntegrationGate.IsOllamaEnabled())
            Skip = "Ollama integration requires explicit opt-in and separate human authorization.";
    }
}

public sealed class OllamaTheoryAttribute : TheoryAttribute
{
    public OllamaTheoryAttribute([CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
    {
        if (!IntegrationGate.IsOllamaEnabled())
            Skip = "Ollama integration requires explicit opt-in and separate human authorization.";
    }
}
