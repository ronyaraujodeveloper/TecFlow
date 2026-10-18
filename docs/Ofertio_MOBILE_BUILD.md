# Ofertio.Mobile — Build, Push e Deep Links

## Build

```bash
dotnet restore Ofertio.sln
dotnet build Ofertio.sln
dotnet publish Ofertio.Mobile/Ofertio.Mobile.csproj -f net9.0-android -c Release
```

## Push (FCM / APNs)

1. Configure `Firebase:CredentialsPath` ou `CredentialsJson` em `Ofertio.API/appsettings.json`.
2. Android: adicione `google-services.json` ao projeto Mobile (não commitar).
3. O app envia `POST /api/devices/register` com JWT após obter token FCM/APNs.

## Deep links (`ofertio://`)

| URL | Rota |
|-----|------|
| `ofertio://engajamento/fila` | `/engajamento/fila` |
| `ofertio://conciliacao/detalhes/42` | `/conciliacao/detalhes/42` |

Teste Android: `adb shell am start -a android.intent.action.VIEW -d "ofertio://engajamento/fila"`
