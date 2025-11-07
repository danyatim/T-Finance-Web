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

WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "TFinanceBackend.dll"]