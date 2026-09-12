using Domain.Common.Events;
using Domain.Pagamentos;
using Domain.Pagamentos.Enums;
using FluentAssertions;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Gateways.Pagamentos;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Infrastructure.Gateways;

public class PagamentoGatewayTests
{
    private readonly Mock<IDomainEventsDispatcher> _dispatcher = new();

    private BillingServiceDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BillingServiceDbContext(options, _dispatcher.Object);
    }

    private static Pagamento CriarPagamento(string preferenceId = "preference-123")
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), preferenceId, 100m);
        return pagamento;
    }

    [Fact]
    public async Task Salvar_DevePersistirOPagamento()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);
        var pagamento = CriarPagamento();

        await gateway.Salvar(pagamento);

        var persistido = await db.Pagamento.FirstOrDefaultAsync(p => p.Id == pagamento.Id);
        persistido.Should().NotBeNull();
    }

    [Fact]
    public async Task BuscarPorId_QuandoExiste_DeveRetornarOPagamento()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);
        var pagamento = CriarPagamento();
        await gateway.Salvar(pagamento);

        var encontrado = await gateway.BuscarPorId(pagamento.Id);

        encontrado.Should().NotBeNull();
    }

    [Fact]
    public async Task BuscarPorId_QuandoNaoExiste_DeveRetornarNull()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);

        var encontrado = await gateway.BuscarPorId(Guid.NewGuid());

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task BuscarPorIdOrcamento_QuandoExiste_DeveRetornarOPagamento()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);
        var pagamento = CriarPagamento();
        await gateway.Salvar(pagamento);

        var encontrado = await gateway.BuscarPorIdOrcamento(pagamento.IdOrcamento);

        encontrado.Should().NotBeNull();
    }

    [Fact]
    public async Task BuscarPorIdOrcamento_QuandoNaoExiste_DeveRetornarNull()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);

        var encontrado = await gateway.BuscarPorIdOrcamento(Guid.NewGuid());

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task BuscarPorPreferenceId_QuandoExiste_DeveRetornarOPagamento()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);
        var pagamento = CriarPagamento("preference-xyz");
        await gateway.Salvar(pagamento);

        var encontrado = await gateway.BuscarPorPreferenceId("preference-xyz");

        encontrado.Should().NotBeNull();
    }

    [Fact]
    public async Task BuscarPorPreferenceId_QuandoNaoExiste_DeveRetornarNull()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);

        var encontrado = await gateway.BuscarPorPreferenceId("nao-existe");

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task Atualizar_DevePersistirAsAlteracoesEDispararOsDomainEvents()
    {
        await using var db = CriarDbContext();
        var gateway = new PagamentoGateway(db);
        var pagamento = CriarPagamento();
        await gateway.Salvar(pagamento);

        pagamento.Aprovar("mp-1");
        await gateway.Atualizar(pagamento);

        var recarregado = await gateway.BuscarPorId(pagamento.Id);
        recarregado!.Status.Should().Be(StatusPagamento.Aprovado);

        _dispatcher.Verify(d => d.DispatchAsync(
            It.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e.GetType().Name == "PagamentoAprovadoDomainEvent")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
