namespace Domain.Orcamentos.Enums;

// Mesma distinção usada no monolito de origem (OrdemServicoServico/OrdemServicoProduto) —
// aqui os dois tipos convivem numa única lista de itens porque o orçamento não precisa
// diferenciar regras de negócio entre eles, só compor o snapshot de nome/valor recebido
// do evento DiagnosticoFinalizado (Execução Service).
public enum TipoItemOrcamento
{
    Servico = 0,
    Produto = 1
}
