---
description: Planeja e implementa uma feature de ponta a ponta seguindo a arquitetura e a Definição de Pronto do projeto
argument-hint: <descrição da feature>
---

Feature pedida: $ARGUMENTS

Siga estas etapas, nesta ordem. Não pule a etapa 2.

## 1. Entender

- Leia o `CLAUDE.md` e, em `docs/projeto/arquitetura.md`, a seção "Nova feature de ponta a ponta".
- Leia os padrões da área: `docs/padroes/api.md`, `docs/padroes/testes.md` e, se houver tela, `docs/projeto/frontend-ui.md`.
- Procure uma feature parecida já existente e use-a como referência (cite os arquivos).
- Liste o que não está claro e só o dono pode decidir (regra de negócio, permissão por plano, seleção única/múltipla em combo etc.).

## 2. Planejar e parar

Mostre um plano com:
- arquivos a criar e alterar, por camada;
- endpoint(s): rota, verbo, autorização (`[Authorize]`, política, `[RequiresAccess]` ou `[AllowAnonymous]` com justificativa) e checagem de dono do objeto;
- migration necessária (nome) e índices;
- testes: unitários, de autorização e E2E, se houver tela;
- documentação a atualizar;
- perguntas em aberto.

**Espere aprovação antes de escrever código.**

## 3. Implementar

- Siga a ordem das camadas descrita em `docs/projeto/arquitetura.md`.
- Escreva o teste junto com o código.
- Não altere nada fora do escopo; anote sugestões para o fim.

## 4. Verificar (Definição de Pronto)

- Rode o build e os testes (comandos no `CLAUDE.md`). Relate números reais.
- Percorra `docs/padroes/definicao-de-pronto.md` e marque cada item, dizendo como verificou.

## 5. Relatar

Arquivos alterados, testes criados, documentação atualizada, pendências e o que ficou fora do escopo.
