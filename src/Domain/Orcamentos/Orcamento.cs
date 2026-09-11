using Domain.Common;
using Domain.Orcamentos.Enums;
using Domain.Orcamentos.Events;
using Domain.Orcamentos.Itens;
using DomainException = Domain.Common.Exceptions.DomainException;

namespace Domain.Orcamentos;

// Disparado ao consumir (via REST manual por enquanto — ver GerarOrcamentoUseCase) o que
// seria o comando GerarOrcamento originado do evento DiagnosticoFinalizado (Execução
// Service). Guarda um snapshot dos itens identificados e o resultado da negociação do
// pagamento (Pagamento, 1:1) via Mercado Pago Checkout Pro.
public class Orcamento : Entity
{
    public Guid Id { get; private set; }
    public Guid IdOrdemServico { get; private set; }
    public StatusOrcamento Status { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public List<OrcamentoItem> Itens { get; init; } = [];

    public void Gerar(Guid idOrdemServico, List<OrcamentoItem> itens)
    {
        if (itens is null || itens.Count == 0)
        {
            throw new DomainException("O orçamento precisa de ao menos um item.");
        }

        Id = Guid.NewGuid();
        IdOrdemServico = idOrdemServico;
        Itens.AddRange(itens);
        ValorTotal = itens.Sum(i => i.Valor);
        Status = StatusOrcamento.Pendente;
        DataCriacao = DateTime.UtcNow;

        Raise(new OrcamentoGeradoDomainEvent(Id, IdOrdemServico, ValorTotal));
    }

    // Consumido a partir da confirmação de pagamento aprovado (webhook do Mercado Pago).
    public void Aprovar()
    {
        if (Status != StatusOrcamento.Pendente)
        {
            throw new DomainException("Só é possível aprovar um orçamento pendente.");
        }

        Status = StatusOrcamento.Aprovado;

        Raise(new OrcamentoStatusAlteradoDomainEvent(Id, IdOrdemServico, Status));
    }

    // Consumido a partir da confirmação de pagamento recusado (webhook do Mercado Pago) —
    // caminho de compensação da saga (ver PLANO-FASE-4-MICROSSERVICOS.md).
    public void Reprovar()
    {
        if (Status != StatusOrcamento.Pendente)
        {
            throw new DomainException("Só é possível reprovar um orçamento pendente.");
        }

        Status = StatusOrcamento.Reprovado;

        Raise(new OrcamentoStatusAlteradoDomainEvent(Id, IdOrdemServico, Status));
    }

    public void Expirar()
    {
        if (Status != StatusOrcamento.Pendente)
        {
            throw new DomainException("Só é possível expirar um orçamento pendente.");
        }

        Status = StatusOrcamento.Expirado;

        Raise(new OrcamentoStatusAlteradoDomainEvent(Id, IdOrdemServico, Status));
    }
}
