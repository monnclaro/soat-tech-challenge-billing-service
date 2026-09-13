using Domain.Orcamentos;
using Domain.Orcamentos.Gateways;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Gateways.Orcamentos;

public class OrcamentoGateway : IOrcamentoGateway
{
    private readonly BillingServiceDbContext _db;

    public OrcamentoGateway(BillingServiceDbContext db) => _db = db;

    public Task<Orcamento?> BuscarPorId(Guid id, CancellationToken ct = default) =>
        _db.Orcamento
            .Include(o => o.Itens)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Orcamento?> BuscarPorIdOrdemServico(Guid idOrdemServico, CancellationToken ct = default) =>
        _db.Orcamento
            .Include(o => o.Itens)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.IdOrdemServico == idOrdemServico, ct);

    public async Task Salvar(Orcamento orcamento, CancellationToken ct = default)
    {
        _db.Orcamento.Add(orcamento);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Atualizar(Orcamento orcamento, CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
