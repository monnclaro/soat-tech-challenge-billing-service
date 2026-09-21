namespace Application.Orcamentos.UseCases;

public record OrcamentoOutput(
    Guid Id,
    Guid IdOrdemServico,
    IReadOnlyList<ItemOrcamentoOutput> Itens,
    decimal ValorTotal,
    string Status,
    DateTime DataCriacao,
    string? LinkPagamento = null);
