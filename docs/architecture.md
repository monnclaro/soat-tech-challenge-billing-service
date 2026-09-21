# Arquitetura — Billing Service

Documento complementar ao [README](../README.md), com mais profundidade sobre camadas,
modelo de domínio e a integração com o Mercado Pago. Justificativa do desenho da saga
como um todo (por que a orquestração vive no OS Service): [ADR 0001 no repositório do
OS Service](https://github.com/monnclaro/soat-tech-challenge-os-service/blob/main/docs/adr/0001-saga-orquestrada-sem-state-machine-separado.md).

## Camadas (Clean Architecture)

```mermaid
graph TD
    Api["Api<br/>Controllers · Presenters · Middlewares"]
    App["Application<br/>UseCases · Ports (persistência + Mercado Pago)"]
    Dom["Domain<br/>Orcamento, Pagamento · Domain Events"]
    Infra["Infrastructure<br/>EF Core/Postgres · SDK Mercado Pago · MassTransit/RabbitMQ · JWT"]
    SK["SharedKernel<br/>Entity, IDomainEvent, marcadores de DI"]

    Api --> App
    App --> Dom
    Infra -.implementa ports.-> App
    Infra --> Dom
    Api -.-> SK
    App -.-> SK
    Dom -.-> SK
    Infra -.-> SK
```

Regra de dependência garantida por testes de arquitetura (NetArchTest,
`tests/Tests/Camadas`) — `Infrastructure` nunca é referenciada por `Application`/`Domain`.

## Modelo de domínio

```mermaid
classDiagram
    class Orcamento {
        +Guid Id
        +Guid IdOrdemServico
        +List~OrcamentoItem~ Itens
        +decimal ValorTotal
        +StatusOrcamento Status
        +Gerar(idOrdemServico, itens)
        +Aprovar()
        +Reprovar()
    }
    class OrcamentoItem {
        +Guid IdItemOrigem
        +string NomeItem
        +decimal Valor
        +TipoItemOrcamento Tipo
    }
    class Pagamento {
        +Guid Id
        +Guid IdOrcamento
        +string PreferenceId
        +string PaymentId
        +decimal Valor
        +StatusPagamento Status
        +Criar(idOrcamento, preferenceId, valor)
        +Aprovar(paymentId)
        +Recusar(paymentId)
    }

    Orcamento "1" *-- "*" OrcamentoItem : itens (snapshot)
    Orcamento "1" --> "0..1" Pagamento : IdOrcamento
```

`OrcamentoItem` é um **snapshot** (nome/valor/tipo congelados no momento da geração)
— mesmo padrão usado no monolito de origem para `OrdemServicoServico`/`OrdemServicoProduto`:
o orçamento não deve mudar se o catálogo (preço de um serviço, por exemplo) mudar depois.

### Máquinas de estado

```mermaid
stateDiagram-v2
    state "Orcamento" as O {
        [*] --> Pendente: Gerar()
        Pendente --> Aprovado: Aprovar()
        Pendente --> Reprovado: Reprovar()
        Pendente --> Expirado: expiração (não implementado nesta fase)
    }
```

```mermaid
stateDiagram-v2
    state "Pagamento" as P {
        [*] --> Pendente: Criar()
        Pendente --> Aprovado: Aprovar(paymentId)
        Pendente --> Recusado: Recusar(paymentId)
    }
```

## Fluxo — geração de orçamento e pagamento

```mermaid
sequenceDiagram
    participant OS as OS Service
    participant Bill as Billing Service
    participant MP as Mercado Pago

    OS-->>Bill: GerarOrcamento (comando)
    Bill->>Bill: Orcamento.Gerar(itens)

    alt Mercado Pago responde
        Bill->>MP: POST /checkout/preferences
        MP-->>Bill: preference_id + init_point
        Bill->>Bill: Pagamento.Criar(preferenceId)
        Bill-->>OS: OrcamentoGerado (evento)
    else Mercado Pago falha (rede, API fora do ar, credenciais inválidas)
        Note over Bill: Compensação
        Bill-->>OS: OrcamentoFalhou (evento)
    end
```

```mermaid
sequenceDiagram
    actor Cliente
    participant MP as Mercado Pago
    participant Bill as Billing Service
    participant OS as OS Service

    Cliente->>MP: Paga via link do Checkout Pro
    MP->>Bill: POST /api/v1/webhooks/mercadopago (notificação)
    Bill->>Bill: Valida assinatura HMAC-SHA256
    Bill->>MP: GET /v1/payments/{id} (confirma o status — nunca confia cegamente no webhook)

    alt Pagamento aprovado
        Bill->>Bill: Pagamento.Aprovar()
        Bill-->>OS: PagamentoAprovado (evento)
    else Pagamento recusado
        Note over Bill: Compensação
        Bill->>Bill: Pagamento.Recusar()
        Bill-->>OS: PagamentoRecusado (evento)
    end
```

### Idempotência de `GerarOrcamentoUseCase`

Três ramos, cobertos explicitamente em `GerarOrcamentoUseCaseTests`:

1. **Orçamento não existe** → cria Orcamento + chama Mercado Pago + cria Pagamento + publica `OrcamentoGerado`.
2. **Orçamento existe E Pagamento existe** → reentrega da mensagem já processada, curto-circuita como duplicado (`OrcamentoJaExiste`).
3. **Orçamento existe mas Pagamento não existe** → uma tentativa anterior salvou o Orçamento e falhou antes de concluir a integração com o Mercado Pago; retoma a partir daqui em vez de recriar o Orçamento ou tratar como duplicado.

Se a chamada ao Mercado Pago falhar (incluindo numa retomada do ramo 3), o use case
publica `OrcamentoFalhou` — ver "Fluxo — geração de orçamento" acima.

## Mensageria — comandos e consumers

| Mensagem | Tipo | Direção | Consumer/Handler |
|---|---|---|---|
| `GerarOrcamento` | Comando | OS → Billing | `GerarOrcamentoConsumer` |
| `OrcamentoGerado` | Evento | Billing → OS | — |
| `OrcamentoFalhou` | Evento (compensação) | Billing → OS | — |
| `PagamentoAprovado` | Evento | Billing → OS | — |
| `PagamentoRecusado` | Evento (compensação) | Billing → OS | — |

Publishers em `Infrastructure/Messaging/MassTransitSagaEventPublisher.cs`; contratos
compartilhados (cópia idêntica nos 3 repos) em `Application/Messaging/Contracts/SagaContracts.cs`.

## Persistência

PostgreSQL via EF Core, banco lógico `soat_billing` isolado (ver README > "Banco de
dados"). Migração inicial em `src/Infrastructure/Database/Migrations`.

## Segurança

Nunca emite JWT — valida o Bearer com o mesmo segredo simétrico do OS Service. O
webhook do Mercado Pago (`POST /api/v1/webhooks/mercadopago`) é a única rota
`[AllowAnonymous]`: autentica via assinatura HMAC, não via Bearer.
