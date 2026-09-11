namespace Domain.Orcamentos.Gateways;

public interface IOrcamentoGateway
{
    Task<Orcamento?> BuscarPorId(Guid id, CancellationToken ct = default);
    Task<Orcamento?> BuscarPorIdOrdemServico(Guid idOrdemServico, CancellationToken ct = default);
    Task Salvar(Orcamento orcamento, CancellationToken ct = default);
    Task Atualizar(Orcamento orcamento, CancellationToken ct = default);
}
