# minicambio-rates

Serviço **Rates** do MiniCâmbio, um sistema de câmbio simplificado usado como projeto de estudo.
Fornece taxas de câmbio mockadas e emite cotações com prazo de validade.

> Repositório em construção (Fase 0: estrutura). Endpoints, variáveis de ambiente e instruções completas
> entram nas próximas fases.

## Requisitos

- .NET SDK 10
- Docker Desktop (testes de integração usam Testcontainers)

## Build e testes

```powershell
dotnet build MiniCambio.Rates.slnx
dotnet test MiniCambio.Rates.slnx
```

## Rodar localmente

```powershell
dotnet run --project src/MiniCambio.Rates.Api
```

A API sobe em `http://localhost:5001`.

## Docker

```powershell
docker build -t minicambio-rates-api .
```

A imagem roda como usuário não-root e escuta na porta `8080`. Para subir o sistema inteiro (bancos, APIs e
frontend), use o `docker-compose.yml` do repositório `minicambio-infra`.
