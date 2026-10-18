using Application.Orcamentos.Gateways;

namespace Infrastructure.MercadoPago;

// Substitui MercadoPagoGateway quando MercadoPago:UseFake=true — usado só pelo teste de
// integração do OS Service (Testcontainers), que sobe este serviço real mas não pode
// depender da API de verdade do Mercado Pago (sem credencial de sandbox, e pagamento real
// não dá pra automatizar num teste repetível). Não bate em rede nenhuma.
//
// Nome termina em "Fake", não em "Gateway" — de propósito: o scan automático de DI
// (Infrastructure/DependencyInjection.cs) registra toda classe terminada em "Gateway", e
// esta classe é registrada manualmente (condicional a UseFake), não pelo scan.
public class MercadoPagoGatewayFake : IMercadoPagoGateway
{
    public Task<PreferenciaCriadaOutput> CriarPreferencia(CriarPreferenciaInput input, CancellationToken ct = default) =>
        Task.FromResult(new PreferenciaCriadaOutput(
            $"fake-preference-{input.IdOrdemServico}",
            $"https://fake-mercadopago.invalid/checkout/{input.IdOrdemServico}"));

    // O webhook real correlaciona paymentId -> IdOrdemServico via o external_reference que
    // o Mercado Pago ecoa de volta (setado como o IdOrdemServico na criação da preferência,
    // ver CriarPreferencia acima). Este fake reproduz isso de forma direta: espera que quem
    // dispara o webhook de teste use o próprio IdOrdemServico (em texto) como paymentId.
    public Task<PagamentoConsultadoOutput?> BuscarPagamentoPorId(string paymentId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(paymentId, out var idOrdemServico))
        {
            return Task.FromResult<PagamentoConsultadoOutput?>(null);
        }

        // Valor não é conferido por RegistrarWebhookPagamentoUseCase (só Status/IdOrdemServico
        // importam pra decidir Aprovar/Recusar) — 0m é suficiente.
        return Task.FromResult<PagamentoConsultadoOutput?>(new PagamentoConsultadoOutput(paymentId, "approved", idOrdemServico, 0m));
    }
}
