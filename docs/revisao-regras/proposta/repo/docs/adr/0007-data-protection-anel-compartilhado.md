# 0007. Data Protection com anel de chaves compartilhado

- **Status:** Aceita (registro retroativo de decisão já em vigor)
- **Data:** 2026-11-25

## Contexto

Tokens de marketplace precisam ser lidos pela API e pelos processos em segundo plano.

## Decisão

API e Worker compartilham o anel em `DataProtection:KeysPath` (volume `api_appdata` nos containers, sem DPAPI) (`docs/rules/backend-architecture.md:56`).

## Alternativas descartadas

- **Chave AES fixa em configuração:** era o legado `ENC1:`; mais difícil de rotacionar.

## Consequências

- Perder o volume torna os tokens ilegíveis: backup das chaves é obrigatório.
- Chaves do Visual Studio (DPAPI) não abrem nos containers e vice-versa.
