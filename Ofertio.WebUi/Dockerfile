FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src

COPY ["nuget.config", "./"]
COPY ["Ofertio.WebUi/Ofertio.WebUi.csproj", "Ofertio.WebUi/"]
COPY ["Ofertio.SharedUi/Ofertio.SharedUi.csproj", "Ofertio.SharedUi/"]
COPY ["Ofertio.Core/Ofertio.Core.csproj", "Ofertio.Core/"]

RUN dotnet restore "Ofertio.WebUi/Ofertio.WebUi.csproj" --configfile nuget.config /p:NuGetSignatureValidationMode=accept

COPY . .
WORKDIR "/src/Ofertio.WebUi"
RUN dotnet publish "Ofertio.WebUi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ofertio.WebUi.dll"]
