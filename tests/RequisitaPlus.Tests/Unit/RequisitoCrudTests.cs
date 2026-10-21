using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Services;
using RequisitaPlus.Web.Services.Requisitos;
using RequisitaPlus.Web.ViewModels.Requisitos;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class RequisitoCrudTests
{
    private static readonly RelogioLocal Clock = new(new ControlledClock(new DateTimeOffset(TestData.Instant.AddDays(1))));
    private static AcessoProprietarioService Access(SimulatedRequisitoContext db, int owner = 1) => new(db,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = TestData.Principal(owner) } });

    private static EditarRequisitoViewModel Edit(SimulatedRequisitoStore store) => new()
    {
        Titulo = store.Rows[0].Titulo, Descricao = store.Rows[0].Descricao, Prioridade = store.Rows[0].Prioridade,
        VersaoEsperada = store.Rows[0].VersaoTextual, RowVersion = Convert.ToBase64String(store.Rows[0].RowVersion)
    };

    [Theory]
    [InlineData("Alta")]
    [InlineData("Média")]
    [InlineData("Baixa")]
    public async Task CreationPreservesUnicodeTrailingWhitespaceAndExactLimits(string priority)
    {
        var store = new SimulatedRequisitoStore(); using var db = store.Context();
        var title = "😀" + new string('x', 147) + " ";
        var description = "ç\n" + new string('x', 4997) + " ";
        await new CadastroRequisitoService(db, Access(db, 2), Clock).CriarAsync(new()
        { Titulo = title, Descricao = description, Prioridade = priority }, TestContext.Current.CancellationToken);
        var row = Assert.Single(store.Rows);
        Assert.Equal(title, row.Titulo); Assert.Equal(description, row.Descricao);
        Assert.Equal(priority, row.Prioridade); Assert.Equal(2, row.IdUsuario);
        Assert.Equal("Rascunho", row.Status); Assert.Equal(1, row.VersaoTextual);
        Assert.Equal(0, row.UltimaOrdemAnalise); Assert.Equal(Clock.AgoraUtc, row.DataCadastroUtc);
        Assert.Null(row.DataAtualizacaoUtc);
    }

    [Theory]
    [InlineData("identical")]
    [InlineData("priority")]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("both")]
    [InlineData("trailing-space")]
    public async Task EditingUsesExactTextComparisonAndPreservesCounters(string change)
    {
        var store = new SimulatedRequisitoStore(); store.Seed(status: "Revisado");
        store.Rows[0].UltimaOrdemAnalise = 7;
        var input = Edit(store);
        if (change is "title" or "both") input.Titulo = "Outro título";
        if (change is "description" or "both") input.Descricao = "Outra descrição";
        if (change == "trailing-space") input.Descricao += " ";
        if (change == "priority") input.Prioridade = "Baixa";
        using var db = store.Context();
        var result = await new EdicaoRequisitoService(db, Access(db), Clock).EditarAsync(1, input, TestContext.Current.CancellationToken);
        Assert.Equal(ResultadoMutacaoRequisito.Concluido, result);
        var textChanged = change is not ("identical" or "priority");
        var row = Assert.Single(store.Rows);
        Assert.Equal(textChanged ? 2 : 1, row.VersaoTextual);
        Assert.Equal(textChanged ? "Rascunho" : "Revisado", row.Status);
        Assert.Equal(7, row.UltimaOrdemAnalise); Assert.Equal(TestData.Instant, row.DataCadastroUtc);
        Assert.Equal(change == "identical" ? null : Clock.AgoraUtc, row.DataAtualizacaoUtc);
        Assert.All(db.ChangeTracker.Entries(), x => Assert.IsType<RequisitaPlus.Web.Models.Requisito>(x.Entity));
    }

    [Fact]
    public async Task ReturningToOriginalTextCreatesThirdVersion()
    {
        var store = new SimulatedRequisitoStore(); store.Seed();
        var original = store.Rows[0].Titulo;
        foreach (var title in new[] { "Texto B", original })
        {
            var input = Edit(store); input.Titulo = title;
            using var db = store.Context();
            Assert.Equal(ResultadoMutacaoRequisito.Concluido, await new EdicaoRequisitoService(db, Access(db), Clock).EditarAsync(1, input, TestContext.Current.CancellationToken));
        }
        Assert.Equal(3, store.Rows[0].VersaoTextual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitRaceIsReportedAsConflictWithoutOverwriting(bool archive)
    {
        var store = new SimulatedRequisitoStore(); store.Seed();
        var input = Edit(store); input.Titulo = "Tentativa";
        using var db = store.Context();
        store.BeforeSave = () => { store.Rows[0].RowVersion = BitConverter.GetBytes(999L); store.Rows[0].Prioridade = "Baixa"; };
        var result = archive
            ? await new ArquivamentoRequisitoService(db, Access(db), Clock).ArquivarAsync(1, new() { RowVersion = input.RowVersion, Confirmacao = true }, TestContext.Current.CancellationToken)
            : await new EdicaoRequisitoService(db, Access(db), Clock).EditarAsync(1, input, TestContext.Current.CancellationToken);
        Assert.Equal(ResultadoMutacaoRequisito.Conflito, result);
        Assert.Equal("Título original", store.Rows[0].Titulo);
        Assert.Equal("Baixa", store.Rows[0].Prioridade);
        Assert.Equal("Rascunho", store.Rows[0].Status);
        Assert.Equal(0, store.Writes);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("rowversion")]
    [InlineData("archived")]
    [InlineData("foreign")]
    [InlineData("missing")]
    public async Task StaleArchivedOrUnavailableRequirementCannotBeChanged(string reason)
    {
        var store = new SimulatedRequisitoStore(); store.Seed(); var input = Edit(store); input.Titulo = "Tentativa";
        if (reason == "version") input.VersaoEsperada++;
        if (reason == "rowversion") input.RowVersion = Convert.ToBase64String(BitConverter.GetBytes(999L));
        if (reason == "archived") store.Rows[0].Status = "Arquivado";
        using var db = store.Context(); var access = Access(db, reason == "foreign" ? 2 : 1);
        var result = await new EdicaoRequisitoService(db, access, Clock).EditarAsync(reason == "missing" ? 999 : 1, input, TestContext.Current.CancellationToken);
        Assert.Equal(reason switch
        {
            "archived" => ResultadoMutacaoRequisito.Arquivado,
            "foreign" or "missing" => ResultadoMutacaoRequisito.NaoEncontrado,
            _ => ResultadoMutacaoRequisito.Conflito
        }, result);
        Assert.Equal(0, store.Writes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("AQ==")]
    public async Task InvalidConcurrencyTokenFailsServiceValidation(string token)
    {
        var store = new SimulatedRequisitoStore(); store.Seed(); var input = Edit(store); input.RowVersion = token;
        using var db = store.Context();
        await Assert.ThrowsAsync<ValidationException>(() => new EdicaoRequisitoService(db, Access(db), Clock).EditarAsync(1, input, TestContext.Current.CancellationToken));
        Assert.Equal(0, store.Writes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditingAndArchivingLeaveTrackedHistoryAndDecisionsUntouched(bool archive)
    {
        var store = new SimulatedRequisitoStore(); store.Seed(status: "Revisado");
        using var db = store.Context();
        var data = new TestData();
        var analysis = data.Analyses[0];
        analysis.Requisito = db.ChangeTracker.Entries<RequisitaPlus.Web.Models.Requisito>().Single().Entity;
        analysis.RevisaoConcluidaEmUtc = TestData.Instant;
        var pending = data.Suggestions[0];
        var decided = new RequisitaPlus.Web.Models.Sugestao
        {
            IdSugestao = 99, IdAnalise = analysis.IdAnalise, Analise = analysis, OrdemNoResultado = 2,
            TipoProblema = "Ambiguidade", Explicacao = "SIMULADO explicação preservada", TextoSugerido = "SIMULADO proposta",
            Decisao = "Ajustada", TextoAplicado = "SIMULADO redação humana", DecididaEmUtc = TestData.Instant
        };
        db.AttachRange(analysis, pending, decided);
        var historical = db.ChangeTracker.Entries().Where(x => x.Entity is not RequisitaPlus.Web.Models.Requisito)
            .Select(x => (Entry: x, Values: x.CurrentValues.Clone())).ToArray();
        var input = Edit(store); input.Titulo = "Nova versão";
        var result = archive
            ? await new ArquivamentoRequisitoService(db, Access(db), Clock).ArquivarAsync(1,
                new() { RowVersion = input.RowVersion, Confirmacao = true }, TestContext.Current.CancellationToken)
            : await new EdicaoRequisitoService(db, Access(db), Clock).EditarAsync(1, input, TestContext.Current.CancellationToken);
        Assert.Equal(ResultadoMutacaoRequisito.Concluido, result);
        foreach (var snapshot in historical)
        {
            Assert.Equal(EntityState.Unchanged, snapshot.Entry.State);
            foreach (var property in snapshot.Values.Properties)
                Assert.Equal(snapshot.Values[property], snapshot.Entry.CurrentValues[property]);
        }
        Assert.Equal("Pendente", pending.Decisao);
        Assert.Equal("Ajustada", decided.Decisao);
        Assert.Equal(archive ? 1 : 2, store.Rows[0].VersaoTextual);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QueriesProjectStoredValuesAndSeparateArchivedRequirements(int owner)
    {
        var store = new SimulatedRequisitoStore(); store.Seed(1, 1); store.Seed(2, 2); store.Seed(3, owner, "Arquivado");
        using var db = store.Context(); var query = new ConsultaRequisitoService(Access(db, owner));
        var active = Assert.Single(await query.ListarAsync(ct: TestContext.Current.CancellationToken));
        Assert.Equal(owner, active.IdRequisito); Assert.Equal("Título original", active.Titulo);
        Assert.Equal("Descrição original", active.Descricao); Assert.Equal("Alta", active.Prioridade);
        Assert.Equal("Rascunho", active.Status); Assert.Equal(1, active.VersaoTextual);
        var archived = Assert.Single(await query.ListarAsync(true, TestContext.Current.CancellationToken));
        Assert.Equal(3, archived.IdRequisito);
        Assert.Equal(active, await query.DetalhesAsync(owner, TestContext.Current.CancellationToken));
        Assert.Null(await query.DetalhesAsync(3 - owner, TestContext.Current.CancellationToken));
    }
}
