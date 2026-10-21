FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json RequestsManagement.slnx ./
COPY src/RequestsManagement.Core/*.csproj src/RequestsManagement.Core/
COPY src/RequestsManagement.Infrastructure/*.csproj src/RequestsManagement.Infrastructure/
COPY src/RequestsManagement.Api/*.csproj src/RequestsManagement.Api/
RUN dotnet restore src/RequestsManagement.Api/RequestsManagement.Api.csproj
COPY src/ src/
RUN dotnet publish src/RequestsManagement.Api/RequestsManagement.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "RequestsManagement.Api.dll"]
