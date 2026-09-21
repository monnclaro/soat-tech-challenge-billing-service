using System.Text.Json.Serialization;

namespace Api.Controllers.Orcamentos.Requests;

// Endpoint interno/manual (não é chamado por clientes finais): dispara a geração de um
// orçamento a partir dos itens identificados no diagnóstico. Mantido para
// depuração/teste manual — em produção, o mesmo fluxo é disparado pelo consumer
// RabbitMQ/MassTransit do comando "GerarOrcamento" (publicado pelo OS Service ao
// consumir o evento DiagnosticoFinalizado do Execução Service).
public record GerarOrcamentoRequest(
    [property: JsonPropertyName("idOrdemServico"), JsonRequired] Guid IdOrdemServico,
    [property: JsonPropertyName("itens")] List<ItemOrcamentoRequest> Itens,
    [property: JsonPropertyName("valorTotal"), JsonRequired] decimal ValorTotal);

public record ItemOrcamentoRequest(
    [property: JsonPropertyName("idItemOrigem")] Guid IdItemOrigem,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("valor")] decimal Valor,
    [property: JsonPropertyName("tipo")] string Tipo);
