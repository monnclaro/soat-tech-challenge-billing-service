namespace Application.Orcamentos.UseCases;

public record ItemOrcamentoOutput(Guid IdItemOrigem, string NomeItem, decimal Valor, string Tipo);
