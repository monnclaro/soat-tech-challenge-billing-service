namespace Soat.Contracts.Saga;

// Contratos de mensageria da saga (Fase 4), compartilhados "por convenção" entre
// os 3 microsserviços (soat-os-service / soat-billing-service /
// soat-execucao-service) — cada repo mantém sua própria cópia idêntica destes
// tipos em vez de depender de um pacote NuGet publicado, para evitar a
// complexidade de autenticação de um feed privado neste estágio do projeto
// (decisão documentada no PLANO-FASE-4-MICROSSERVICOS.md). São DTOs puros, sem
// lógica — o único "código compartilhado" real entre os serviços. Vivem na
// camada Application (não Infrastructure) porque os handlers de orquestração
// da saga (Application) precisam referenciá-los sem violar a regra
// "Application não depende de Infrastructure".
//
// Convenção: comandos (imperativo, endereçados a um serviço específico via
// Send/Publish) descrevem uma ação a executar; eventos (particípio passado)
// descrevem um fato já ocorrido que qualquer serviço pode reagir.

public record ItemServicoDiagnosticado(Guid IdServico, string NomeServico, decimal Valor);
public record ItemProdutoDiagnosticado(Guid IdProduto, string NomeProduto, decimal ValorUnitario, decimal Quantidade);

// Comandos — OS Service -> Execução Service
public record IniciarDiagnostico(Guid IdOrdemServico, Guid IdCliente, Guid IdVeiculo);
public record IniciarExecucao(Guid IdOrdemServico);

// Comandos — OS Service -> Billing Service
public record GerarOrcamento(
    Guid IdOrdemServico,
    IReadOnlyList<ItemServicoDiagnosticado> Servicos,
    IReadOnlyList<ItemProdutoDiagnosticado> Produtos,
    decimal ValorTotal);

// Eventos — Execução Service -> OS Service
public record DiagnosticoFinalizado(
    Guid IdOrdemServico,
    IReadOnlyList<ItemServicoDiagnosticado> Servicos,
    IReadOnlyList<ItemProdutoDiagnosticado> Produtos);

public record DiagnosticoFalhou(Guid IdOrdemServico, string Motivo);
public record ExecucaoFinalizada(Guid IdOrdemServico);

// Eventos — Billing Service -> OS Service
public record OrcamentoGerado(Guid IdOrdemServico, Guid IdOrcamento, decimal ValorTotal, string LinkPagamento);
public record PagamentoAprovado(Guid IdOrdemServico, Guid IdPagamento);
public record PagamentoRecusado(Guid IdOrdemServico, Guid IdPagamento, string Motivo);
