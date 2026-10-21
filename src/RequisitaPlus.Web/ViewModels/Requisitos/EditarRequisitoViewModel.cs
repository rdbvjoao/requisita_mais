using System.ComponentModel.DataAnnotations;

namespace RequisitaPlus.Web.ViewModels.Requisitos;

public sealed class EditarRequisitoViewModel : CriarRequisitoViewModel, IValidatableObject
{
    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Recarregue a página para obter a versão atual.")]
    public long VersaoEsperada { get; set; }

    [Required(ErrorMessage = "Recarregue a página para obter a versão atual.")]
    public string RowVersion { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ConcorrenciaRequisitoViewModel.ValidarToken(RowVersion);
}

public sealed class ConcorrenciaRequisitoViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Recarregue a página para obter a versão atual.")]
    public string RowVersion { get; set; } = string.Empty;
    public bool Confirmacao { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in ValidarToken(RowVersion)) yield return error;
        if (!Confirmacao)
            yield return new ValidationResult("Confirme o arquivamento do requisito.", [nameof(Confirmacao)]);
    }

    internal static IEnumerable<ValidationResult> ValidarToken(string token)
    {
        var buffer = new byte[8];
        if (string.IsNullOrEmpty(token) || !Convert.TryFromBase64String(token, buffer, out var size) || size != 8)
            yield return new ValidationResult("Versão inválida. Recarregue a página.", [nameof(RowVersion)]);
    }
}
