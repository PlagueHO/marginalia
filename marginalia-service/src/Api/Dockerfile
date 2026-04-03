FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Api/Marginalia.Api.csproj", "src/Api/"]
COPY ["src/Domain/Marginalia.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Marginalia.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Orchestration/ServiceDefaults/Marginalia.ServiceDefaults.csproj", "src/Orchestration/ServiceDefaults/"]

RUN dotnet restore "src/Api/Marginalia.Api.csproj"

COPY ["src/", "src/"]

WORKDIR /src/src/Api
RUN dotnet publish "Marginalia.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Marginalia.Api.dll"]