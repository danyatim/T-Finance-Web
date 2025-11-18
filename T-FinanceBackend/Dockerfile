## Production multi-stage Dockerfile for T-FinanceBackend
## Build stage: use SDK to restore & publish
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy csproj first to leverage Docker cache for restore
COPY ["TFinanceBackend.csproj", "./"]
RUN dotnet restore "TFinanceBackend.csproj"

# Copy rest of sources and publish
COPY . ./
RUN dotnet publish "TFinanceBackend.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    /p:UseAppHost=false


## Runtime stage: smaller ASP.NET image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final

WORKDIR /app

# Create a non-root user for running the app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Copy published app
COPY --from=build /app/publish ./

# Environment
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Ensure permissions for the non-root user
RUN chown -R appuser:appgroup /app

EXPOSE 8080

# Healthcheck endpoint expects /healthz to return 200
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

USER appuser

ENTRYPOINT ["dotnet", "TFinanceBackend.dll"]