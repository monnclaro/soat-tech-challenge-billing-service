using Domain.Common.Events;
using FluentAssertions;
using Infrastructure.Database.Helpers;

namespace Tests.Infrastructure.Database.Helpers;

public class NoopDomainEventsDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_DeveConcluirSemFazerNada()
    {
        var dispatcher = new NoopDomainEventsDispatcher();

        var acao = async () => await dispatcher.DispatchAsync(Array.Empty<IDomainEvent>());

        await acao.Should().NotThrowAsync();
    }
}
