# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy central package management / build props first for better layer caching.
COPY Directory.Build.props Directory.Packages.props ./

# Copy csproj files only, then restore (maximizes Docker layer cache hits).
COPY src/CampaignUserService.Api/CampaignUserService.Api.csproj src/CampaignUserService.Api/
COPY src/CampaignUserService.Application/CampaignUserService.Application.csproj src/CampaignUserService.Application/
COPY src/CampaignUserService.Domain/CampaignUserService.Domain.csproj src/CampaignUserService.Domain/
COPY src/CampaignUserService.Infrastructure/CampaignUserService.Infrastructure.csproj src/CampaignUserService.Infrastructure/
COPY src/CampaignUserService.SharedKernel/CampaignUserService.SharedKernel.csproj src/CampaignUserService.SharedKernel/

RUN dotnet restore src/CampaignUserService.Api/CampaignUserService.Api.csproj

# Copy the remaining source and publish.
COPY src/ src/

RUN dotnet publish src/CampaignUserService.Api/CampaignUserService.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Run as a non-root user (security best practice).
RUN addgroup --gid 1000 appgroup && adduser --uid 1000 --gid 1000 --disabled-password --gecos "" appuser

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "CampaignUserService.Api.dll"]
