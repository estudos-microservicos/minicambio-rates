# minicambio-rates

Serviço **Rates** do MiniCâmbio, um sistema de câmbio simplificado usado como projeto de estudo.
Fornece taxas de câmbio mockadas e emite cotações com prazo de validade. É chamado pelo serviço Orders.

## Regras

- Moedas suportadas (seed da migration): USD 5,40 · EUR 5,85 · GBP 6,90 · CHF 6,10 (BRL por unidade).
- Taxa média "ao vivo" = taxa da tabela com variação aleatória de ±1%, recalculada a cada consulta (não persistida).
- Spread (`Rates__SpreadPercent`, padrão `0.5` = 0,5%):
  - `Buy` (cliente compra moeda estrangeira): `rate = mid * (1 + spread)`;
  - `Sell` (cliente vende moeda estrangeira): `rate = mid * (1 - spread)`.
- `brlAmount = round(foreignAmount * rate, 2)` (arredondamento `AwayFromZero`); taxas com 6 casas.
- Cotação válida por `Rates__QuoteTtlSeconds` segundos (padrão `60`).

## Endpoints

| Método | Rota | Resposta |
|---|---|---|
| GET | `/api/v1/rates` | 200 com `[{ currency, midRate, buyRate, sellRate, quotedAt, servedBy }]` |
| GET | `/api/v1/rates/{currency}` | 200 ou 404 |
| POST | `/api/v1/quotes` | body `{ currency, side, foreignAmount }` → 201 + `Location`; 400 se inválido; 422 se a moeda não é suportada |
| GET | `/api/v1/quotes/{id}` | 200 com a cotação e `isExpired`; 404 |
| GET | `/health/live` | 200 se o processo responde (sem dependências) |
| GET | `/health/ready` | 200 se o banco responde |

Erros seguem ProblemDetails (`title` e `detail` em português; 400 traz `errors` por campo). Todas as respostas
devolvem o header `X-Correlation-Id` (o recebido ou um novo). Em `Development`, a documentação fica em
`/scalar` (OpenAPI em `/openapi/v1.json`).

## Variáveis de ambiente

| Variável | Padrão | Descrição |
|---|---|---|
| `ConnectionStrings__Default` | — (obrigatória) | Connection string do Postgres |
| `Database__ApplyMigrationsOnStartup` | `false` | Aplica as migrations ao subir (`true` no compose) |
| `Rates__SpreadPercent` | `0.5` | Spread em pontos percentuais |
| `Rates__QuoteTtlSeconds` | `60` | Validade da cotação em segundos |

Em `Development`, o `appsettings.Development.json` já aponta para o Postgres do `compose.dev.yml` e liga as migrations.

## Requisitos

- .NET SDK 10
- Docker Desktop (Postgres local e testes de integração com Testcontainers)

## Rodar localmente

```powershell
docker compose -f compose.dev.yml up -d          # só o Postgres, na porta 5433
dotnet run --project src/MiniCambio.Rates.Api    # http://localhost:5001 (Scalar em /scalar)
```

## Testes

```powershell
dotnet test MiniCambio.Rates.slnx
```

- `tests/MiniCambio.Rates.UnitTests`: cálculos de taxa/spread/arredondamento, cotação, validação e handlers (com fakes).
- `tests/MiniCambio.Rates.IntegrationTests`: endpoints contra um Postgres real (Testcontainers), com a variação
  aleatória fixada em zero.

## Migrations

```powershell
dotnet tool exec dotnet-ef@10.0.14 --yes -- migrations add <Nome> --project src/MiniCambio.Rates.Api --output-dir Infrastructure/Persistence/Migrations
```

## Docker

```powershell
docker build -t minicambio-rates-api .
```

A imagem roda como usuário não-root e escuta na porta `8080`. Para subir o sistema inteiro (bancos, APIs e
frontend), use o `docker-compose.yml` do repositório `minicambio-infra`.
