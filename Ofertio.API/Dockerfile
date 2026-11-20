FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src

COPY ["nuget.config", "./"]
COPY ["Ofertio.API/Ofertio.API.csproj", "Ofertio.API/"]
COPY ["Ofertio.Application/Ofertio.Application.csproj", "Ofertio.Application/"]
COPY ["Ofertio.Business/Ofertio.Business.csproj", "Ofertio.Business/"]
COPY ["Ofertio.Core/Ofertio.Core.csproj", "Ofertio.Core/"]
COPY ["Ofertio.Data/Ofertio.Data.csproj", "Ofertio.Data/"]
COPY ["Ofertio.Database/Ofertio.Database.csproj", "Ofertio.Database/"]
COPY ["Ofertio.Infrastructure/Ofertio.Infrastructure.csproj", "Ofertio.Infrastructure/"]
COPY ["Ofertio.Infrastructure.Services/Ofertio.Infrastructure.Services.csproj", "Ofertio.Infrastructure.Services/"]
COPY ["Ofertio.Observability/Ofertio.Observability.csproj", "Ofertio.Observability/"]
COPY ["Ofertio.Util/Ofertio.Util.csproj", "Ofertio.Util/"]

RUN dotnet restore "Ofertio.API/Ofertio.API.csproj" --configfile nuget.config /p:NuGetSignatureValidationMode=accept

COPY . .
WORKDIR "/src/Ofertio.API"
RUN dotnet publish "Ofertio.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ofertio.API.dll"]
