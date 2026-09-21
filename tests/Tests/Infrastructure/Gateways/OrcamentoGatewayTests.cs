using Domain.Common.Events;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Itens;
using FluentAssertions;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Gateways.Orcamentos;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Infrastructure.Gateways;

public class OrcamentoGatewayTests
{
    private readonly Mock<IDomainEventsDispatcher> _dispatcher = new();

    private BillingServiceDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BillingServiceDbContext(options, _dispatcher.Object);
    }

    private static Orcamento CriarOrcamento(Guid idOrdemServico)
    {
        var orcamento = new Orcamento();
        orcamento.Gerar(idOrdemServico, [new OrcamentoItem(Guid.NewGuid(), "Item", 100m, TipoItemOrcamento.Servico)]);
        return orcamento;
    }

    [Fact]
    public async Task Salvar_DevePersistirOOrcamentoEDispararOsDomainEvents()
    {
        await using var db = CriarDbContext();
        var gateway = new OrcamentoGateway(db);
        var orcamento = CriarOrcamento(Guid.NewGuid());

        await gateway.Salvar(orcamento);

        var persistido = await db.Orcamento.FirstOrDefaultAsync(o => o.Id == orcamento.Id);
        persistido.Should().NotBeNull();

        _dispatcher.Verify(d => d.DispatchAsync(
            It.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e.GetType().Name == "OrcamentoGeradoDomainEvent")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuscarPorId_QuandoExiste_DeveRetornarOOrcamentoComItens()
    {
        await using var db = CriarDbContext();
        var gateway = new OrcamentoGateway(db);
        var orcamento = CriarOrcamento(Guid.NewGuid());
        await gateway.Salvar(orcamento);

        var encontrado = await gateway.BuscarPorId(orcamento.Id);

        encontrado.Should().NotBeNull();
        encontrado!.Itens.Should().HaveCount(1);
    }

    [Fact]
    public async Task BuscarPorId_QuandoNaoExiste_DeveRetornarNull()
    {
        await using var db = CriarDbContext();
        var gateway = new OrcamentoGateway(db);

        var encontrado = await gateway.BuscarPorId(Guid.NewGuid());

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task BuscarPorIdOrdemServico_QuandoExiste_DeveRetornarOOrcamento()
    {
        await using var db = CriarDbContext();
        var gateway = new OrcamentoGateway(db);
        var idOrdemServico = Guid.NewGuid();
        var orcamento = CriarOrcamento(idOrdemServico);
        await gateway.Salvar(orcamento);

        var encontrado = await gateway.BuscarPorIdOrdemServico(idOrdemServico);

        encontrado.Should().NotBeNull();
        encontrado!.IdOrdemServico.Should().Be(idOrdemServico);
    }

    [Fact]
    public async Task BuscarPorIdOrdemServico_QuandoNaoExiste_DeveRetornarNull()
    {
        await using var db = CriarDbContext();
        var gateway = new OrcamentoGateway(db);

        var encontrado = await gateway.BuscarPorIdOrdemServico(Guid.NewGuid());

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task Atualizar_DevePersistirAsAlteracoes()
    {
        await using var db = CriarDbContext();
        var gateway = new OrcamentoGateway(db);
        var orcamento = CriarOrcamento(Guid.NewGuid());
        await gateway.Salvar(orcamento);

        orcamento.Aprovar();
        await gateway.Atualizar(orcamento);

        var recarregado = await gateway.BuscarPorId(orcamento.Id);
        recarregado!.Status.Should().Be(StatusOrcamento.Aprovado);
    }
}
