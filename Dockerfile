# ── Estágio 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura dependências
COPY GestorVendas.Domain/GestorVendas.Domain.csproj           GestorVendas.Domain/
COPY GestorVendas.Application/GestorVendas.Application.csproj GestorVendas.Application/
COPY GestorVendas.Infra/GestorVendas.Infra.csproj             GestorVendas.Infra/
COPY GestorVendas.Web/GestorVendas.Web.csproj                 GestorVendas.Web/

RUN dotnet restore GestorVendas.Web/GestorVendas.Web.csproj

# Copia o restante do código e publica
COPY . .
RUN dotnet publish GestorVendas.Web/GestorVendas.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Estágio 2: Runtime ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Cria usuário não-root por segurança
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

# Railway injeta a variável PORT automaticamente
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "GestorVendas.Web.dll"]
