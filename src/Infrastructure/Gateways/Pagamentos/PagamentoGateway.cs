using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Gateways.Pagamentos;

public class PagamentoGateway : IPagamentoGateway
{
    private readonly BillingServiceDbContext _db;

    public PagamentoGateway(BillingServiceDbContext db) => _db = db;

    public Task<Pagamento?> BuscarPorId(Guid id, CancellationToken ct = default) =>
        _db.Pagamento.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Pagamento?> BuscarPorIdOrcamento(Guid idOrcamento, CancellationToken ct = default) =>
        _db.Pagamento.FirstOrDefaultAsync(p => p.IdOrcamento == idOrcamento, ct);

    public Task<Pagamento?> BuscarPorPreferenceId(string preferenceId, CancellationToken ct = default) =>
        _db.Pagamento.FirstOrDefaultAsync(p => p.PreferenceId == preferenceId, ct);

    public async Task Salvar(Pagamento pagamento, CancellationToken ct = default)
    {
        _db.Pagamento.Add(pagamento);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Atualizar(Pagamento pagamento, CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
