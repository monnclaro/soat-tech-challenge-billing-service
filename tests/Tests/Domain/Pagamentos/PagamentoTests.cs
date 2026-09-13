using Domain.Common.Exceptions;
using Domain.Pagamentos;
using Domain.Pagamentos.Enums;
using FluentAssertions;

namespace Tests.Domain.Pagamentos;

public class PagamentoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveIniciarPendente()
    {
        var idOrcamento = Guid.NewGuid();
        var pagamento = new Pagamento();

        pagamento.Criar(idOrcamento, "preference-123", 200m);

        pagamento.Id.Should().NotBeEmpty();
        pagamento.IdOrcamento.Should().Be(idOrcamento);
        pagamento.PreferenceId.Should().Be("preference-123");
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.Valor.Should().Be(200m);
        pagamento.PaymentId.Should().BeNull();
    }

    [Fact]
    public void Criar_ComValorInvalido_DeveLancarDomainException()
    {
        var pagamento = new Pagamento();

        var acao = () => pagamento.Criar(Guid.NewGuid(), "preference-123", 0m);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_SemPreferenceId_DeveLancarDomainException()
    {
        var pagamento = new Pagamento();

        var acao = () => pagamento.Criar(Guid.NewGuid(), "", 100m);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Aprovar_QuandoPendente_DeveAlterarStatusEGuardarPaymentId()
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), "preference-123", 200m);

        pagamento.Aprovar("payment-456");

        pagamento.Status.Should().Be(StatusPagamento.Aprovado);
        pagamento.PaymentId.Should().Be("payment-456");
        pagamento.DataAtualizacao.Should().NotBeNull();
        pagamento.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PagamentoAprovadoDomainEvent");
    }

    [Fact]
    public void Recusar_QuandoPendente_DeveAlterarStatusEGuardarPaymentId()
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), "preference-123", 200m);

        pagamento.Recusar("payment-456");

        pagamento.Status.Should().Be(StatusPagamento.Recusado);
        pagamento.PaymentId.Should().Be("payment-456");
        pagamento.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PagamentoRecusadoDomainEvent");
    }

    [Fact]
    public void Aprovar_QuandoJaAprovado_DeveLancarDomainException()
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), "preference-123", 200m);
        pagamento.Aprovar("payment-456");

        var acao = () => pagamento.Aprovar("payment-789");

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Recusar_QuandoJaRecusado_DeveLancarDomainException()
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), "preference-123", 200m);
        pagamento.Recusar("payment-456");

        var acao = () => pagamento.Recusar("payment-789");

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Aprovar_SemPaymentId_DeveLancarDomainException()
    {
        var pagamento = new Pagamento();
        pagamento.Criar(Guid.NewGuid(), "preference-123", 200m);

        var acao = () => pagamento.Aprovar("");

        acao.Should().Throw<DomainException>();
    }
}
