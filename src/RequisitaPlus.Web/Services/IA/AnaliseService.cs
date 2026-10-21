using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.Services;

namespace RequisitaPlus.Web.Services.IA;

public sealed record PedidoAnalise(int IdRequisito, int IdUsuario, Guid IdOperacao, long Versao, string RowVersion);
public sealed record InicioAnalise(Guid? IdOperacao, string? Erro, bool NaoEncontrado = false);

// Uma instância Web, sem fila. O trabalho e seu escopo não dependem da conexão do navegador.
public sealed class AnaliseService(IServiceScopeFactory scopes, OllamaDisponibilidade disponibilidade,
    IHostApplicationLifetime lifetime, ILogger<AnaliseService> logger)
{
    private readonly SemaphoreSlim capacidade = new(1, 1);
    // Coordena o marcador de prazo com a releitura e o commit do worker local.
    // RowVersion continua detectando alterações feitas fora deste serviço.
    private readonly SemaphoreSlim gravacaoOperacao = new(1, 1);

    public async Task<InicioAnalise> IniciarAsync(PedidoAnalise pedido)
    {
        if (!disponibilidade.TryObter(out var options, out var erro)) return new(null, erro);
        if (!await capacidade.WaitAsync(0))
        {
            using var consulta = scopes.CreateScope();
            var existente = await consulta.ServiceProvider.GetRequiredService<ApplicationDbContext>().OperacoesAnalise
                .AsNoTracking().SingleOrDefaultAsync(x => x.IdOperacao == pedido.IdOperacao && x.IdUsuario == pedido.IdUsuario
                    && x.IdRequisito == pedido.IdRequisito && x.VersaoTextual == pedido.Versao);
            return existente is null ? new(null, "A IA está ocupada. Aguarde a operação atual antes de confirmar outra análise.")
                : new(pedido.IdOperacao, null);
        }
        var inicio = Stopwatch.GetTimestamp();
        var envioRegistrado = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prazoUtc = DateTime.UtcNow.AddSeconds(options!.OperationTimeoutSeconds);
        var tarefa = ExecutarAsync(pedido, options!, inicio, prazoUtc, envioRegistrado);
        if (await Task.WhenAny(tarefa, envioRegistrado.Task) == tarefa) return await tarefa;
        var restante = options!.OperationTimeoutSeconds - Stopwatch.GetElapsedTime(inicio).TotalSeconds;
        if (restante <= 0) return await RegistrarPrazoAsync(pedido.IdOperacao, prazoUtc);
        // ExecutarAsync observa e trata todas as falhas, inclusive após esta espera.
        try { return await tarefa.WaitAsync(TimeSpan.FromSeconds(restante)); }
        catch (TimeoutException) { return await RegistrarPrazoAsync(pedido.IdOperacao, prazoUtc); }
    }

    private async Task<InicioAnalise> ExecutarAsync(PedidoAnalise pedido, OllamaOptions options, long inicio, DateTime prazoUtc, TaskCompletionSource envioRegistrado)
    {
        OperacaoAnalise? operacao = null;
        var envioPossivel = false;
        var terminal = false;
        var gravacaoAdquirida = false;
        var etapa = "Preparacao";
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var requisito = await db.Requisitos.SingleOrDefaultAsync(x => x.IdRequisito == pedido.IdRequisito && x.IdUsuario == pedido.IdUsuario);
            if (requisito is null) return new(null, "Requisito não encontrado.", true);
            var existente = await db.OperacoesAnalise.AsNoTracking().SingleOrDefaultAsync(x => x.IdOperacao == pedido.IdOperacao);
            var fingerprint = Fingerprint(pedido, requisito, options);
            if (existente is not null)
                return existente.IdUsuario == pedido.IdUsuario && existente.Fingerprint == fingerprint
                    ? new(pedido.IdOperacao, null) : new(null, "A confirmação já foi utilizada. Reabra os detalhes do requisito.");
            if (requisito.Status == "Arquivado") return new(null, "Requisitos arquivados são somente leitura.");
            if (requisito.VersaoTextual != pedido.Versao || Convert.ToBase64String(requisito.RowVersion) != pedido.RowVersion)
                return new(null, "O requisito foi alterado. Recarregue os detalhes e confirme novamente.");
            if (await db.OperacoesAnalise.AnyAsync(x => !x.TerminoComprovado &&
                (x.Estado == "Enviada" || x.Estado == "Incerta")))
                return new(null, "IA em recuperação: existe um envio sem término comprovado. Consulte o procedimento operacional.");
            var titulo = requisito.Titulo;
            var descricao = requisito.Descricao;
            var versao = requisito.VersaoTextual;
            var prompt = ContratoIA.Prompt(titulo, descricao, options.MaxProblems);
            if (!ContratoIA.Cabe(prompt, options)) return new(null,
                "O texto excede o contexto seguro da IA. Nenhum texto foi cortado ou enviado; reduza a redação para analisar.");
            var client = scope.ServiceProvider.GetRequiredService<IOllamaClient>();
            using var verificacao = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping);
            verificacao.CancelAfter(TimeSpan.FromSeconds(Math.Min(10, options.OperationTimeoutSeconds)));
            if (!await client.CompativelAsync(options, verificacao.Token)) return new(null,
                "O modelo local não apresenta o tokenizer compatível com a guarda de contexto. A análise não foi enviada.");
            await gravacaoOperacao.WaitAsync();
            gravacaoAdquirida = true;
            etapa = "RegistroEnvio";
            operacao = new OperacaoAnalise
            {
                IdOperacao = pedido.IdOperacao, IdUsuario = pedido.IdUsuario, IdRequisito = pedido.IdRequisito,
                VersaoTextual = versao, Fingerprint = fingerprint, Estado = "Enviada", Modelo = options.Model,
                CriadaEmUtc = prazoUtc.AddSeconds(-options.OperationTimeoutSeconds), EnvioRegistradoEmUtc = DateTime.UtcNow
            };
            db.OperacoesAnalise.Add(operacao);
            await db.SaveChangesAsync(); // Intenção durável ANTES do primeiro byte da inferência.
            envioRegistrado.TrySetResult();
            if (Stopwatch.GetElapsedTime(inicio).TotalSeconds >= options.OperationTimeoutSeconds)
            {
                operacao.Estado = "FalhaEncerrada"; operacao.TerminoComprovado = true;
                operacao.MotivoFalha = "TempoLimite"; operacao.EncerradaEmUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(); return new(pedido.IdOperacao, null);
            }
            gravacaoOperacao.Release();
            gravacaoAdquirida = false;
            etapa = "Inferencia";
            envioPossivel = true;
            var resposta = await client.GerarAsync(prompt, options, lifetime.ApplicationStopping);
            terminal = resposta.TerminoComprovado;
            var excedeu = Stopwatch.GetElapsedTime(inicio).TotalSeconds >= options.OperationTimeoutSeconds;
            await gravacaoOperacao.WaitAsync();
            gravacaoAdquirida = true;
            etapa = "ReleituraResultado";
            using var persistencia = scopes.CreateScope();
            db = persistencia.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            operacao = await db.OperacoesAnalise.SingleAsync(x => x.IdOperacao == pedido.IdOperacao);
            requisito = await db.Requisitos.SingleAsync(x => x.IdRequisito == pedido.IdRequisito && x.IdUsuario == pedido.IdUsuario);
            operacao.TerminoComprovado = terminal;
            operacao.TokensEntrada = resposta.TokensEntrada;
            operacao.TokensSaida = resposta.TokensSaida;
            if (excedeu) operacao.PrazoExcedidoEmUtc = prazoUtc;
            if (excedeu || resposta.Problemas is null)
            {
                operacao.Estado = terminal ? "FalhaEncerrada" : "Incerta";
                operacao.MotivoFalha = excedeu ? "TempoLimite" : "RespostaInvalida";
                operacao.EncerradaEmUtc = terminal ? DateTime.UtcNow : null;
                etapa = "RegistroSemResultado";
                await db.SaveChangesAsync();
                return new(pedido.IdOperacao, null);
            }
            // O contexto novo relê o rowversion; SaveChanges disputa alterações subsequentes.
            var atual = requisito.Status != "Arquivado" && requisito.VersaoTextual == versao;
            requisito.UltimaOrdemAnalise = checked(requisito.UltimaOrdemAnalise + 1);
            if (atual) requisito.Status = "Analisado";
            var analise = new Analise
            {
                IdOperacao = pedido.IdOperacao, IdRequisito = pedido.IdRequisito, VersaoTextualAnalisada = versao,
                TituloAnalisado = titulo, DescricaoAnalisada = descricao, DataAnaliseUtc = operacao.EnvioRegistradoEmUtc!.Value,
                PersistidaEmUtc = DateTime.UtcNow, OrdemPersistencia = requisito.UltimaOrdemAnalise,
                Modelo = options.Model, VersaoPrompt = ContratoIA.VersaoPrompt,
                TokensEntrada = resposta.TokensEntrada, TokensSaida = resposta.TokensSaida
            };
            db.Analises.Add(analise);
            var ordem = 0;
            foreach (var problema in resposta.Problemas)
                db.Sugestoes.Add(new Sugestao { Analise = analise, OrdemNoResultado = ++ordem,
                    TipoProblema = problema.TipoProblema, Explicacao = problema.Explicacao, TextoSugerido = problema.TextoSugerido });
            operacao.Estado = "Persistida";
            operacao.EncerradaEmUtc = DateTime.UtcNow;
            // EF usa transação curta para este único SaveChanges: análise, filhos, ordem e operação.
            etapa = "CommitResultado";
            await db.SaveChangesAsync();
            return new(pedido.IdOperacao, null);
        }
        catch (Exception failure)
        {
            LogarFalha(failure, pedido.IdOperacao, etapa);
            if (operacao is not null)
            {
                try
                {
                    if (!gravacaoAdquirida)
                    {
                        await gravacaoOperacao.WaitAsync();
                        gravacaoAdquirida = true;
                    }
                    using var recuperacao = scopes.CreateScope();
                    var db = recuperacao.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var registro = await db.OperacoesAnalise.SingleOrDefaultAsync(x => x.IdOperacao == pedido.IdOperacao);
                    if (registro is null) return new(null, "Não foi possível registrar a operação. Nenhuma inferência foi enviada.");
                    // Commit ambíguo pode ter concluído. Jamais sobrescrever uma operação persistida.
                    if (registro.Estado != "Persistida")
                    {
                        registro.TerminoComprovado = terminal || !envioPossivel;
                        registro.Estado = registro.TerminoComprovado ? "FalhaEncerrada" : "Incerta";
                        registro.MotivoFalha = terminal ? "ConflitoOuFalhaPersistencia" : "ComunicacaoInterrompida";
                        registro.EncerradaEmUtc = registro.TerminoComprovado ? DateTime.UtcNow : null;
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception recoveryFailure)
                {
                    LogarFalha(recoveryFailure, pedido.IdOperacao, "Recuperacao");
                    // A intenção durável continua a bloquear novos envios.
                }
                return new(pedido.IdOperacao, null);
            }
            return new(null, "Não foi possível concluir a análise. Verifique se o Ollama está disponível e tente novamente.");
        }
        finally
        {
            if (gravacaoAdquirida) gravacaoOperacao.Release();
            capacidade.Release();
        }
    }

    private async Task<InicioAnalise> RegistrarPrazoAsync(Guid id, DateTime prazoUtc)
    {
        // Um commit iniciado com resposta tempestiva pode terminar após o prazo.
        // Não disputar seu rowversion nem prolongar a espera HTTP pelo commit.
        if (!await gravacaoOperacao.WaitAsync(0)) return new(id, null);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var operacao = await db.OperacoesAnalise.SingleOrDefaultAsync(x => x.IdOperacao == id);
            if (operacao is { Estado: "Enviada", TerminoComprovado: false })
            {
                operacao.PrazoExcedidoEmUtc = prazoUtc;
                await db.SaveChangesAsync();
            }
        }
        catch (Exception failure)
        {
            LogarFalha(failure, id, "RegistroPrazo");
            // O envio durável permanece bloqueante; nunca liberar por relógio.
        }
        finally { gravacaoOperacao.Release(); }
        return new(id, null);
    }

    private void LogarFalha(Exception failure, Guid id, string etapa)
    {
        var categoria = failure switch
        {
            DbUpdateConcurrencyException => "Concorrencia",
            DbUpdateException => "Persistencia",
            HttpRequestException => "Comunicacao",
            OperationCanceledException => "Cancelamento",
            _ => "Interna"
        };
        // Não passar a exceção, Message, SQL, parâmetros, prompt ou resposta ao logger.
        logger.LogError(new EventId(1002, "FalhaAnalise"),
            "Falha na análise: operação {IdOperacao}; etapa {Etapa}; categoria {Categoria}; código SQL {SqlCode}.",
            id, etapa, categoria, SqlErrorDiagnostics.Code(failure));
    }

    private static string Fingerprint(PedidoAnalise pedido, Requisito requisito, OllamaOptions options) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        { pedido.IdUsuario, pedido.IdRequisito, pedido.Versao, requisito.Titulo, requisito.Descricao, options, ContratoIA.VersaoPrompt }))));
}
