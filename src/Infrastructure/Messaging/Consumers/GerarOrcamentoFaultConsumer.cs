using Application.Common.Interfaces;
using MassTransit;
using Soat.Contracts.Saga;

namespace Infrastructure.Messaging.Consumers;

// Reage ao esgotamento das retentativas automáticas do MassTransit (UseMessageRetry,
// 3x/5s, configurado globalmente em Infrastructure.DependencyInjection) ao processar
// GerarOrcamento. Só agora — depois que o retry não resolveu sozinho — a saga é avisada
// pra compensar (cancelar a OS). Nem toda falha exige compensação: se a causa for
// transitória (rede, Mercado Pago momentaneamente fora do ar), uma das retentativas
// resolve sozinha e este consumer nunca chega a ser acionado.
public class GerarOrcamentoFaultConsumer : IConsumer<Fault<GerarOrcamento>>
{
    private readonly ISagaEventPublisher _sagaEventPublisher;

    public GerarOrcamentoFaultConsumer(ISagaEventPublisher sagaEventPublisher)
    {
        _sagaEventPublisher = sagaEventPublisher;
    }

    public async Task Consume(ConsumeContext<Fault<GerarOrcamento>> context)
    {
        var idOrdemServico = context.Message.Message.IdOrdemServico;
        var motivo = context.Message.Exceptions.FirstOrDefault()?.Message
            ?? "Falha ao gerar a preferência de pagamento no Mercado Pago.";

        await _sagaEventPublisher.PublicarOrcamentoFalhou(idOrdemServico, motivo, context.CancellationToken);
    }
}
