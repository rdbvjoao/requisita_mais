using System.ComponentModel.DataAnnotations;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.ViewModels.Requisitos;

namespace RequisitaPlus.Web.ViewModels.Analises;

public sealed class ConfirmarAnaliseViewModel : IValidatableObject
{
    public Guid IdOperacao { get; set; }
    [Range(1, long.MaxValue)]
    public long Versao { get; set; }
    [Required]
    public string RowVersion { get; set; } = "";
    public bool Confirmacao { get; set; }
    public DetalhesRequisitoViewModel? Requisito { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (IdOperacao == Guid.Empty) yield return new("Reabra a confirmação da análise.", [nameof(IdOperacao)]);
        if (!Confirmacao) yield return new("Confirme o processamento local pela IA.", [nameof(Confirmacao)]);
        foreach (var erro in ConcorrenciaRequisitoViewModel.ValidarToken(RowVersion)) yield return erro;
    }
}

public sealed record HistoricoAnaliseViewModel(DetalhesRequisitoViewModel Requisito, IReadOnlyList<Analise> Analises);
public sealed record ResultadoAnaliseViewModel(Analise Analise, IReadOnlyList<Sugestao> Sugestoes, bool Atual);
