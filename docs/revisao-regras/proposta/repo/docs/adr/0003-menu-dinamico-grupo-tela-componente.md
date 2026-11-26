# 0003. Menu dinâmico e permissão por plano em árvore Grupo → Tela → Componente

- **Status:** Aceita (registro retroativo de decisão já em vigor)
- **Data:** 2026-11-25

## Contexto

Pedidos de "esconder X no plano Y" geravam flags avulsas e `if` por plano espalhados pelas telas.

## Decisão

Todo item controlável por plano é um `MenuItem` (Group/Page/Component/Feature) com cascata de negação, checado na tela (`AccessGate`) e no servidor (`[RequiresAccess]`) (`docs/rules/backend-architecture.md:66-78`).

## Alternativas descartadas

- **Flags por plano no código:** espalha regra e exige deploy para mudar.
- **Permissão só na tela:** não protege a API.

## Consequências

- Tela nova exige registro no catálogo.
- O filtro respondia 200 com `ACCESS_DENIED` porque o cliente tratava 403 como sessão expirada. A ADR 0010 troca para 403 com `ProblemDetails`, junto com o ajuste do cliente.
