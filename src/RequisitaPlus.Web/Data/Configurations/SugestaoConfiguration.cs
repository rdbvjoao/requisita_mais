using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Web.Data.Configurations;

public sealed class SugestaoConfiguration : IEntityTypeConfiguration<Sugestao>
{
    public void Configure(EntityTypeBuilder<Sugestao> builder)
    {
        builder.ToTable("Sugestao", table =>
        {
            table.HasCheckConstraint("CK_Sugestao_OrdemNoResultado", "[OrdemNoResultado] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_Sugestao_TipoProblema", MappingRules.ExactValues("TipoProblema", "Ambiguidade", "Termo vago", "Falta de clareza", "Falta de especificidade"));
            table.HasCheckConstraint("CK_Sugestao_Decisao", MappingRules.ExactValues("Decisao", "Pendente", "Aplicada", "Ajustada", "Descartada"));
            table.HasCheckConstraint("CK_Sugestao_Explicacao", MappingRules.Text("Explicacao", 500));
            table.HasCheckConstraint("CK_Sugestao_TextoAplicado", "[TextoAplicado] IS NULL OR DATALENGTH([TextoAplicado]) <= 10000");
            table.HasCheckConstraint("CK_Sugestao_DecisaoTextoData",
                "([Decisao] = N'Pendente' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NULL) OR " +
                "([Decisao] = N'Descartada' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NOT NULL) OR " +
                "([Decisao] IN (N'Aplicada', N'Ajustada') AND [TextoAplicado] IS NOT NULL AND " +
                MappingRules.NonBlank("TextoAplicado") + " AND [DecididaEmUtc] IS NOT NULL)");
        });
        builder.HasKey(x => x.IdSugestao);
        builder.Property(x => x.IdSugestao).UseIdentityColumn();
        builder.HasOne(x => x.Analise).WithMany().HasForeignKey(x => x.IdAnalise).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.IdAnalise, x.OrdemNoResultado }).IsUnique();
        builder.Property(x => x.TipoProblema).HasColumnType("nvarchar(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Explicacao).HasColumnType("nvarchar(500)").HasMaxLength(500).IsRequired();
        builder.Property(x => x.TextoSugerido).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.Decisao).HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.TextoAplicado).HasColumnType("nvarchar(max)").HasMaxLength(5000).IsRequired(false);
        builder.Property(x => x.DecididaEmUtc).Utc();
        builder.Property(x => x.RowVersion).IsRowVersion().HasColumnType("rowversion");
        // No row CHECK can count the children of an analysis or confirm a human action.
    }
}
