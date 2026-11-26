---
description: Confere o diff atual e atualiza a documentação afetada (regras, roteiros, diagramas, .env.example, checklist)
---

## Diff atual

!`git status --short`

!`git diff --stat HEAD`

## O que fazer

1. Leia `docs/padroes/documentacao.md` (tabela "O que existe e para quem").
2. Para cada mudança do diff, responda se ela afeta:
   - uma regra em `docs/padroes/` ou `docs/projeto/` (regra nova, alterada ou que deixou de ser verdade);
   - um roteiro (`docs/EXECUCAO_*.md`, `docs/CONFIGURACAO_PRODUCAO_DOCKER.md`);
   - um diagrama (`docs/DIAGRAMAS_ARQUITETURA.md`): novo controller → serviço, entidade, integração ou referência entre projetos;
   - configuração (`.env.example`, chave nova em `appsettings.json`, cofre);
   - o checklist (`docs/CHECKLIST_ATIVO.md`: item concluído vai para o histórico);
   - uma decisão que merece ADR (`docs/adr/`).
3. Mostre a lista "documento → o que mudar" e **peça confirmação** antes de editar.
4. Edite mantendo cada regra em um único lugar: se a regra já existe em outro arquivo, aponte com link em vez de reescrever.
5. Ao terminar, liste os arquivos de documentação alterados.
