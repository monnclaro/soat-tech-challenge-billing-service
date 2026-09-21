namespace Application.Orcamentos.UseCases.GerarOrcamento;

// Disparado tanto pelo consumer RabbitMQ/MassTransit do comando "GerarOrcamento"
// (publicado pelo OS Service ao consumir o evento DiagnosticoFinalizado do Execução
// Service) quanto pelo endpoint REST interno/manual (POST /api/v1/orcamentos),
// mantido para depuração/teste manual.
public record GerarOrcamentoInput(Guid IdOrdemServico, List<ItemOrcamentoInput> Itens, decimal ValorTotal);
