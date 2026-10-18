using Application.Orcamentos.Gateways;
using FluentAssertions;
using Infrastructure.MercadoPago;

namespace Tests.Infrastructure.MercadoPago;

public class MercadoPagoGatewayFakeTests
{
    private readonly MercadoPagoGatewayFake _gateway = new();

    [Fact]
    public async Task CriarPreferencia_DevolvePreferenciaDeterministicaSemChamarRedeNenhuma()
    {
        var idOrdemServico = Guid.NewGuid();
        var input = new CriarPreferenciaInput(idOrdemServico, [new ItemPreferenciaInput("Troca de óleo", 120m)]);

        var resultado = await _gateway.CriarPreferencia(input);

        resultado.PreferenceId.Should().Contain(idOrdemServico.ToString());
        resultado.InitPoint.Should().Contain(idOrdemServico.ToString());
    }

    [Fact]
    public async Task BuscarPagamentoPorId_ComGuidValido_DevolveAprovadoParaEsseIdOrdemServico()
    {
        var idOrdemServico = Guid.NewGuid();

        var resultado = await _gateway.BuscarPagamentoPorId(idOrdemServico.ToString());

        resultado.Should().NotBeNull();
        resultado!.Status.Should().Be("approved");
        resultado.IdOrdemServico.Should().Be(idOrdemServico);
    }

    [Fact]
    public async Task BuscarPagamentoPorId_ComIdQueNaoEhGuid_DevolveNulo()
    {
        var resultado = await _gateway.BuscarPagamentoPorId("nao-e-um-guid");

        resultado.Should().BeNull();
    }
}
