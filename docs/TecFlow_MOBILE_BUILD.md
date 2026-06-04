# TecFlow.Mobile — Build, Push e Deep Links

## Build

```bash
dotnet restore TecFlow.sln
dotnet build TecFlow.sln
dotnet publish TecFlow.Mobile/TecFlow.Mobile.csproj -f net9.0-android -c Release
```

## Push (FCM / APNs)

1. Configure `Firebase:CredentialsPath` ou `CredentialsJson` em `TecFlow.API/appsettings.json`.
2. Android: adicione `google-services.json` ao projeto Mobile (não commitar).
3. O app envia `POST /api/devices/register` com JWT após obter token FCM/APNs.

## Deep links (`tecflow://`)

| URL | Rota |
|-----|------|
| `tecflow://engajamento/fila` | `/engajamento/fila` |
| `tecflow://conciliacao/detalhes/42` | `/conciliacao/detalhes/42` |

Teste Android: `adb shell am start -a android.intent.action.VIEW -d "tecflow://engajamento/fila"`
