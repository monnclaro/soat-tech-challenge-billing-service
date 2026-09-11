namespace Application.Orcamentos.UseCases.GerarOrcamento;

// Hoje exposto como endpoint REST interno/manual (POST /api/v1/orcamentos) — na versão
// final da Fase 4 este caso de uso é disparado por um consumer RabbitMQ/MassTransit do
// comando "GerarOrcamento", publicado pelo OS Service ao consumir o evento
// DiagnosticoFinalizado do Execução Service. A mensageria ainda não está ligada (ver
// PLANO-FASE-4-MICROSSERVICOS.md, seção 8, item 5) — isto fica para um PR de follow-up.
public record GerarOrcamentoInput(Guid IdOrdemServico, List<ItemOrcamentoInput> Itens, decimal ValorTotal);
