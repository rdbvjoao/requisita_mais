using RequisitaPlus.Tests.Support;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class IntegrationGateTests
{
    private static string? Missing(string _) => null;

    [Fact]
    public void MissingConfigurationDisablesBothIntegrationsAndRejectsSqlAccess()
    {
        Assert.False(IntegrationGate.IsSqlEnabled(Missing));
        Assert.False(IntegrationGate.IsOllamaEnabled(Missing));
        Assert.Throws<InvalidOperationException>(() => IntegrationGate.RequireSqlConnectionString(Missing));
    }

    [Theory]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("1")]
    [InlineData("yes")]
    [InlineData("TRUE")]
    [InlineData(" true")]
    [InlineData("true ")]
    [InlineData("true,false")]
    public void AmbiguousOptInCannotEnableEitherService(string value)
    {
        var settings = ValidSqlSettings();
        settings[IntegrationGate.SqlOptIn] = value;
        settings[IntegrationGate.OllamaOptIn] = value;
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
        Assert.False(IntegrationGate.IsOllamaEnabled(settings.GetValueOrDefault));
    }

    [Fact]
    public void OptInsAreIndependentAndSqlStillRequiresItsOwnConfiguration()
    {
        var settings = new Dictionary<string, string?> { [IntegrationGate.OllamaOptIn] = "true" };
        Assert.True(IntegrationGate.IsOllamaEnabled(settings.GetValueOrDefault));
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));

        settings = ValidSqlSettings();
        Assert.True(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
        Assert.False(IntegrationGate.IsOllamaEnabled(settings.GetValueOrDefault));
    }

    [Theory]
    [InlineData(IntegrationGate.SqlConnection)]
    [InlineData(IntegrationGate.DisposableDatabase)]
    [InlineData(IntegrationGate.MainDatabase)]
    public void EachSqlSettingIsMandatoryWithoutFallback(string missingKey)
    {
        var settings = ValidSqlSettings();
        settings.Remove(missingKey);
        settings["ConnectionStrings__DefaultConnection"] = "Server=unusable;Database=Principal";
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
        Assert.Throws<InvalidOperationException>(() => IntegrationGate.RequireSqlConnectionString(settings.GetValueOrDefault));
    }

    [Theory]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=RequisitaPlus_Test_Gate")]
    [InlineData("Server=unusable;Database=Principal")]
    [InlineData("Server=unusable;Database=master")]
    [InlineData("Server=unusable")]
    [InlineData("Database=RequisitaPlus_Test_Gate")]
    [InlineData("not a connection string")]
    [InlineData("Server=unusable;Database=RequisitaPlus_Test_Gate;AttachDbFilename=example.mdf")]
    [InlineData("Server=unusable;Database=RequisitaPlus_Test_Gate;User Instance=true")]
    public void UnsafeOrIncompleteSqlTargetsAreRejectedWithoutConnecting(string connection)
    {
        var settings = ValidSqlSettings();
        settings[IntegrationGate.SqlConnection] = connection;
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
    }

    [Fact]
    public void TestDatabaseCannotEqualMainDatabaseEvenWithDifferentCasing()
    {
        var settings = ValidSqlSettings();
        settings[IntegrationGate.MainDatabase] = "requisitaplus_test_gate";
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
    }

    [Theory]
    [InlineData("Principal")]
    [InlineData("master")]
    [InlineData("RequisitaPlus_Test_")]
    [InlineData("RequisitaPlus_Test_Gate ")]
    public void DisposableDatabaseMustHaveAnUnambiguousTestName(string database)
    {
        var settings = ValidSqlSettings();
        settings[IntegrationGate.DisposableDatabase] = database;
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
    }

    [Fact]
    public void ValidExplicitSettingsReturnOnlyTheDedicatedTestConnection()
    {
        var settings = ValidSqlSettings();
        var canonical = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(IntegrationGate.RequireSqlConnectionString(settings.GetValueOrDefault));
        Assert.Equal("lpc:" + IntegrationGate.LocalInstance, canonical.DataSource);
        Assert.Equal(settings[IntegrationGate.DisposableDatabase], canonical.InitialCatalog);
        Assert.Equal(Environment.MachineName, canonical.HostNameInCertificate);
        Assert.Equal(Microsoft.Data.SqlClient.SqlConnectionEncryptOption.Mandatory, canonical.Encrypt);
        Assert.False(canonical.TrustServerCertificate);
        Assert.True(canonical.IntegratedSecurity);
        Assert.False(canonical.Pooling);
        Assert.Equal(0, canonical.ConnectRetryCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitLocalCertificateNameIsAcceptedAndCanonicalized(bool lowerCase)
    {
        var settings = ValidSqlSettings();
        var name = lowerCase ? Environment.MachineName.ToLowerInvariant() : Environment.MachineName;
        settings[IntegrationGate.SqlConnection] += $";HostNameInCertificate={name}";
        var canonical = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(IntegrationGate.RequireSqlConnectionString(settings.GetValueOrDefault));
        Assert.Equal(Environment.MachineName, canonical.HostNameInCertificate);
        Assert.Equal("lpc:" + IntegrationGate.LocalInstance, canonical.DataSource);
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost")]
    [InlineData("remote.invalid")]
    [InlineData("*")]
    [InlineData("prefix")]
    [InlineData("suffix")]
    [InlineData("fqdn")]
    public void CertificateNameMustMatchMachineExactlyExceptForCase(string value)
    {
        var settings = ValidSqlSettings();
        var name = value switch
        {
            "prefix" => " " + Environment.MachineName,
            "suffix" => Environment.MachineName + " ",
            "fqdn" => Environment.MachineName + ".invalid",
            _ => value
        };
        settings[IntegrationGate.SqlConnection] += $";HostNameInCertificate=\"{name}\"";
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
        Assert.Throws<InvalidOperationException>(() => IntegrationGate.RequireSqlConnectionString(settings.GetValueOrDefault));
    }

    private static Dictionary<string, string?> ValidSqlSettings() => new()
    {
        [IntegrationGate.SqlOptIn] = "true",
        [IntegrationGate.SqlConnection] = @"Server=.\REQUISITAPLUS;Database=RequisitaPlus_Test_Gate;Integrated Security=true",
        [IntegrationGate.DisposableDatabase] = "RequisitaPlus_Test_Gate",
        [IntegrationGate.MainDatabase] = "RequisitaPlus_Principal",
        [IntegrationGate.SqlOperation] = "test",
        [IntegrationGate.ExpectedMachine] = Environment.MachineName
    };
}
