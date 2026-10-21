using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RequisitaPlus.Web.Models;

/// <summary>
/// Estrutura do requisito; não implementa cadastro, autorização ou transições.
/// T011 configurará PK identity, FK obrigatória, tipos SQL, CHECKs e rowversion.
/// Os serviços atribuirão proprietário, status, datas e contadores; entradas HTTP
/// deverão usar ViewModels próprios. BindNever não substitui essa separação.
/// </summary>
public class Requisito : IValidatableObject
{
    [BindNever]
    public int IdRequisito { get; set; }

    // FK não anulável; existência e integridade referencial dependem da T011.
    [BindNever]
    public int IdUsuario { get; set; }

    [BindNever]
    public ApplicationUser Usuario { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    // StringLength conta unidades UTF-16, sem truncamento ou limite do contexto IA.
    [Required]
    [StringLength(5000)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [StringLength(5)]
    [AllowedValues("Alta", "Média", "Baixa")]
    public string Prioridade { get; set; } = string.Empty;

    // O estado inicial Rascunho será atribuído pelo serviço de cadastro.
    [BindNever]
    [Required]
    [StringLength(20)]
    [AllowedValues("Rascunho", "Analisado", "Revisado", "Arquivado")]
    public string Status { get; set; } = string.Empty;

    [BindNever]
    [Range(typeof(long), "1", "9223372036854775807")]
    public long VersaoTextual { get; set; } = 1;

    [BindNever]
    [Range(typeof(long), "0", "9223372036854775807")]
    public long UltimaOrdemAnalise { get; set; }

    [BindNever]
    public DateTime DataCadastroUtc { get; set; }

    [BindNever]
    public DateTime? DataAtualizacaoUtc { get; set; }

    // Gerado pelo SQL Server após o mapeamento futuro; não é VersaoTextual.
    [BindNever]
    public byte[] RowVersion { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataCadastroUtc.Kind != DateTimeKind.Utc)
            yield return new ValidationResult("A data de cadastro deve representar UTC.", [nameof(DataCadastroUtc)]);

        if (DataAtualizacaoUtc is { Kind: not DateTimeKind.Utc })
            yield return new ValidationResult("A data de atualização deve representar UTC.", [nameof(DataAtualizacaoUtc)]);
    }
}
