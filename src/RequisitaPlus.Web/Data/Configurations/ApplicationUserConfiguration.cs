using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Web.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Keep the standard Identity tables, keys and auxiliary relationships.
        builder.Property(x => x.Nome).HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.UserName).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NormalizedUserName).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
        builder.ToTable("AspNetUsers", table =>
            table.HasCheckConstraint("CK_AspNetUsers_Nome", MappingRules.Text("Nome", 100)));
        // Syntax, derivation and normalization of email remain in Identity/cadastro.
    }
}
