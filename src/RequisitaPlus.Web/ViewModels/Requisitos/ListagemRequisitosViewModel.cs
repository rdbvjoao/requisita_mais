using System.ComponentModel.DataAnnotations;

namespace RequisitaPlus.Web.ViewModels.Requisitos;

public sealed class FiltrosRequisitosViewModel
{
    [StringLength(150, ErrorMessage = "A pesquisa deve ter até 150 caracteres.")]
    public string? Pesquisa { get; set; }

    [AllowedValues(null, "", "Alta", "Média", "Baixa", ErrorMessage = "Selecione uma prioridade válida.")]
    public string? Prioridade { get; set; }

    [AllowedValues(null, "", "Rascunho", "Analisado", "Revisado", "Arquivado", ErrorMessage = "Selecione um status válido.")]
    public string? Status { get; set; }

    public bool TemFiltros => !string.IsNullOrWhiteSpace(Pesquisa)
        || !string.IsNullOrEmpty(Prioridade) || !string.IsNullOrEmpty(Status);
}

public sealed record IndicadoresRequisitosViewModel(int Ativos, int AnalisadosPelaIa, int SugestoesPendentes, int Arquivados);

public sealed record ListagemRequisitosViewModel(FiltrosRequisitosViewModel Filtros,
    IndicadoresRequisitosViewModel Indicadores, IReadOnlyList<DetalhesRequisitoViewModel> Requisitos);
