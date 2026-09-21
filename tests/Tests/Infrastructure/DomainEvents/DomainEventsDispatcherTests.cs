using Application.Common.Interfaces;
using Domain.Common.Events;
using Domain.Pagamentos.Events;
using FluentAssertions;
using Infrastructure.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Infrastructure.DomainEvents;

public class DomainEventsDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ComHandlerRegistrado_DeveInvocarOHandlerParaOEvento()
    {
        var handlerMock = new Mock<IDomainEventHandler<PagamentoAprovadoDomainEvent>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<PagamentoAprovadoDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped<IDomainEventHandler<PagamentoAprovadoDomainEvent>>(_ => handlerMock.Object);
        var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventsDispatcher(provider);
        var domainEvent = new PagamentoAprovadoDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "mp-1");

        await dispatcher.DispatchAsync([domainEvent]);

        handlerMock.Verify(h => h.Handle(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_SemHandlerRegistrado_NaoDeveLancarException()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventsDispatcher(provider);
        var domainEvent = new PagamentoRecusadoDomainEvent(Guid.NewGuid(), Guid.NewGuid(), "mp-1");

        var acao = async () => await dispatcher.DispatchAsync([domainEvent]);

        await acao.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_ComListaVazia_NaoDeveInvocarNenhumHandler()
    {
        var handlerMock = new Mock<IDomainEventHandler<PagamentoAprovadoDomainEvent>>();

        var services = new ServiceCollection();
        services.AddScoped<IDomainEventHandler<PagamentoAprovadoDomainEvent>>(_ => handlerMock.Object);
        var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventsDispatcher(provider);

        await dispatcher.DispatchAsync([]);

        handlerMock.Verify(h => h.Handle(It.IsAny<PagamentoAprovadoDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
