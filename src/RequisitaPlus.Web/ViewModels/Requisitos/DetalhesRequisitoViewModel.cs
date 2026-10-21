namespace RequisitaPlus.Web.ViewModels.Requisitos;

public sealed record DetalhesRequisitoViewModel(int IdRequisito, string Titulo, string Descricao,
    string Prioridade, string Status, long VersaoTextual, DateTime DataCadastroUtc,
    DateTime? DataAtualizacaoUtc, string RowVersion);
