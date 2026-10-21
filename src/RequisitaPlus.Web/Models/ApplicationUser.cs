using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RequisitaPlus.Web.Models;

/// <summary>
/// Identidade com chave inteira, compatível com IdentityRole&lt;int&gt;.
/// T011 definirá chave, Unicode, limites de 150 para os campos Identity e índice único.
/// T012/T022 configurarão o cadastro: Email obrigatório e válido (até 150),
/// UserName derivado do Email e campos normalizados pelo normalizador do Identity.
/// NormalizedEmail será obrigatório no cadastro; não é calculado por este modelo.
/// PasswordHash e os demais membros herdados continuam gerenciados pelo Identity.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}
