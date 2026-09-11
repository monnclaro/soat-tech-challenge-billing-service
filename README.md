# SOAT — Billing Service

Microsserviço responsável por **orçamento e pagamento** dentro da arquitetura de
microsserviços da Fase 4 do Tech Challenge (FIAP). Extraído do monolito
[`soat-tech-challenge`](https://github.com/monnclaro/soat-tech-challenge) — o conceito de
Orçamento/Pagamento é novo, não existia no monolito original.

Plano completo da migração (arquitetura, saga, infraestrutura, ordem de execução):
[`PLANO-FASE-4-MICROSSERVICOS.md`](../PLANO-FASE-4-MICROSSERVICOS.md) na raiz do workspace.

## Responsabilidades

- Gerar o orçamento de uma ordem de serviço a partir dos itens identificados no diagnóstico.
- Criar a cobrança no Mercado Pago (Checkout Pro) e devolver o link de pagamento.
- Registrar e verificar o resultado do pagamento via webhook do Mercado Pago.
- Publicar o resultado (orçamento gerado, pagamento aprovado/recusado) para o OS Service
  avançar ou compensar a saga.

## Arquitetura

Clean Architecture, mesmo padrão validado no monolito de origem e replicado no
[OS Service](https://github.com/monnclaro/soat-tech-challenge-os-service):

```
src/
  Domain/          # Orcamento, Pagamento — entidades, regras de negócio, sem dependências externas
  Application/     # Casos de uso, ports (persistência + integração com o Mercado Pago)
  Infrastructure/  # EF Core (PostgreSQL), SDK do Mercado Pago, segurança (validação JWT)
  Api/             # ASP.NET Core host, controllers, presenters, middlewares
  SharedKernel/    # Tipos cross-cutting (marcadores de DI)
```

Regras de dependência entre camadas garantidas por testes de arquitetura (NetArchTest) em
`tests/Tests/Camadas`.

### Entidades

- **Orcamento**: `IdOrdemServico`, snapshot dos itens (`OrcamentoItem`: nome/valor/tipo
  congelados no momento da geração — mesmo padrão de "congelar" dados já usado no monolito
  em `OrdemServicoServico`/`OrdemServicoProduto`), `ValorTotal`, `Status`
  (`Pendente` → `Aprovado` | `Reprovado` | `Expirado`).
- **Pagamento**: `IdOrcamento`, `PreferenceId`/`PaymentId` do Mercado Pago, `Status`
  (`Pendente` → `Aprovado` | `Recusado`), `Valor`, datas de criação/atualização. Nasce junto
  com o Orçamento, já com a preferência de pagamento criada no Mercado Pago.

## Integração com o Mercado Pago

Fluxo **Checkout Pro** via SDK oficial ([`mercadopago-sdk`](https://www.nuget.org/packages/mercadopago-sdk),
pacote .NET mantido pela própria Mercado Pago — https://github.com/mercadopago/sdk-dotnet):

1. Ao gerar um orçamento (`GerarOrcamentoUseCase`), o serviço cria uma *Preference* com
   `external_reference = IdOrdemServico` e devolve o `init_point` (link de pagamento) —
   implementado em `Infrastructure/MercadoPago/MercadoPagoGateway.cs`.
2. O Mercado Pago notifica o resultado via webhook
   (`POST /api/v1/webhooks/mercadopago`, sem autenticação JWT — a autenticidade é garantida
   pela assinatura da notificação, não por um Bearer token).
3. A assinatura é validada com o algoritmo HMAC-SHA256 documentado oficialmente
   (`x-signature`/`x-request-id`) em `Infrastructure/MercadoPago/WebhookSignatureValidator.cs`.
   Sem `MercadoPago:WebhookSecret` configurado (ex.: ambiente local sem o segredo real), a
   validação é ignorada com um aviso no código — **isto é uma limitação aceita do scaffold**,
   nunca deve acontecer com credenciais reais configuradas.
4. O resultado da notificação nunca é confiado cegamente: o serviço sempre consulta de volta
   `GET /v1/payments/{id}` no Mercado Pago para confirmar o status antes de atualizar
   `Pagamento`/`Orcamento`.

### Configurando credenciais de sandbox

1. Crie (ou use) uma conta de desenvolvedor no [Mercado Pago Developers](https://www.mercadopago.com.br/developers).
2. Em "Suas integrações" → crie uma aplicação → aba "Credenciais de teste" → copie o
   **Access Token de teste** (`TEST-...`).
3. Configure em `.env` (`MercadoPago__AccessToken`) ou `appsettings.Development.json`
   (`MercadoPago:AccessToken`) — nunca commitar uma credencial real.
4. Para testar o webhook localmente, exponha a porta 8082 publicamente (ex.: `ngrok http 8082`)
   e configure a URL pública + `/api/v1/webhooks/mercadopago` como notification URL da
   aplicação de teste no painel do Mercado Pago; copie o "Secret Key" gerado para
   `MercadoPago__WebhookSecret`.

## Mensageria (RabbitMQ/MassTransit)

Ligada: um consumer MassTransit (`GerarOrcamentoConsumer`) reage ao comando `GerarOrcamento`
publicado pelo OS Service, reaproveitando o `GerarOrcamentoUseCase` já existente (mesma regra
de negócio do endpoint REST interno). Ao concluir, publica `OrcamentoGerado`; ao aprovar ou
recusar um pagamento (webhook do Mercado Pago), publica `PagamentoAprovado`/`PagamentoRecusado`
— todos consumidos pelo OS Service para avançar/compensar a saga. Contratos em
`Soat.Contracts.Saga` (`src/Application/Messaging/Contracts/SagaContracts.cs`), cópia idêntica
à do OS Service (sem pacote NuGet compartilhado — ver plano).

**Verificado contra infraestrutura real** (RabbitMQ + Postgres locais, sem mocks): um
publisher standalone simulando o OS Service publicou `GerarOrcamento`, e o consumer
efetivamente criou o `Orcamento` + itens no Postgres e chamou a API real do Mercado Pago
(falhou com 401 por não haver um Access Token de sandbox configurado neste teste — a
integração em si é real, não mockada). Esse teste também revelou uma lacuna de idempotência
real (retry após falha na chamada ao Mercado Pago reprocessava como "já existe" sem nunca
criar o Pagamento) — corrigida: `GerarOrcamentoUseCase` agora retoma a partir de um `Orcamento`
já persistido sem `Pagamento` associado, em vez de tratá-lo como duplicado.

## Escopo deste scaffold — o que ainda falta (follow-up)

- Não há Kubernetes manifests nem pipeline de CI/CD neste repositório ainda — deferidos para
  uma fase posterior, junto dos outros dois serviços.
- Integração com o Mercado Pago não foi exercitada contra uma conta de sandbox real (só
  contra a API real com token inválido, confirmando que a chamada em si funciona) — falta
  testar o fluxo completo com credenciais de teste válidas.

## Banco de dados

PostgreSQL (`soat_billing`) — banco lógico próprio, isolado do banco do OS Service (nenhum
outro serviço acessa este banco diretamente). Migração inicial (`InicialBillingService`) já
gerada em `src/Infrastructure/Database/Migrations`.

## Autenticação

Este serviço **nunca emite JWT** — apenas valida o Bearer recebido usando o mesmo segredo
simétrico compartilhado com o OS Service (que é quem autentica o back-office). Endpoints
`GET`/`POST` de orçamentos e pagamentos exigem `[Authorize]`; o webhook do Mercado Pago
(`POST /api/v1/webhooks/mercadopago`) é `[AllowAnonymous]` e se autentica via assinatura
HMAC, não via Bearer token.

## Rodando localmente

```bash
cp .env.example .env   # ajuste a senha do Postgres e as credenciais de teste do Mercado Pago
docker compose up --build
```

API em `http://localhost:8082`, documentação OpenAPI (Scalar) em `/scalar` (ambiente de
desenvolvimento), health check em `/health`.

## Testes

```bash
dotnet test
```

Cobre: regras de arquitetura (NetArchTest, `tests/Tests/Camadas`) e transições de estado das
entidades `Orcamento`/`Pagamento` (xUnit + FluentAssertions, `tests/Tests/Domain`).

## CI/CD

`.github/workflows/ci-cd.yml`, mesmo padrão do OS Service:

1. **Gate de cobertura (80%)** — `dotnet test` com Coverlet (`/p:Threshold=80 /p:ThresholdType=line`), falha o job se ficar abaixo.
2. **Quality Gate do SonarCloud** — `dotnet-sonarscanner begin/end` em volta do build, consumindo o relatório OpenCover do Coverlet.

Em `pull_request`, roda só `build-test`. Em `push` para `main`, roda também `deploy`: build/push da imagem no ECR e `kubectl apply` dos manifests em `k8s/` (namespace `soat-billing`, NodePort `30082`).

### Secrets necessários no repositório GitHub

| Secret | Uso |
|---|---|
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_SESSION_TOKEN` | Credenciais de sessão temporária da AWS Academy (deploy) |
| `SONAR_TOKEN` | Autenticação no SonarCloud (org `monnclaro`) |
| `MERCADOPAGO_ACCESS_TOKEN` / `MERCADOPAGO_WEBHOOK_SECRET` | Credenciais reais/sandbox do Mercado Pago, injetadas no Secret do deployment |
| `NEW_RELIC_LICENSE_KEY` | Injetada no Secret do deployment |

### Proteção da branch `main`

Configuração manual no GitHub (Settings > Branches): exigir PR antes do merge, exigir que o check `Build, Test & Quality Gate` passe, sem push direto.
