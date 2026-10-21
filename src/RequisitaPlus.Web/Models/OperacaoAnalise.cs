using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RequisitaPlus.Web.Models;

/// <summary>
/// Estrutura do diário técnico, sem envio, transições ou persistência.
/// T011 mapeará chave, FKs e rowversion; T052 criará GUID/fingerprint por confirmação
/// e verificará a propriedade. T053–T057 cuidarão de execução e recuperação.
/// Códigos de falha e evidências devem ser mínimos, sem prompts ou textos sensíveis.
/// As validações locais não garantem idempotência ou integridade SQL.
/// </summary>
[BindNever]
public class OperacaoAnalise : IValidatableObject
{
    public Guid IdOperacao { get; set; }
    public int IdUsuario { get; set; }
    public ApplicationUser Usuario { get; set; } = null!;
    public int IdRequisito { get; set; }
    public Requisito Requisito { get; set; } = null!;

    [Range(typeof(long), "1", "9223372036854775807")]
    public long VersaoTextual { get; set; }

    // Representação aprovada para T010: valida ambos os casos, sem normalizar.
    // A geração futura deverá produzir hexadecimal minúsculo (T052).
    [Required]
    [StringLength(64, MinimumLength = 64)]
    [RegularExpression(@"\A[0-9A-Fa-f]{64}\z")]
    public string Fingerprint { get; set; } = string.Empty;

    [Required]
    [AllowedValues("Recebida", "Enviada", "ResultadoDisponivel", "Persistida", "FalhaEncerrada", "Incerta")]
    public string Estado { get; set; } = string.Empty;

    public DateTime CriadaEmUtc { get; set; }
    public DateTime? EnvioRegistradoEmUtc { get; set; }
    public DateTime? EncerradaEmUtc { get; set; }

    // Prazo excedido não prova término remoto nem libera capacidade.
    public DateTime? PrazoExcedidoEmUtc { get; set; }
    public bool TerminoComprovado { get; set; }

    public string? MotivoFalha { get; set; }
    public string? EvidenciaRecuperacao { get; set; }

    [Required]
    public string Modelo { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int? TokensEntrada { get; set; }

    [Range(0, int.MaxValue)]
    public int? TokensSaida { get; set; }

    // Zero na configuração local; nenhum consumo ou preço é calculado aqui.
    public decimal CustoApi { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IdOperacao == Guid.Empty)
            yield return new ValidationResult("Informe o identificador da operação.", [nameof(IdOperacao)]);

        if (CriadaEmUtc.Kind != DateTimeKind.Utc)
            yield return new ValidationResult("A data de criação deve representar UTC.", [nameof(CriadaEmUtc)]);

        if (EnvioRegistradoEmUtc is { Kind: not DateTimeKind.Utc })
            yield return new ValidationResult("A data de envio deve representar UTC.", [nameof(EnvioRegistradoEmUtc)]);

        if (EncerradaEmUtc is { Kind: not DateTimeKind.Utc })
            yield return new ValidationResult("A data de encerramento deve representar UTC.", [nameof(EncerradaEmUtc)]);

        if (PrazoExcedidoEmUtc is { Kind: not DateTimeKind.Utc })
            yield return new ValidationResult("A data do prazo excedido deve representar UTC.", [nameof(PrazoExcedidoEmUtc)]);
    }
}
