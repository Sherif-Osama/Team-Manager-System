# Build from the solution root so the API can resolve all Clean Architecture project references.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY ["TeamManager.Api/TeamManager.Api.csproj", "TeamManager.Api/"]
COPY ["TeamManager.Application/TeamManager.Application.csproj", "TeamManager.Application/"]
COPY ["TeamManager.Domain/TeamManager.Domain.csproj", "TeamManager.Domain/"]
COPY ["TeamManager.Infrastructure/TeamManager.Infrastructure.csproj", "TeamManager.Infrastructure/"]

RUN dotnet restore "TeamManager.Api/TeamManager.Api.csproj"

FROM restore AS publish
COPY . .
WORKDIR /src/TeamManager.Api
RUN dotnet publish "TeamManager.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "TeamManager.Api.dll"]
