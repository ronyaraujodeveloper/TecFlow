---
description: Revisa o diff da branch atual (ou da indicada) contra a Definição de Pronto, segurança e padrões do projeto
argument-hint: [branch base, padrão main]
---

Base de comparação informada: "$ARGUMENTS". Se estiver vazia, use `main`. Abaixo, `<base>` significa essa branch.

## Contexto (calculado contra a main; refaça com `<base>` se for outra)

!`git log --oneline main..HEAD`

!`git diff --stat main...HEAD`

## O que fazer

1. Leia o diff completo (`git diff <base>...HEAD`). Leia também o código ao redor quando o diff sozinho não bastar.
2. Avalie, nesta ordem, citando `arquivo:linha`:
   1. **Correção:** bugs, casos de borda, nulos, concorrência, async.
   2. **Segurança** (`docs/padroes/seguranca.md`): endpoint sem autorização, `[AllowAnonymous]` sem justificativa, falta de checagem de dono do objeto, entidade exposta, valor monetário vindo do cliente, webhook sem assinatura/idempotência, segredo ou dado sensível em log ou arquivo versionado.
   3. **Banco** (`docs/padroes/banco-de-dados.md`): migration aplicada alterada, migration destrutiva sem plano, índice faltando, N+1.
   4. **Testes** (`docs/padroes/testes.md`): comportamento novo sem teste, teste alterado só para passar, endpoint novo sem teste de autorização.
   5. **Padrões do projeto e de API** (`CLAUDE.md`, `docs/projeto/`, `docs/padroes/api.md`): erro devolvido com HTTP 200, código HTTP errado, entidade em resposta, rota nova fora do padrão do projeto.
   6. **Documentação:** o que mudou de comportamento e não foi documentado.
3. Rode build e testes e informe o resultado.

## Formato da resposta

- Veredito: **Aprovar**, **Aprovar com ressalvas** ou **Pedir mudanças**.
- Achados em tabela: `Severidade (Bloqueante/Importante/Sugestão) | arquivo:linha | problema | sugestão`.
- Checklist da Definição de Pronto com ✅/❌/N.A.

Não altere arquivos. Só revise.
