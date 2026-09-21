namespace Soat.Contracts.Saga;

// Contratos de mensageria da saga (Fase 4), compartilhados "por convenção" entre
// os 3 microsserviços (soat-os-service / soat-billing-service /
// soat-execucao-service) — cada repo mantém sua própria cópia idêntica destes
// tipos em vez de depender de um pacote NuGet publicado, para evitar a
// complexidade de autenticação de um feed privado neste estágio do projeto.
// São DTOs puros, sem lógica — o único "código compartilhado" real entre os
// serviços. Vivem na
// camada Application (não Infrastructure) porque os handlers de orquestração
// da saga (Application) precisam referenciá-los sem violar a regra
// "Application não depende de Infrastructure".

#region Marcadores (ISagaCommand / ISagaEvent)

// Marca explicitamente cada mensagem como comando (imperativo, "faça algo" —
// endereçado a um serviço específico, mesmo sendo tecnicamente roteado via
// Publish nesta topologia simples) ou evento (particípio passado, "algo já
// aconteceu" — qualquer serviço interessado pode reagir). Não tem efeito em
// runtime (MassTransit não exige isso) — só documenta a intenção de cada
// mensagem no próprio tipo, em vez de depender só do comentário acima dele.
public interface ISagaCommand;
public interface ISagaEvent;

#endregion

#region Tipos compartilhados (DTOs)

public record ItemServicoDiagnosticado(Guid IdServico, string NomeServico, decimal Valor);
public record ItemProdutoDiagnosticado(Guid IdProduto, string NomeProduto, decimal ValorUnitario, decimal Quantidade);

#endregion

#region Comandos — OS Service -> Execução Service

public record IniciarDiagnostico(Guid IdOrdemServico, Guid IdCliente, Guid IdVeiculo) : ISagaCommand;
public record IniciarExecucao(Guid IdOrdemServico) : ISagaCommand;

#endregion

#region Comandos — OS Service -> Billing Service

public record GerarOrcamento(
    Guid IdOrdemServico,
    IReadOnlyList<ItemServicoDiagnosticado> Servicos,
    IReadOnlyList<ItemProdutoDiagnosticado> Produtos,
    decimal ValorTotal) : ISagaCommand;

#endregion

#region Eventos — Execução Service -> OS Service

public record DiagnosticoFinalizado(
    Guid IdOrdemServico,
    IReadOnlyList<ItemServicoDiagnosticado> Servicos,
    IReadOnlyList<ItemProdutoDiagnosticado> Produtos) : ISagaEvent;

public record DiagnosticoFalhou(Guid IdOrdemServico, string Motivo) : ISagaEvent;
public record ExecucaoFinalizada(Guid IdOrdemServico) : ISagaEvent;

// Compensação: falha durante a execução (ex.: peça indisponível), depois do
// diagnóstico já ter sido finalizado — diferente de DiagnosticoFalhou, que
// cobre a falha ainda na fase de diagnóstico.
public record ExecucaoFalhou(Guid IdOrdemServico, string Motivo) : ISagaEvent;

#endregion

#region Eventos — Billing Service -> OS Service

public record OrcamentoGerado(Guid IdOrdemServico, Guid IdOrcamento, decimal ValorTotal, string LinkPagamento) : ISagaEvent;

// Compensação: falha ao gerar o orçamento (ex.: Mercado Pago fora do ar) — diferente
// de PagamentoRecusado, que só existe depois de um orçamento já ter sido gerado.
public record OrcamentoFalhou(Guid IdOrdemServico, string Motivo) : ISagaEvent;

public record PagamentoAprovado(Guid IdOrdemServico, Guid IdPagamento) : ISagaEvent;
public record PagamentoRecusado(Guid IdOrdemServico, Guid IdPagamento, string Motivo) : ISagaEvent;

#endregion
