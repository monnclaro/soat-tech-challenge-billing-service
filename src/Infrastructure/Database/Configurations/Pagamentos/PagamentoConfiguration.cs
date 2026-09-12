using System.Diagnostics.CodeAnalysis;
using Domain.Pagamentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations.Pagamentos;

// Mapeamento EF Core declarativo (mesma natureza de uma Migration) — sem branching nem
// regra de negócio, só nomes de coluna/tabela.
[ExcludeFromCodeCoverage]
public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("pagamento");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.IdOrcamento).HasColumnName("id_orcamento").IsRequired();
        builder.HasIndex(x => x.IdOrcamento).IsUnique();

        builder.Property(x => x.PreferenceId).HasColumnName("preference_id").HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.PreferenceId).IsUnique();

        builder.Property(x => x.PaymentId).HasColumnName("payment_id").HasMaxLength(100);

        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.Valor).HasColumnName("valor").HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao");
    }
}
