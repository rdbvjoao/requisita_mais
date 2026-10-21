using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Services;
using RequisitaPlus.Web.Services.IA;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class PilotInfrastructureTests
{
    [Theory]
    [InlineData(2026, 10, 19, -3)]
    [InlineData(2018, 1, 15, -2)]
    public void PresentationUsesZoneRulesIncludingHistoricalDaylightSaving(int year, int month, int day, int offset)
    {
        var instant = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);
        var clock = new ControlledClock(instant);
        var local = new RelogioLocal(clock);
        Assert.Equal(instant, local.AgoraUtc);
        Assert.Equal(DateTimeKind.Utc, local.AgoraUtc.Kind);
        Assert.Equal(TimeSpan.FromHours(offset), local.ParaApresentacao(instant).Offset);
        Assert.Equal(12 + offset, local.ParaApresentacao(instant).Hour);
        clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal(instant.AddMinutes(10), local.AgoraUtc);
        Assert.Equal(local.AgoraUtc, local.AgoraUtc);
        Assert.Throws<ArgumentException>(() => local.ParaApresentacao(DateTime.SpecifyKind(instant, DateTimeKind.Unspecified)));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task ErrorResponseAndLogsDoNotCopySensitiveRequestOrExceptionContent()
    {
        var sink = new Sink();
        var services = new ServiceCollection();
        services.AddLogging(logging => { Program.ConfigureLogging(logging); logging.AddProvider(sink); });
        using var provider = services.BuildServiceProvider();
        var formatting = provider.GetRequiredService<IOptions<SimpleConsoleFormatterOptions>>().Value;
        Assert.True(formatting.UseUtcTimestamp);
        Assert.Contains("'Z'", formatting.TimestampFormat);
        var context = new DefaultHttpContext { RequestServices = provider };
        const string sensitive = "SIMULADO senha token prompt descrição resposta";
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature { Error = new Exception(sensitive) });
        context.Request.Headers.Authorization = sensitive;
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sensitive));
        context.Response.Body = new MemoryStream();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware");
        Assert.False(logger.IsEnabled(LogLevel.Error));
        await Program.HandleErrorAsync(context);
        context.Response.Body.Position = 0;
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain(sensitive, response);
        Assert.DoesNotContain(sensitive, Assert.Single(sink.Messages));
        Assert.Equal("Não foi possível concluir a solicitação.", sink.Messages[0]);
    }

    [Fact]
    public void SimulatedGraphsAreValidFreshAndPreserveTextVersions()
    {
        var data = new TestData();
        foreach (var item in data.Users.Cast<object>().Concat(data.Requirements).Concat(data.Analyses).Concat(data.Suggestions).Concat(data.Operations))
        {
            var errors = new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(item, new ValidationContext(item), errors, true), string.Join(",", errors));
        }
        Assert.Equal(2, data.Users.Select(x => x.Id).Distinct().Count());
        foreach (var analysis in data.Analyses)
        {
            Assert.True(analysis.VersaoTextualAnalisada < analysis.Requisito.VersaoTextual);
            Assert.NotEqual(analysis.DescricaoAnalisada, analysis.Requisito.Descricao);
            var operation = Assert.Single(data.Operations, x => x.IdOperacao == analysis.IdOperacao);
            Assert.Equal(analysis.IdRequisito, operation.IdRequisito);
            Assert.Equal(analysis.Requisito.IdUsuario, operation.IdUsuario);
            Assert.True(operation.TerminoComprovado);
            Assert.Equal("Persistida", operation.Estado);
            Assert.Contains("SIMULADO", analysis.DescricaoAnalisada);
        }
        data.Requirements[0].Descricao = "alterada";
        Assert.Contains("SIMULADO", new TestData().Requirements[0].Descricao);
    }

    [Fact]
    public async Task SimulatedHttpBarrierControlsCallsWithoutDelaysOrNetwork()
    {
        using var handler = new FakeOllamaHandler();
        var barrier = handler.EnqueueBlocked("{\"simulado\":true}");
        handler.EnqueueJson("{}", System.Net.HttpStatusCode.ServiceUnavailable);
        using var client = new HttpClient(handler);
        var pending = client.GetAsync("http://simulado.invalid/", TestContext.Current.CancellationToken);
        await barrier.WaitUntilEnteredAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(pending.IsCompleted);
        Assert.Equal(1, handler.Calls);
        using var second = await client.GetAsync("http://simulado.invalid/", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, second.StatusCode);
        barrier.Complete();
        using var first = await pending;
        Assert.Equal("{\"simulado\":true}", await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, handler.Calls);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("http://simulado.invalid/", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SimulatedHttpSupportsCancellationAndTransportFailures()
    {
        using var handler = new FakeOllamaHandler();
        var barrier = handler.EnqueueBlocked("{}");
        using var client = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        var pending = client.GetAsync("http://simulado.invalid/", cancellation.Token);
        await barrier.WaitUntilEnteredAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        handler.Enqueue((_, _) => throw new HttpRequestException("SIMULADO transporte interrompido"));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("http://simulado.invalid/", TestContext.Current.CancellationToken));
        Assert.Equal(2, handler.Calls);
    }

    private sealed class Sink : ILoggerProvider, ILogger
    {
        public List<string> Messages { get; } = [];
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
