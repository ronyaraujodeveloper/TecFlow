---
description: Audita a performance do escopo indicado contra docs/padroes/performance.md, com evidências e sem alterar nada
argument-hint: [escopo: pasta, serviço, tela ou "tudo"]
---

Escopo: $ARGUMENTS (vazio = repositório inteiro).

## Regras desta auditoria

- Somente leitura. Não altere arquivos nem rode carga contra homologação ou produção.
- Evidência `arquivo:linha` em toda afirmação.
- Respeite a seção "Quando NÃO otimizar" de `docs/padroes/performance.md`: não recomende Redis, CDN, fila nova ou paralelismo sem gargalo demonstrado.

## Passos

1. Mapeie os caminhos quentes do escopo: listagens, buscas, jobs periódicos, chamadas externas, telas mais usadas.
2. Percorra P1 a P17 de `docs/padroes/performance.md`. Status: OK / PARCIAL / ERRADO / N.A. / NÃO VERIFICÁVEL (diga o que seria preciso medir).
3. Procure especificamente:
   - consulta dentro de laço (`foreach` com `await _db`/`FirstOrDefaultAsync`/`AnyAsync`);
   - listagem sem `Take`/paginação;
   - leitura sem `AsNoTracking` em repositório;
   - `SaveChanges` em laço;
   - `HttpClient` sem timeout ou sem política de retry;
   - trabalho lento (e-mail, mídia) dentro da requisição;
   - `Channel.CreateUnbounded`.
4. Para cada achado: impacto estimado (alto/médio/baixo), como medir antes de corrigir, correção proposta e esforço.

## Entrega

Responda no chat com nota 0–10, os 5 pontos de maior impacto e a matriz completa. Gere arquivo em `docs/auditoria/` só se o usuário pedir.
