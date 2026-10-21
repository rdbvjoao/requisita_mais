using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.Services;

namespace RequisitaPlus.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; set; } = "/";

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [MinLength(8, ErrorMessage = "A senha deve ter pelo menos oito caracteres.")]
        [RegularExpression(@"^(?=[\s\S]*[A-Z])(?=[\s\S]*[a-z])(?=[\s\S]*[0-9])(?=[\s\S]*[^a-zA-Z0-9])[\s\S]+$", ErrorMessage = "A senha deve conter maiúscula, minúscula, número e caractere especial.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmação da senha")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = LocalReturnUrl(returnUrl);

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = LocalReturnUrl(returnUrl);
        if (!ModelState.IsValid) return Page();
        var user = new ApplicationUser { Nome = Input.Nome, Email = Input.Email, UserName = Input.Email };
        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, Input.Password);
        }
        catch (DbUpdateException exception) when (IsEmailCollision(exception))
        {
            var logger = HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("RequisitaPlus.Sql");
            if (logger != null && SqlErrorDiagnostics.Code(exception) is int code) SqlErrorDiagnostics.Log(logger, code);
            // The unique database index arbitrates a race after Identity validation.
            // Never sign in or redirect as success after a failed INSERT.
            ModelState.AddModelError("Input.Email", "Este e-mail já está cadastrado.");
            return Page();
        }
        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(ReturnUrl);
        }
        foreach (var error in result.Errors)
        {
            var message = error.Code switch
            {
                "DuplicateEmail" or "DuplicateUserName" => "Este e-mail já está cadastrado.",
                "InvalidEmail" or "InvalidUserName" => "Informe um e-mail válido.",
                _ => "A senha deve ter pelo menos oito caracteres, maiúscula, minúscula, número e caractere especial."
            };
            ModelState.AddModelError(string.Empty, message);
        }
        return Page();
    }

    private string LocalReturnUrl(string? value) => Url.IsLocalUrl(value) ? value! : "/";

    private static bool IsEmailCollision(DbUpdateException exception) =>
        exception.Entries.Any(x => x.Entity is ApplicationUser)
        && exception.InnerException is SqlException sql
        && sql.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627
            && (error.Message.Contains("EmailIndex", StringComparison.Ordinal)
                || error.Message.Contains("UserNameIndex", StringComparison.Ordinal)));
}
