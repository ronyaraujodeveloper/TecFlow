# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TecFlow.sln ./
COPY TecFlow.Core/TecFlow.Core.csproj TecFlow.Core/
COPY TecFlow.Application/TecFlow.Application.csproj TecFlow.Application/
COPY TecFlow.Util/TecFlow.Util.csproj TecFlow.Util/
COPY TecFlow.Infrastructure/TecFlow.Infrastructure.csproj TecFlow.Infrastructure/
COPY TecFlow.Infrastructure.Services/TecFlow.Infrastructure.Services.csproj TecFlow.Infrastructure.Services/
COPY TecFlow.Orquestrador/TecFlow.Orquestrador.csproj TecFlow.Orquestrador/

RUN dotnet restore TecFlow.Orquestrador/TecFlow.Orquestrador.csproj

COPY TecFlow.Core/ TecFlow.Core/
COPY TecFlow.Application/ TecFlow.Application/
COPY TecFlow.Util/ TecFlow.Util/
COPY TecFlow.Infrastructure/ TecFlow.Infrastructure/
COPY TecFlow.Infrastructure.Services/ TecFlow.Infrastructure.Services/
COPY TecFlow.Orquestrador/ TecFlow.Orquestrador/

RUN dotnet publish TecFlow.Orquestrador/TecFlow.Orquestrador.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TecFlow.Orquestrador.dll"]
