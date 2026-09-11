using Domain.Common.Exceptions;
using Domain.Orcamentos;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Itens;
using FluentAssertions;

namespace Tests.Domain.Orcamentos;

public class OrcamentoTests
{
    private static List<OrcamentoItem> ItensValidos() =>
    [
        new OrcamentoItem(Guid.NewGuid(), "Troca de óleo", 150m, TipoItemOrcamento.Servico),
        new OrcamentoItem(Guid.NewGuid(), "Filtro de óleo", 50m, TipoItemOrcamento.Produto)
    ];

    [Fact]
    public void Gerar_ComItensValidos_DeveCriarOrcamentoPendenteComValorTotalSomado()
    {
        var idOrdemServico = Guid.NewGuid();
        var orcamento = new Orcamento();

        orcamento.Gerar(idOrdemServico, ItensValidos());

        orcamento.Id.Should().NotBeEmpty();
        orcamento.IdOrdemServico.Should().Be(idOrdemServico);
        orcamento.Status.Should().Be(StatusOrcamento.Pendente);
        orcamento.ValorTotal.Should().Be(200m);
        orcamento.Itens.Should().HaveCount(2);
        orcamento.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "OrcamentoGeradoDomainEvent");
    }

    [Fact]
    public void Gerar_SemItens_DeveLancarDomainException()
    {
        var orcamento = new Orcamento();

        var acao = () => orcamento.Gerar(Guid.NewGuid(), []);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Aprovar_QuandoPendente_DeveAlterarStatusParaAprovado()
    {
        var orcamento = new Orcamento();
        orcamento.Gerar(Guid.NewGuid(), ItensValidos());
        orcamento.ClearDomainEvents();

        orcamento.Aprovar();

        orcamento.Status.Should().Be(StatusOrcamento.Aprovado);
        orcamento.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Aprovar_QuandoNaoPendente_DeveLancarDomainException()
    {
        var orcamento = new Orcamento();
        orcamento.Gerar(Guid.NewGuid(), ItensValidos());
        orcamento.Aprovar();

        var acao = () => orcamento.Aprovar();

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reprovar_QuandoPendente_DeveAlterarStatusParaReprovado()
    {
        var orcamento = new Orcamento();
        orcamento.Gerar(Guid.NewGuid(), ItensValidos());

        orcamento.Reprovar();

        orcamento.Status.Should().Be(StatusOrcamento.Reprovado);
    }

    [Fact]
    public void Reprovar_QuandoJaAprovado_DeveLancarDomainException()
    {
        var orcamento = new Orcamento();
        orcamento.Gerar(Guid.NewGuid(), ItensValidos());
        orcamento.Aprovar();

        var acao = () => orcamento.Reprovar();

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expirar_QuandoPendente_DeveAlterarStatusParaExpirado()
    {
        var orcamento = new Orcamento();
        orcamento.Gerar(Guid.NewGuid(), ItensValidos());

        orcamento.Expirar();

        orcamento.Status.Should().Be(StatusOrcamento.Expirado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void OrcamentoItem_ComValorInvalido_DeveLancarDomainException(decimal valor)
    {
        var acao = () => new OrcamentoItem(Guid.NewGuid(), "Item", valor, TipoItemOrcamento.Servico);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void OrcamentoItem_SemNome_DeveLancarDomainException()
    {
        var acao = () => new OrcamentoItem(Guid.NewGuid(), "   ", 10m, TipoItemOrcamento.Servico);

        acao.Should().Throw<DomainException>();
    }
}
