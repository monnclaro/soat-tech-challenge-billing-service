using Application.Orcamentos.Gateways;
using global::MercadoPago.Client.Payment;
using global::MercadoPago.Client.Preference;
using global::MercadoPago.Config;
using Microsoft.Extensions.Options;

namespace Infrastructure.MercadoPago;

// Implementação real do fluxo Checkout Pro via SDK oficial (pacote NuGet "mercadopago-sdk").
// AccessToken de sandbox/teste — ver README para instruções de como gerar credenciais de
// teste no painel do Mercado Pago. Nenhuma chamada aqui é mockada: CriarPreferencia chama
// de fato o endpoint /checkout/preferences, e BuscarPagamentoPorId chama /v1/payments/{id}.
public class MercadoPagoGateway : IMercadoPagoGateway
{
    public MercadoPagoGateway(IOptions<MercadoPagoSettings> settings)
    {
        // MercadoPagoConfig.AccessToken é estático (design do próprio SDK oficial) — seguro
        // aqui porque este processo só atende a uma conta/credencial do Mercado Pago.
        MercadoPagoConfig.AccessToken = settings.Value.AccessToken;
    }

    public async Task<PreferenciaCriadaOutput> CriarPreferencia(CriarPreferenciaInput input, CancellationToken ct = default)
    {
        var request = new PreferenceRequest
        {
            ExternalReference = input.IdOrdemServico.ToString(),
            Items = input.Itens
                .Select(i => new PreferenceItemRequest
                {
                    Title = i.Nome,
                    Quantity = 1,
                    CurrencyId = "BRL",
                    UnitPrice = i.Valor
                })
                .ToList(),
            NotificationUrl = input.NotificationUrl
        };

        var client = new PreferenceClient();
        var preference = await client.CreateAsync(request, cancellationToken: ct);

        return new PreferenciaCriadaOutput(preference.Id, preference.InitPoint);
    }

    public async Task<PagamentoConsultadoOutput?> BuscarPagamentoPorId(string paymentId, CancellationToken ct = default)
    {
        if (!long.TryParse(paymentId, out var id))
        {
            return null;
        }

        var client = new PaymentClient();
        var payment = await client.GetAsync(id, cancellationToken: ct);

        if (payment is null || string.IsNullOrWhiteSpace(payment.ExternalReference))
        {
            return null;
        }

        if (!Guid.TryParse(payment.ExternalReference, out var idOrdemServico))
        {
            return null;
        }

        return new PagamentoConsultadoOutput(
            payment.Id.ToString()!,
            payment.Status ?? "unknown",
            idOrdemServico,
            payment.TransactionAmount ?? 0);
    }
}
