using System.ComponentModel.DataAnnotations;

namespace RequisitaPlus.Web.ViewModels.Requisitos;

public class CriarRequisitoViewModel
{
    [Display(Name = "Título")]
    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(150, ErrorMessage = "O título deve ter até 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Display(Name = "Descrição")]
    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(5000, ErrorMessage = "A descrição deve ter até 5.000 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha a prioridade.")]
    [AllowedValues("Alta", "Média", "Baixa", ErrorMessage = "Escolha Alta, Média ou Baixa.")]
    public string Prioridade { get; set; } = string.Empty;
}
