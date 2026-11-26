# Documentação (base universal)

## 1. O que existe e para quem

| Documento | Público | Conteúdo | NÃO contém |
|---|---|---|---|
| `README.md` | humanos | o que é o sistema, como rodar, como contribuir, mapa da documentação | instruções para IA, regras detalhadas, histórico |
| `CLAUDE.md` | IA (e humanos curiosos) | índice, comandos, regras essenciais, áreas sensíveis | tutorial, histórico, regras detalhadas |
| `docs/padroes/` | ambos | regras universais (kit base) | particularidades do projeto |
| `docs/projeto/` | ambos | regras do domínio, arquitetura, integrações, UI, ambientes | roteiros passo a passo |
| `docs/adr/` | ambos | decisões de arquitetura: contexto, decisão, alternativas, consequências | regras do dia a dia |
| roteiros (`EXECUCAO_*`, deploy) | humanos | passo a passo operacional | regras (só apontam para elas) |
| checklist/pendências | ambos | o que falta fazer | o que já foi feito |
| histórico | consulta | detalhe de entregas antigas | regras vigentes |

## 2. Regras

- DEVE: atualizar a documentação afetada **na mesma tarefa (mesma branch)** que muda o comportamento: regra, roteiro, diagrama, `.env.example`, comando no README. Motivo: documentação atrasada vira regra falsa. Verificar: item da DoD; `/atualizar-docs`.
- DEVE: cada regra em um único arquivo. Os outros apontam com link, sem reescrever.
- DEVE: registrar ADR quando a decisão: troca tecnologia, cria dependência nova de infraestrutura, define padrão que outras features vão seguir, ou aceita conscientemente um risco.
- NÃO DEVE: documentar o que o código ou o `git log` já dizem (lista de arquivos alterados, nomes de métodos óbvios).
- DEVE: roteiro operacional testado de ponta a ponta na última mudança; o roteiro diz a data em que foi validado.
- DEVE: documento arquivado ir para `docs/arquivo/` com aviso no topo "congelado, não é regra".

## 3. ADR

- Arquivo `docs/adr/NNNN-titulo-curto.md`, numeração sequencial, modelo em `0000-modelo.md`.
- Status: `Proposta`, `Aceita`, `Substituída por NNNN`. ADR aceita não é editada; uma nova a substitui.
- Até uma página.
