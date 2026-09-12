using System.Diagnostics.CodeAnalysis;
using Domain.Orcamentos.Itens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations.Orcamentos;

// Mapeamento EF Core declarativo (mesma natureza de uma Migration) — sem branching nem
// regra de negócio, só nomes de coluna/tabela.
[ExcludeFromCodeCoverage]
public class OrcamentoItemConfiguration : IEntityTypeConfiguration<OrcamentoItem>
{
    public void Configure(EntityTypeBuilder<OrcamentoItem> builder)
    {
        builder.ToTable("orcamento_item");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.IdItemOrigem).HasColumnName("id_item_origem").IsRequired();

        builder.Property(x => x.NomeItem)
            .HasColumnName("nome_item")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Valor).HasColumnName("valor").HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").IsRequired();

        builder.Property<Guid>("IdOrcamento").HasColumnName("id_orcamento");
    }
}
