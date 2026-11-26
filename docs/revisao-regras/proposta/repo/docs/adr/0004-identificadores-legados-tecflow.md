# 0004. Identificadores legados TecFlow não são renomeados

- **Status:** Aceita (registro retroativo de decisão já em vigor)
- **Data:** 2026-11-25

## Contexto

O produto se chamava TecFlow. Vários identificadores estão gravados em dados cifrados, URLs de afiliado e credenciais.

## Decisão

Manter `ApplicationName="TecFlow"`, purpose `TecFlow.MarketplaceTokens.v1`, migration `InitialTecFlow`, prefixos `tecflow-*` (`docs/rules/encoding-standards.md:39-47`).

## Alternativas descartadas

- **Renomear com migração de dados:** exigiria recriptografar todos os tokens e refazer links de afiliado publicados.

## Consequências

- Nomes antigos convivem com a marca Ofertio; a lista fica em `docs/projeto/encoding-e-legado.md` §4.
