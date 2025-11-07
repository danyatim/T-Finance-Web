FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["TFinanceBackend.csproj", "."]
RUN dotnet restore "TFinanceBackend.csproj"

COPY . .

RUN dotnet publish "TFinanceBackend.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final

ARG APP_UID=1001
ARG APP_GID=1001

RUN addgroup --system --gid ${APP_GID} appgroup \
    && adduser --system --uid ${APP_UID} --ingroup appgroup appuser

WORKDIR /app

COPY --from=build /app/publish ./

RUN chown -R appuser:appgroup /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "TFinanceBackend.dll"]