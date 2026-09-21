# SOAT — Billing Service

[![CI/CD](https://github.com/monnclaro/soat-tech-challenge-billing-service/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/monnclaro/soat-tech-challenge-billing-service/actions/workflows/ci-cd.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=soat-tech-challenge-billing-service&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=soat-tech-challenge-billing-service)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=soat-tech-challenge-billing-service&metric=coverage)](https://sonarcloud.io/summary/new_code?id=soat-tech-challenge-billing-service)

Microsserviço responsável por **orçamento e pagamento** dentro da arquitetura de
microsserviços da Fase 4 do Tech Challenge (FIAP). Extraído do monolito
[`soat-tech-challenge`](https://github.com/monnclaro/soat-tech-challenge) — o conceito de
Orçamento/Pagamento é novo, não existia no monolito original.

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

Documentação completa (diagramas de camadas, modelo de domínio, máquina de estados, fluxo de geração de orçamento/pagamento, mensageria): [docs/architecture.md](./docs/architecture.md).

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

## Papel na saga

O OS Service é o orquestrador: seu próprio agregado `OrdemServico` guarda o estado da saga e
reage a domain events publicando comandos. Este serviço só reage ao comando abaixo e publica
os dois eventos de volta — não conhece os outros passos da saga (diagnóstico, execução):

| Direção | Mensagem | Efeito neste serviço |
|---|---|---|
| OS Service → Billing (comando) | `GerarOrcamento` | Gera o orçamento + cria a preferência de pagamento no Mercado Pago |
| Billing → OS Service (evento) | `OrcamentoGerado` | Publicado ao concluir a geração do orçamento |
| Billing → OS Service (evento) | `OrcamentoFalhou` | **Compensação**: publicado se a chamada ao Mercado Pago falhar (rede, API fora do ar) — o OS Service cancela a OS em vez de ficar esperando um orçamento que nunca chega |
| Billing → OS Service (evento) | `PagamentoAprovado` / `PagamentoRecusado` | Publicado a partir do webhook do Mercado Pago — `PagamentoRecusado` é o caminho de compensação da saga (cancela a OS) |

Justificativa completa do desenho da saga (por que a orquestração vive no OS Service, sem um saga state machine separado): [ADR 0001 no repositório do OS Service](https://github.com/monnclaro/soat-tech-challenge-os-service/blob/main/docs/adr/0001-saga-orquestrada-sem-state-machine-separado.md).

## Mensageria (RabbitMQ/MassTransit)

Um consumer MassTransit (`GerarOrcamentoConsumer`) reage ao comando `GerarOrcamento`
publicado pelo OS Service, reaproveitando o `GerarOrcamentoUseCase` já existente (mesma regra
de negócio do endpoint REST interno — nenhuma lógica duplicada entre a via HTTP e a via
mensageria). Ao concluir, publica `OrcamentoGerado`; se a chamada ao Mercado Pago lançar
exceção, publica `OrcamentoFalhou` em vez de deixar a mensagem cair na fila de erro do
RabbitMQ sem nenhuma reação (compensação — ver "Papel na saga" acima); ao aprovar ou recusar
um pagamento (webhook do Mercado Pago), publica `PagamentoAprovado`/`PagamentoRecusado` —
todos consumidos pelo OS Service para avançar/compensar a saga. Contratos em `Soat.Contracts.Saga`
(`src/Application/Messaging/Contracts/SagaContracts.cs`), cópia idêntica à dos outros dois
serviços — mantida por convenção em cada repo em vez de um pacote NuGet compartilhado, para
evitar a complexidade de um feed privado nesta fase do projeto (são DTOs puros, marcados com
as interfaces `ISagaCommand`/`ISagaEvent` para deixar explícito no próprio tipo se é um
comando ou um evento da saga).

**Verificado contra infraestrutura real** (RabbitMQ + Postgres locais, sem mocks): um
publisher standalone simulando o OS Service publicou `GerarOrcamento`, e o consumer
efetivamente criou o `Orcamento` + itens no Postgres e chamou a API real do Mercado Pago
(falhou com 401 por não haver um Access Token de sandbox configurado neste teste — a
integração em si é real, não mockada). Esse teste também revelou uma lacuna de idempotência
real (retry após falha na chamada ao Mercado Pago reprocessava como "já existe" sem nunca
criar o Pagamento) — corrigida: `GerarOrcamentoUseCase` agora retoma a partir de um `Orcamento`
já persistido sem `Pagamento` associado, em vez de tratá-lo como duplicado.

## Escopo — o que ainda fica de fora deste repositório

- Integração com o Mercado Pago não foi exercitada contra uma conta de sandbox real (só
  contra a API real com token inválido, confirmando que a chamada em si funciona) — falta
  testar o fluxo completo com credenciais de teste válidas.
- Testes de integração com Testcontainers (Postgres) — os testes hoje são unitários
  (domínio + Application com Moq) e de arquitetura.

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

Especificação OpenAPI (Swagger) exportada em [`docs/openapi.json`](./docs/openapi.json) —
importável direto no Postman (File > Import) ou em qualquer ferramenta compatível com
OpenAPI 3. Com a API rodando localmente, a versão sempre atualizada também fica disponível
em `/openapi/v1.json`.

## Testes e cobertura

```bash
dotnet test
```

Cobre: regras de arquitetura (NetArchTest, `tests/Tests/Camadas`), transições de estado das
entidades `Orcamento`/`Pagamento` (xUnit + FluentAssertions), e os use cases/consumers/presenters
da Application/Infrastructure/Api (Moq) — incluindo os 3 ramos de idempotência de
`GerarOrcamentoUseCase` e o caminho de compensação `OrcamentoFalhou`.

### Evidência de cobertura

**122/122 testes passando**, gerado localmente com Coverlet
(`dotnet test -p:CollectCoverage=true -p:CoverletOutputFormat=opencover`):

| Módulo | Linha | Branch | Método |
|---|---|---|---|
| Api | 99,16% | 92% | 98% |
| Application | 94,02% | 100% | 82,35% |
| Domain | 91,3% | 92,3% | 84,61% |
| Infrastructure | 94,16% | 100% | 84,9% |
| SharedKernel | 100% | 100% | 100% |
| **Total** | **94,54%** | **96,33%** | **86,66%** |

Cobertura contínua nos badges no topo deste README e no
[dashboard do SonarCloud](https://sonarcloud.io/summary/new_code?id=soat-tech-challenge-billing-service).

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
