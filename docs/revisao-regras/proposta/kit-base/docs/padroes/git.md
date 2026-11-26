# Fluxo Git (base universal)

> Pensado para quem trabalha sozinho: sem pull request obrigatório. A revisão acontece antes do merge local com `/revisar-pr`, e o CI roda em todo push. Se o projeto ganhar mais pessoas, ative PR e proteção da `main` (§5).

## 1. Branches

- DEVE: `main` sempre implantável.
- DEVE: uma branch por tarefa, criada a partir da `main` atualizada, no formato `tipo/descricao-curta` (ex.: `feat/checkout-pix`, `fix/seguranca-a-usuarios-anonimos`). Os tipos são os mesmos dos commits.
- RECOMENDADO: branch de vida curta (até ~3 dias). Mudança pequena e segura (documentação, ajuste de texto) pode ir direto na `main`.

## 2. Commits

- DEVE: Conventional Commits: `tipo(escopo): descrição no imperativo, em português`. Tipos: `feat`, `fix`, `docs`, `refactor`, `test`, `perf`, `build`, `ci`, `chore`, `style`. Quebra de compatibilidade: `!` depois do tipo e rodapé `BREAKING CHANGE:`.
- DEVE: um commit = uma mudança coerente que compila.
- DEVE: informar no corpo do commit o que um revisor precisaria saber: `[AllowAnonymous]` novo e o motivo, migration destrutiva, configuração nova.
- NÃO DEVE: commitar segredo, `.env`, backup, saída de teste ou arquivo temporário. Verificar: gitleaks no CI (e, opcionalmente, em pre-commit); `.gitignore`.

## 3. Antes de levar para a `main`

1. Rodar `/revisar-pr` (revisão do diff da branch contra a Definição de Pronto, segurança e padrões).
2. Conferir a Definição de Pronto (`definicao-de-pronto.md`).
3. Merge local: `git switch main`, `git pull`, `git merge --squash <branch>` (ou `--ff-only`, se os commits já estiverem limpos) e `git push`.
4. Conferir se o CI da `main` ficou verde. Se ficou vermelho, corrigir antes de qualquer outra tarefa.
5. Apagar a branch.

## 4. Versão e changelog

- DEVE: tag `vAAAA.MM.DD[-n]` no commit que foi para produção. Motivo: saber exatamente o que está rodando e para onde voltar.
- O changelog é o `git log` entre tags. NÃO DEVE: manter lista manual de arquivos alterados.

## 5. Quando houver mais de uma pessoa

Ative: PR obrigatório, template de PR com o checklist da Definição de Pronto, proteção da `main` exigindo o status `ci` verde, bloqueio de force push.
