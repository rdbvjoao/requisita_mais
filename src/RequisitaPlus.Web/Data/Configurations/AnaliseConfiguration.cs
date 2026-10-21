using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Web.Data.Configurations;

public sealed class AnaliseConfiguration : IEntityTypeConfiguration<Analise>
{
    public void Configure(EntityTypeBuilder<Analise> builder)
    {
        builder.ToTable("Analise", table =>
        {
            table.HasCheckConstraint("CK_Analise_TituloAnalisado", MappingRules.Text("TituloAnalisado", 150));
            table.HasCheckConstraint("CK_Analise_DescricaoAnalisada", MappingRules.Text("DescricaoAnalisada", 5000));
            table.HasCheckConstraint("CK_Analise_VersaoTextualAnalisada", "[VersaoTextualAnalisada] > 0");
            table.HasCheckConstraint("CK_Analise_OrdemPersistencia", "[OrdemPersistencia] > 0");
            table.HasCheckConstraint("CK_Analise_Modelo", MappingRules.NonBlank("Modelo"));
            table.HasCheckConstraint("CK_Analise_TokensEntrada", "[TokensEntrada] IS NULL OR [TokensEntrada] >= 0");
            table.HasCheckConstraint("CK_Analise_TokensSaida", "[TokensSaida] IS NULL OR [TokensSaida] >= 0");
        });
        builder.HasKey(x => x.IdAnalise);
        builder.Property(x => x.IdAnalise).UseIdentityColumn();
        builder.HasOne(x => x.Requisito).WithMany().HasForeignKey(x => x.IdRequisito).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperacaoAnalise>().WithOne().HasForeignKey<Analise>(x => x.IdOperacao).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.IdOperacao).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.HasIndex(x => x.IdOperacao).IsUnique();
        builder.HasIndex(x => new { x.IdRequisito, x.OrdemPersistencia }).IsUnique();
        builder.HasIndex(x => new { x.IdRequisito, x.VersaoTextualAnalisada, x.OrdemPersistencia }).IsDescending(false, false, true);
        builder.Property(x => x.VersaoTextualAnalisada).HasColumnType("bigint");
        builder.Property(x => x.OrdemPersistencia).HasColumnType("bigint");
        builder.Property(x => x.TituloAnalisado).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescricaoAnalisada).HasColumnType("nvarchar(max)").HasMaxLength(5000).IsRequired();
        builder.Property(x => x.Modelo).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.DigestModelo).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.VersaoPrompt).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.DataAnaliseUtc).Utc();
        builder.Property(x => x.PersistidaEmUtc).Utc();
        builder.Property(x => x.RevisaoConcluidaEmUtc).Utc();
        builder.Property(x => x.TokensEntrada).IsRequired(false);
        builder.Property(x => x.TokensSaida).IsRequired(false);
        builder.Property(x => x.CustoApi).HasPrecision(18, 6);
    }
}
