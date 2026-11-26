# 0005. Só API oficial dos marketplaces, sem raspagem

- **Status:** Aceita (registro retroativo de decisão já em vigor)
- **Data:** 2026-11-25

## Contexto

Raspagem de páginas do Mercado Livre gerava bloqueios anti-robô e risco de banimento do app.

## Decisão

Usar só APIs oficiais com Bearer token; tratar desafio anti-robô e captcha como inconclusivo e suspender GETs públicos por 15 min (`docs/rules/marketplaces-oauth.md:7-10`).

## Alternativas descartadas

- **Raspagem com contorno de captcha:** viola termos dos marketplaces e é frágil.

## Consequências

- Só aparecem produtos de catálogo com oferta ativa no Mercado Livre.
- Busca depende do token Master e de cotas.
