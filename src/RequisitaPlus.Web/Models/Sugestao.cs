using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RequisitaPlus.Web.Models;

/// <summary>
/// Apontamento da IA e registro separado da decisão humana (RT1).
/// T011 definirá PK, FK, unicidade, CHECKs combinados e rowversion.
/// T050/T056 validarão e persistirão a resposta; T079–T087 controlarão
/// confirmação, decisão definitiva e alteração atômica do requisito.
/// Esta entidade não aplica propostas nem altera os textos automaticamente.
/// </summary>
[BindNever]
public class Sugestao : IValidatableObject
{
    public int IdSugestao { get; set; }
    public int IdAnalise { get; set; }
    public Analise Analise { get; set; } = null!;

    [Range(1, 5)]
    public int OrdemNoResultado { get; set; }

    [Required]
    [StringLength(50)]
    [AllowedValues("Ambiguidade", "Termo vago", "Falta de clareza", "Falta de especificidade")]
    public string TipoProblema { get; set; } = string.Empty;

    // Limite em unidades UTF-16; um exemplo opcional integra o mesmo texto.
    [Required]
    [StringLength(500)]
    public string Explicacao { get; set; } = string.Empty;

    // Proposta original da IA: ausência é null; sem limite funcional adicional.
    public string? TextoSugerido { get; set; }

    [Required]
    [StringLength(20)]
    [AllowedValues("Pendente", "Aplicada", "Ajustada", "Descartada")]
    public string Decisao { get; set; } = "Pendente";

    // Descrição final confirmada, independente da proposta original da IA.
    // Pendente/Descartada sem texto; Aplicada/Ajustada com texto válido e data:
    // coerência combinada será exigida no serviço e nos CHECKs futuros.
    [StringLength(5000)]
    public string? TextoAplicado { get; set; }

    public DateTime? DecididaEmUtc { get; set; }

    // Gerenciado futuramente pelo SQL Server; não é a versão textual do requisito.
    public byte[] RowVersion { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DecididaEmUtc is { Kind: not DateTimeKind.Utc })
            yield return new ValidationResult("A data da decisão deve representar UTC.", [nameof(DecididaEmUtc)]);
    }
}
