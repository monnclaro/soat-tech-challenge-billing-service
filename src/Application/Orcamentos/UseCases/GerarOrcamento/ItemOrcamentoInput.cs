using Domain.Orcamentos.Enums;

namespace Application.Orcamentos.UseCases.GerarOrcamento;

public record ItemOrcamentoInput(Guid IdItemOrigem, string NomeItem, decimal Valor, TipoItemOrcamento Tipo);
