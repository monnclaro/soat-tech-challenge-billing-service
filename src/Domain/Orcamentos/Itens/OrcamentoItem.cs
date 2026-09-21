using Domain.Common;
using Domain.Orcamentos.Enums;
using DomainException = Domain.Common.Exceptions.DomainException;

namespace Domain.Orcamentos.Itens;

// Snapshot de um serviço/produto identificado no diagnóstico (nome/valor no momento em
// que o orçamento foi gerado) — mesmo padrão de "congelar" dados já usado no monolito de
// origem em OrdemServicoServico/OrdemServicoProduto. Não guarda o id do próprio orçamento
// como propriedade de domínio: o vínculo é resolvido via chave estrangeira "sombra" no EF
// Core (ver OrcamentoItemConfiguration), porque o Id do Orcamento só é conhecido depois que
// a lista de itens já foi construída (ver Orcamento.Gerar).
public class OrcamentoItem : Entity
{
    public Guid Id { get; private set; }
    public Guid IdItemOrigem { get; private set; }
    public string NomeItem { get; private set; } = null!;
    public decimal Valor { get; private set; }
    public TipoItemOrcamento Tipo { get; private set; }

    public OrcamentoItem(Guid idItemOrigem, string nomeItem, decimal valor, TipoItemOrcamento tipo)
    {
        if (string.IsNullOrWhiteSpace(nomeItem))
            throw new DomainException("O nome do item do orçamento é obrigatório.");

        if (valor <= 0)
            throw new DomainException("O valor do item do orçamento deve ser maior que zero.");

        Id = Guid.NewGuid();
        IdItemOrigem = idItemOrigem;
        NomeItem = nomeItem;
        Valor = valor;
        Tipo = tipo;
    }
}
