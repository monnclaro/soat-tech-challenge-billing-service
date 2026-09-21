using Application.Common.Interfaces;
using Application.Orcamentos.Gateways;
using Domain.Orcamentos;
using Domain.Orcamentos.Gateways;
using Domain.Orcamentos.Itens;
using Domain.Pagamentos;
using Domain.Pagamentos.Gateways;

namespace Application.Orcamentos.UseCases.GerarOrcamento;

public class GerarOrcamentoUseCase : IUseCase
{
    private readonly IOrcamentoGateway _orcamentoGateway;
    private readonly IPagamentoGateway _pagamentoGateway;
    private readonly IMercadoPagoGateway _mercadoPagoGateway;
    private readonly ISagaEventPublisher _sagaEventPublisher;
    private readonly IGerarOrcamentoOutputPort _outputPort;

    public GerarOrcamentoUseCase(
        IOrcamentoGateway orcamentoGateway,
        IPagamentoGateway pagamentoGateway,
        IMercadoPagoGateway mercadoPagoGateway,
        ISagaEventPublisher sagaEventPublisher,
        IGerarOrcamentoOutputPort outputPort)
    {
        _orcamentoGateway = orcamentoGateway;
        _pagamentoGateway = pagamentoGateway;
        _mercadoPagoGateway = mercadoPagoGateway;
        _sagaEventPublisher = sagaEventPublisher;
        _outputPort = outputPort;
    }

    public async Task Execute(GerarOrcamentoInput input, CancellationToken ct = default)
    {
        var orcamento = await _orcamentoGateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (orcamento is not null)
        {
            // O Orcamento já existe — mas só é de fato um duplicado (reentrega da
            // mensagem já totalmente processada) se o Pagamento também existe. Se não
            // existe, uma tentativa anterior falhou entre salvar o Orcamento e concluir a
            // integração com o Mercado Pago (ex.: a própria chamada falhou) — em vez de
            // reprocessar como duplicado (e nunca gerar o Pagamento), retoma a partir daqui
            // reaproveitando o Orcamento já persistido.
            var pagamentoExistente = await _pagamentoGateway.BuscarPorIdOrcamento(orcamento.Id, ct);
            if (pagamentoExistente is not null)
            {
                _outputPort.OrcamentoJaExiste($"Já existe um orçamento para a ordem de serviço '{input.IdOrdemServico}'.");
                return;
            }
        }
        else
        {
            var itens = input.Itens
                .Select(i => new OrcamentoItem(i.IdItemOrigem, i.NomeItem, i.Valor, i.Tipo))
                .ToList();

            orcamento = new Orcamento();
            orcamento.Gerar(input.IdOrdemServico, itens);

            if (orcamento.ValorTotal != input.ValorTotal)
            {
                _outputPort.ValorDivergente(
                    $"O valor total informado ({input.ValorTotal:0.00}) não confere com a soma dos itens ({orcamento.ValorTotal:0.00}).");
                return;
            }

            await _orcamentoGateway.Salvar(orcamento, ct);
        }

        PreferenciaCriadaOutput preferencia;
        try
        {
            preferencia = await _mercadoPagoGateway.CriarPreferencia(
                new CriarPreferenciaInput(
                    input.IdOrdemServico,
                    orcamento.Itens.Select(i => new ItemPreferenciaInput(i.NomeItem, i.Valor)).ToList()),
                ct);
        }
        catch (Exception ex)
        {
            // Fronteira com o Mercado Pago (rede, API fora do ar, credenciais inválidas) —
            // nunca deixa a mensagem morrer silenciosamente sem sinalizar a saga: publica a
            // falha para o OS Service cancelar a OS (compensação), em vez de deixar a
            // mensagem cair na fila de erro do RabbitMQ sem nenhuma reação. O Orcamento já
            // persistido continua ali, sem Pagamento — se o problema for corrigido, reprocessar
            // manualmente a mesma mensagem retoma daqui (ver o branch de retomada acima).
            await _sagaEventPublisher.PublicarOrcamentoFalhou(input.IdOrdemServico, ex.Message, ct);
            _outputPort.Falha($"Falha ao gerar a preferência de pagamento no Mercado Pago: {ex.Message}");
            return;
        }

        var pagamento = new Pagamento();
        pagamento.Criar(orcamento.Id, preferencia.PreferenceId, orcamento.ValorTotal);

        await _pagamentoGateway.Salvar(pagamento, ct);

        // Publicado aqui (não via domain event do agregado Orcamento) porque o link de
        // pagamento só existe depois da chamada ao Mercado Pago — o agregado de domínio não
        // conhece esse dado no momento em que OrcamentoGeradoDomainEvent é levantado.
        await _sagaEventPublisher.PublicarOrcamentoGerado(
            input.IdOrdemServico, orcamento.Id, orcamento.ValorTotal, preferencia.InitPoint, ct);

        _outputPort.Ok(new OrcamentoOutput(
            orcamento.Id,
            orcamento.IdOrdemServico,
            orcamento.Itens.Select(i => new ItemOrcamentoOutput(i.IdItemOrigem, i.NomeItem, i.Valor, i.Tipo.ToString())).ToList(),
            orcamento.ValorTotal,
            orcamento.Status.ToString(),
            orcamento.DataCriacao,
            preferencia.InitPoint));
    }
}
