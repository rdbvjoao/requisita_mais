using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Web.Data.Configurations;

public sealed class OperacaoAnaliseConfiguration : IEntityTypeConfiguration<OperacaoAnalise>
{
    public void Configure(EntityTypeBuilder<OperacaoAnalise> builder)
    {
        builder.ToTable("OperacaoAnalise", table =>
        {
            table.HasCheckConstraint("CK_OperacaoAnalise_IdOperacao", "[IdOperacao] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint("CK_OperacaoAnalise_VersaoTextual", "[VersaoTextual] > 0");
            table.HasCheckConstraint("CK_OperacaoAnalise_Fingerprint", "DATALENGTH([Fingerprint]) = 128 AND [Fingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9A-Fa-f]%'");
            table.HasCheckConstraint("CK_OperacaoAnalise_Estado", MappingRules.ExactValues("Estado", "Recebida", "Enviada", "ResultadoDisponivel", "Persistida", "FalhaEncerrada", "Incerta"));
            table.HasCheckConstraint("CK_OperacaoAnalise_Modelo", MappingRules.NonBlank("Modelo"));
            table.HasCheckConstraint("CK_OperacaoAnalise_TokensEntrada", "[TokensEntrada] IS NULL OR [TokensEntrada] >= 0");
            table.HasCheckConstraint("CK_OperacaoAnalise_TokensSaida", "[TokensSaida] IS NULL OR [TokensSaida] >= 0");
        });
        builder.HasKey(x => x.IdOperacao);
        builder.Property(x => x.IdOperacao).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Requisito).WithMany().HasForeignKey(x => x.IdRequisito).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.VersaoTextual).HasColumnType("bigint");
        builder.Property(x => x.Fingerprint).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Estado).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CriadaEmUtc).Utc();
        builder.Property(x => x.EnvioRegistradoEmUtc).Utc();
        builder.Property(x => x.EncerradaEmUtc).Utc();
        builder.Property(x => x.PrazoExcedidoEmUtc).Utc();
        builder.Property(x => x.TerminoComprovado).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.MotivoFalha).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.EvidenciaRecuperacao).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.Modelo).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.TokensEntrada).IsRequired(false);
        builder.Property(x => x.TokensSaida).IsRequired(false);
        builder.Property(x => x.CustoApi).HasPrecision(18, 6);
        builder.Property(x => x.RowVersion).IsRowVersion().HasColumnType("rowversion");
    }
}
