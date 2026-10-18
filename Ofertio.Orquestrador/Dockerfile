# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Ofertio.sln ./
COPY Ofertio.Core/Ofertio.Core.csproj Ofertio.Core/
COPY Ofertio.Application/Ofertio.Application.csproj Ofertio.Application/
COPY Ofertio.Util/Ofertio.Util.csproj Ofertio.Util/
COPY Ofertio.Infrastructure/Ofertio.Infrastructure.csproj Ofertio.Infrastructure/
COPY Ofertio.Infrastructure.Services/Ofertio.Infrastructure.Services.csproj Ofertio.Infrastructure.Services/
COPY Ofertio.Orquestrador/Ofertio.Orquestrador.csproj Ofertio.Orquestrador/

RUN dotnet restore Ofertio.Orquestrador/Ofertio.Orquestrador.csproj

COPY Ofertio.Core/ Ofertio.Core/
COPY Ofertio.Application/ Ofertio.Application/
COPY Ofertio.Util/ Ofertio.Util/
COPY Ofertio.Infrastructure/ Ofertio.Infrastructure/
COPY Ofertio.Infrastructure.Services/ Ofertio.Infrastructure.Services/
COPY Ofertio.Orquestrador/ Ofertio.Orquestrador/

RUN dotnet publish Ofertio.Orquestrador/Ofertio.Orquestrador.csproj \
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

ENTRYPOINT ["dotnet", "Ofertio.Orquestrador.dll"]
