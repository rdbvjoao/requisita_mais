using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Web.Data.Configurations;

public sealed class RequisitoConfiguration : IEntityTypeConfiguration<Requisito>
{
    public void Configure(EntityTypeBuilder<Requisito> builder)
    {
        builder.ToTable("Requisito", table =>
        {
            table.HasCheckConstraint("CK_Requisito_Titulo", MappingRules.Text("Titulo", 150));
            table.HasCheckConstraint("CK_Requisito_Descricao", MappingRules.Text("Descricao", 5000));
            table.HasCheckConstraint("CK_Requisito_Prioridade", MappingRules.ExactValues("Prioridade", "Alta", "Média", "Baixa"));
            table.HasCheckConstraint("CK_Requisito_Status", MappingRules.ExactValues("Status", "Rascunho", "Analisado", "Revisado", "Arquivado"));
            table.HasCheckConstraint("CK_Requisito_VersaoTextual", "[VersaoTextual] > 0");
            table.HasCheckConstraint("CK_Requisito_UltimaOrdemAnalise", "[UltimaOrdemAnalise] >= 0");
        });
        builder.HasKey(x => x.IdRequisito);
        builder.Property(x => x.IdRequisito).UseIdentityColumn();
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Titulo).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descricao).HasColumnType("nvarchar(max)").HasMaxLength(5000).IsRequired();
        builder.Property(x => x.Prioridade).HasColumnType("nvarchar(5)").HasMaxLength(5).IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.VersaoTextual).HasColumnType("bigint");
        builder.Property(x => x.UltimaOrdemAnalise).HasColumnType("bigint");
        builder.Property(x => x.DataCadastroUtc).Utc();
        builder.Property(x => x.DataAtualizacaoUtc).Utc();
        builder.Property(x => x.RowVersion).IsRowVersion().HasColumnType("rowversion");
        builder.HasIndex(x => new { x.IdUsuario, x.Status, x.IdRequisito });
    }
}
