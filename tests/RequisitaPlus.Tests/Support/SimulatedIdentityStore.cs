using Microsoft.AspNetCore.Identity;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Tests.Support;

/// <summary>Identity store double. Proves HTTP behavior, never SQL constraints.</summary>
public sealed class SimulatedIdentityStore : IUserPasswordStore<ApplicationUser>, IUserEmailStore<ApplicationUser>, IUserSecurityStampStore<ApplicationUser>
{
    private readonly List<ApplicationUser> users = [];
    public IReadOnlyList<ApplicationUser> Users => users;
    public void Dispose() { }
    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct)
    {
        lock (users)
        {
            if (users.Any(x => x.NormalizedEmail == user.NormalizedEmail))
                return Task.FromResult(IdentityResult.Failed(new IdentityErrorDescriber().DuplicateEmail(user.Email!)));
            user.Id = users.Count + 1;
            users.Add(user);
        }
        return Task.FromResult(IdentityResult.Success);
    }
    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(IdentityResult.Success);
    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) => throw new NotSupportedException();
    public Task<ApplicationUser?> FindByIdAsync(string id, CancellationToken ct) => Task.FromResult(users.SingleOrDefault(x => x.Id.ToString() == id));
    public Task<ApplicationUser?> FindByNameAsync(string name, CancellationToken ct) => Task.FromResult(users.SingleOrDefault(x => x.NormalizedUserName == name));
    public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(users.SingleOrDefault(x => x.NormalizedEmail == email));
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Id.ToString());
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.UserName);
    public Task SetUserNameAsync(ApplicationUser user, string? value, CancellationToken ct) { user.UserName = value; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.NormalizedUserName);
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? value, CancellationToken ct) { user.NormalizedUserName = value; return Task.CompletedTask; }
    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Email);
    public Task SetEmailAsync(ApplicationUser user, string? value, CancellationToken ct) { user.Email = value; return Task.CompletedTask; }
    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.NormalizedEmail);
    public Task SetNormalizedEmailAsync(ApplicationUser user, string? value, CancellationToken ct) { user.NormalizedEmail = value; return Task.CompletedTask; }
    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.EmailConfirmed);
    public Task SetEmailConfirmedAsync(ApplicationUser user, bool value, CancellationToken ct) { user.EmailConfirmed = value; return Task.CompletedTask; }
    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash);
    public Task SetPasswordHashAsync(ApplicationUser user, string? value, CancellationToken ct) { user.PasswordHash = value; return Task.CompletedTask; }
    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash != null);
    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.SecurityStamp);
    public Task SetSecurityStampAsync(ApplicationUser user, string value, CancellationToken ct) { user.SecurityStamp = value; return Task.CompletedTask; }
}
