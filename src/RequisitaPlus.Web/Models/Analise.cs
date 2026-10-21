using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RequisitaPlus.Web.Models;

/// <summary>
/// Resultado válido de análise, com snapshots independentes do requisito atual.
/// T011 configurará PK, FKs e unicidade; o vínculo de operação será mapeado após T010.
/// Serviços futuros garantirão validade do resultado, snapshots imutáveis após persistência,
/// commit atômico e revisão humana explícita. A entidade não executa essas operações.
/// </summary>
[BindNever]
public class Analise : IValidatableObject
{
    public int IdAnalise { get; set; }
    public int IdRequisito { get; set; }
    public Requisito Requisito { get; set; } = null!;
    public Guid IdOperacao { get; set; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long VersaoTextualAnalisada { get; set; }

    [Required]
    [StringLength(150)]
    public string TituloAnalisado { get; set; } = string.Empty;

    // Comprimento em unidades UTF-16; não transforma o snapshot recebido.
    [Required]
    [StringLength(5000)]
    public string DescricaoAnalisada { get; set; } = string.Empty;

    public DateTime DataAnaliseUtc { get; set; }
    public DateTime PersistidaEmUtc { get; set; }

    // Atribuída em transação; PersistidaEmUtc não determina a análise ativa.
    [Range(typeof(long), "1", "9223372036854775807")]
    public long OrdemPersistencia { get; set; }

    [Required]
    public string Modelo { get; set; } = string.Empty;

    public string? DigestModelo { get; set; }

    // O modelo aprovado não fixa obrigatoriedade, formato ou tamanho deste identificador.
    public string? VersaoPrompt { get; set; }

    [Range(0, int.MaxValue)]
    public int? TokensEntrada { get; set; }

    [Range(0, int.MaxValue)]
    public int? TokensSaida { get; set; }

    // Zero na configuração local; sem cálculo ou suposição de preços de outro provedor.
    public decimal CustoApi { get; set; }

    // Só os serviços de revisão futuros poderão registrar a confirmação humana.
    public DateTime? RevisaoConcluidaEmUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IdOperacao == Guid.Empty)
            yield return new ValidationResult("Informe a operação da análise.", [nameof(IdOperacao)]);

        if (DataAnaliseUtc.Kind != DateTimeKind.Utc)
            yield return new ValidationResult("A data da análise deve representar UTC.", [nameof(DataAnaliseUtc)]);

        if (PersistidaEmUtc.Kind != DateTimeKind.Utc)
            yield return new ValidationResult("A data de persistência deve representar UTC.", [nameof(PersistidaEmUtc)]);

        if (RevisaoConcluidaEmUtc is { Kind: not DateTimeKind.Utc })
            yield return new ValidationResult("A data de revisão deve representar UTC.", [nameof(RevisaoConcluidaEmUtc)]);
    }
}
