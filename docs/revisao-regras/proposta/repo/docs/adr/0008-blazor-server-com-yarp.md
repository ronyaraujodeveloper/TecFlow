# 0008. WebUi Blazor Server repassando rotas públicas para a API via YARP

- **Status:** Aceita (registro retroativo de decisão já em vigor)
- **Data:** 2026-11-25

## Contexto

Callbacks OAuth e links de rastreio precisam de um host público único.

## Decisão

A WebUi atende o domínio e repassa `/api/mercadolivre/*`, `/r/*` e rotas públicas para a API via YARP; o Caddy envia `/api/*` direto para a API (`docs/rules/marketplaces-oauth.md:21`; `docs/rules/ambientes-docker.md:20`).

## Alternativas descartadas

- **Subaplicação `/api` no IIS:** causava HTTP 500.35 no mesmo pool (motivo histórico).

## Consequências

- Duas rotas para a API (Caddy e YARP) precisam ficar coerentes.
