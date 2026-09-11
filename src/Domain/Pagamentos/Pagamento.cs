using Domain.Common;
using Domain.Pagamentos.Enums;
using Domain.Pagamentos.Events;
using DomainException = Domain.Common.Exceptions.DomainException;

namespace Domain.Pagamentos;

// Representa a cobrança criada no Mercado Pago (Checkout Pro) para um Orcamento. Nasce
// junto com o Orcamento (ver GerarOrcamentoUseCase) já com a preferência de pagamento
// criada, e é atualizado quando o webhook de notificação (RegistrarWebhookPagamentoUseCase)
// confirma o resultado da cobrança.
public class Pagamento : Entity
{
    public Guid Id { get; private set; }
    public Guid IdOrcamento { get; private set; }
    public string PreferenceId { get; private set; } = null!;
    public string? PaymentId { get; private set; }
    public StatusPagamento Status { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataAtualizacao { get; private set; }

    public void Criar(Guid idOrcamento, string preferenceId, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(preferenceId))
        {
            throw new DomainException("O identificador da preferência de pagamento é obrigatório.");
        }

        if (valor <= 0)
        {
            throw new DomainException("O valor do pagamento deve ser maior que zero.");
        }

        Id = Guid.NewGuid();
        IdOrcamento = idOrcamento;
        PreferenceId = preferenceId;
        Status = StatusPagamento.Pendente;
        Valor = valor;
        DataCriacao = DateTime.UtcNow;
    }

    public void Aprovar(string paymentId)
    {
        if (Status != StatusPagamento.Pendente)
        {
            throw new DomainException("Só é possível aprovar um pagamento pendente.");
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            throw new DomainException("O identificador do pagamento no Mercado Pago é obrigatório.");
        }

        PaymentId = paymentId;
        Status = StatusPagamento.Aprovado;
        DataAtualizacao = DateTime.UtcNow;

        Raise(new PagamentoAprovadoDomainEvent(Id, IdOrcamento, paymentId));
    }

    public void Recusar(string paymentId)
    {
        if (Status != StatusPagamento.Pendente)
        {
            throw new DomainException("Só é possível recusar um pagamento pendente.");
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            throw new DomainException("O identificador do pagamento no Mercado Pago é obrigatório.");
        }

        PaymentId = paymentId;
        Status = StatusPagamento.Recusado;
        DataAtualizacao = DateTime.UtcNow;

        Raise(new PagamentoRecusadoDomainEvent(Id, IdOrcamento, paymentId));
    }
}
