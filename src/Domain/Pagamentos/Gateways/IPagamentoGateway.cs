namespace Domain.Pagamentos.Gateways;

public interface IPagamentoGateway
{
    Task<Pagamento?> BuscarPorId(Guid id, CancellationToken ct = default);
    Task<Pagamento?> BuscarPorIdOrcamento(Guid idOrcamento, CancellationToken ct = default);
    Task<Pagamento?> BuscarPorPreferenceId(string preferenceId, CancellationToken ct = default);
    Task Salvar(Pagamento pagamento, CancellationToken ct = default);
    Task Atualizar(Pagamento pagamento, CancellationToken ct = default);
}
