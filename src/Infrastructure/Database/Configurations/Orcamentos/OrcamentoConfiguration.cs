using Domain.Orcamentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations.Orcamentos;

public class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("orcamento");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.IdOrdemServico).HasColumnName("id_ordem_servico").IsRequired();
        builder.HasIndex(x => x.IdOrdemServico).IsUnique();

        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.ValorTotal).HasColumnName("valor_total").HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();

        // Chave estrangeira "sombra" — OrcamentoItem não expõe IdOrcamento como propriedade
        // de domínio (ver comentário em OrcamentoItem.cs).
        builder.HasMany(x => x.Itens)
            .WithOne()
            .HasForeignKey("IdOrcamento")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
