using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Tests.Support;

/// <summary>Grafo novo por instância. SIMULADO: nunca é seed ou histórico de produção.</summary>
public sealed class TestData
{
    public static readonly DateTime Instant = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
    public ApplicationUser[] Users { get; }
    public Requisito[] Requirements { get; }
    public Analise[] Analyses { get; }
    public Sugestao[] Suggestions { get; }
    public OperacaoAnalise[] Operations { get; }

    public TestData()
    {
        Users = [User(1), User(2)];
        Requirements = Users.Select(u => new Requisito
        {
            IdRequisito = u.Id, IdUsuario = u.Id, Usuario = u,
            Titulo = $"SIMULADO requisito {u.Id}", Descricao = "SIMULADO descrição atual versão 2.",
            Prioridade = "Alta", Status = "Rascunho", VersaoTextual = 2,
            UltimaOrdemAnalise = 1, DataCadastroUtc = Instant, DataAtualizacaoUtc = Instant.AddMinutes(1)
        }).ToArray();
        Operations = Requirements.Select(r => new OperacaoAnalise
        {
            IdOperacao = Guid.Parse($"00000000-0000-0000-0000-{r.IdRequisito:D12}"),
            IdUsuario = r.IdUsuario, Usuario = r.Usuario, IdRequisito = r.IdRequisito, Requisito = r,
            VersaoTextual = 1,
            // Identificador sintético; não representa fingerprint de envio real.
            Fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"SIMULADO {r.IdRequisito}"))),
            Estado = "Persistida", CriadaEmUtc = Instant, EnvioRegistradoEmUtc = Instant.AddSeconds(1),
            EncerradaEmUtc = Instant.AddSeconds(2), TerminoComprovado = true,
            Modelo = "qwen2.5:3b", TokensEntrada = 100, TokensSaida = 50, CustoApi = 0
        }).ToArray();
        Analyses = Operations.Select(o => new Analise
        {
            IdAnalise = o.IdRequisito, IdRequisito = o.IdRequisito, Requisito = o.Requisito,
            IdOperacao = o.IdOperacao, VersaoTextualAnalisada = 1,
            TituloAnalisado = o.Requisito.Titulo, DescricaoAnalisada = "SIMULADO descrição anterior versão 1.",
            DataAnaliseUtc = o.EncerradaEmUtc!.Value, PersistidaEmUtc = Instant.AddSeconds(3),
            OrdemPersistencia = 1, Modelo = o.Modelo, TokensEntrada = o.TokensEntrada, TokensSaida = o.TokensSaida
        }).ToArray();
        Suggestions = Analyses.Select(a => new Sugestao
        {
            IdSugestao = a.IdAnalise, IdAnalise = a.IdAnalise, Analise = a, OrdemNoResultado = 1,
            TipoProblema = "Termo vago", Explicacao = "SIMULADO: falta um limite verificável.",
            TextoSugerido = "SIMULADO proposta integral de descrição.", Decisao = "Pendente"
        }).ToArray();
    }

    public static ClaimsPrincipal Principal(int id) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, id.ToString(System.Globalization.CultureInfo.InvariantCulture))], "Simulado"));
    private static ApplicationUser User(int id) => new()
    {
        Id = id, Nome = $"SIMULADO usuário {id}", Email = $"simulado{id}@example.invalid",
        UserName = $"simulado{id}@example.invalid", NormalizedEmail = $"SIMULADO{id}@EXAMPLE.INVALID",
        NormalizedUserName = $"SIMULADO{id}@EXAMPLE.INVALID"
    };
}
