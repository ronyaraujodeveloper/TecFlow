# 0002. Três objetos por entidade (Filter, Dto, ResponseDto)

- **Status:** Substituída por 0009 (era: Aceita, registro retroativo)
- **Data:** 2026-11-25

## Contexto

DTOs de criação e atualização duplicados (Create/Update) tornavam cada entidade cara de manter.

## Decisão

Cada entidade tem só `[Nome]Filter` (GET, em `Ofertio.Database/Filter`), `[Nome]Dto` (POST/PUT) e `[Nome]ResponseDto` (envelope `Status`/`Descricao`/`Data`/`DataList`) (`docs/rules/backend-architecture.md:26-34`).

## Alternativas descartadas

- **Create/Update/Read DTOs separados:** mais arquivos e mapeamento duplicado.
- **Expor a entidade direto:** sem controle do que sai.

## Consequências

- O `Filter` morar em `Ofertio.Database` faz o `Business` e a `SharedUi` referenciarem a camada de banco (dívida conhecida).
- `Data` tipado com a entidade expôs campos sensíveis (`UserAccountResponseDto` com `PasswordHash`). Resolvido pela ADR 0009 (quarto objeto `[Nome]ReadDto`).
