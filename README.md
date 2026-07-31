# Meme Token Hub Social Service

The Social Service is the authoritative bounded context for Meme Token Hub community interactions. It owns follows, likes, comments, sentiment votes, timestamped KOL support, community posts, activity feeds, and reputation source data. Other services receive changes through versioned integration events rather than sharing its MongoDB database.

## Technology

- .NET 10 and ASP.NET Core Web API
- MongoDB (`MemeTokenHubSocial`)
- JWT bearer authentication
- Swashbuckle/OpenAPI with Swagger UI
- NUnit unit, integration, and contract test projects
- Azure App Service deployment through GitHub Actions

## Architecture

The solution separates responsibilities to support SOLID design:

- `Domain`: entities and domain enums without infrastructure dependencies.
- `Application`: use-case interfaces, request/response models, and orchestration services.
- `Infrastructure`: MongoDB persistence and integration-event adapters.
- `Api`: controllers, authentication, Swagger, health checks, and middleware.

Every interface and top-level class has its own file. The API depends on abstractions, and infrastructure implementations are selected through dependency injection.

## Prerequisites

- .NET SDK 10
- MongoDB 7 or later
- A GitHub Packages token with `read:packages` when enabling `MemeTokenHub.Shared`

## Package source

`nuget.config` includes `https://nuget.pkg.github.com/isboat/index.json`. The pinned `MemeTokenHub.Shared` package is optional during anonymous local builds because GitHub Packages requires authentication. Enable it after authenticating:

```bash
dotnet nuget update source MemeTokenHub --username USER --password TOKEN --store-password-in-clear-text
dotnet restore -p:UseMemeTokenHubShared=true
```

## Run locally

```bash
dotnet restore
dotnet build
dotnet run --project src/MemeTokenHub.SocialService.Api
```

Swagger UI is available at `/swagger`; the OpenAPI document is `/swagger/v1/swagger.json`. Health endpoints are `/health/live` and `/health/ready`.

Override configuration with environment variables such as `MongoDb__ConnectionString`, `MongoDb__DatabaseName`, `Jwt__SecretKey`, `Jwt__Issuer`, and `Jwt__Audience`. Never use the development JWT secret in production.

## Quality checks

```bash
dotnet format MemeTokenHub.SocialService.sln --verify-no-changes
dotnet build MemeTokenHub.SocialService.sln --configuration Release
dotnet test MemeTokenHub.SocialService.sln --configuration Release
```

Formatting is enforced by `.editorconfig` using Microsoft C# formatting option names. `Directory.Build.props` enables current analyzers and treats warnings as errors.

## CI/CD

- `.github/workflows/pull-request.yml` runs only for pull requests and performs restore, formatting verification, build, and tests.
- `.github/workflows/main.yml` runs validation and publish on pushes to `main`. Production deployment occurs only through its manual `workflow_dispatch` trigger and requires the Azure OIDC and web-app secrets referenced by that workflow.

## API areas

All routes are under `/api/social`: follows, token likes/comments, votes, KOL support, posts, and reputation. Write operations require a platform JWT. Public aggregate and content reads permit anonymous access where documented in Swagger.
