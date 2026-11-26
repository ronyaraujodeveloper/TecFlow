# 0006. Desenvolvimento e homologação em Docker espelhando a produção

- **Status:** Aceita (registro retroativo de decisão já em vigor)
- **Data:** 2026-11-25

## Contexto

A homologação em IIS divergia da produção Docker e escondia erros de configuração.

## Decisão

Os três ambientes usam o mesmo `docker-compose.yml` da produção; diferenças só em override e registradas na tabela de cada roteiro (`docs/rules/ambientes-docker.md`).

## Alternativas descartadas

- **IIS local:** arquivado (`docs/arquivo/EXECUCAO_HOMOLOGACAO_IIS.md`).
- **Compose separado por ambiente:** diverge com o tempo.

## Consequências

- Máquina de desenvolvimento precisa de Docker com ~5 GB.
- Regras herdadas do IIS foram revistas em 25/11/2026: BOM só em `.ps1`; subaplicação `/api` removida das regras.
