namespace Application.Pagamentos.UseCases;

public record PagamentoOutput(
    Guid Id,
    Guid IdOrcamento,
    string PreferenceId,
    string LinkPagamento,
    string? PaymentId,
    string Status,
    decimal Valor,
    DateTime DataCriacao,
    DateTime? DataAtualizacao);
