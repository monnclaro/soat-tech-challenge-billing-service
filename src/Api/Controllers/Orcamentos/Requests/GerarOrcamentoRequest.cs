using System.Text.Json.Serialization;

namespace Api.Controllers.Orcamentos.Requests;

// Endpoint interno/manual (não é chamado por clientes finais): dispara a geração de um
// orçamento a partir dos itens identificados no diagnóstico. Na versão final da Fase 4
// isto deixa de ser um endpoint REST e passa a ser um consumer RabbitMQ/MassTransit do
// comando "GerarOrcamento", publicado pelo OS Service ao consumir o evento
// DiagnosticoFinalizado do Execução Service — ver PLANO-FASE-4-MICROSSERVICOS.md.
public record GerarOrcamentoRequest(
    [property: JsonPropertyName("idOrdemServico")] Guid IdOrdemServico,
    [property: JsonPropertyName("itens")] List<ItemOrcamentoRequest> Itens,
    [property: JsonPropertyName("valorTotal")] decimal ValorTotal);

public record ItemOrcamentoRequest(
    [property: JsonPropertyName("idItemOrigem")] Guid IdItemOrigem,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("valor")] decimal Valor,
    [property: JsonPropertyName("tipo")] string Tipo);
