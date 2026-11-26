# Performance (base universal)

> Base: checklist de 31 itens. A ordem é: **medir, depois otimizar**. As regras DEVE abaixo são baratas e evitam os problemas mais comuns; o resto só com necessidade medida.

## 1. Banco de dados (EF Core)

| # | Regra | Verificar |
|---|---|---|
| P1 | DEVE projetar para DTO com `Select` em leituras de listagem e detalhe exposto pela API. Motivo: menos colunas, sem vazar entidade. | Revisão; teste de contrato da resposta. |
| P2 | DEVE usar `AsNoTracking()` em consultas só de leitura (ou `QueryTrackingBehavior.NoTracking` no contexto de leitura). | Busca em repositórios na revisão (`/revisar-pr`). |
| P3 | NÃO DEVE consultar o banco dentro de laço (N+1). Carregue em lote (`Where(x => ids.Contains(x.Id))`) ou com `Include` consciente. | Revisão; log de SQL em Development. |
| P4 | DEVE paginar toda listagem que cresce com o uso (máximo definido no projeto). Exceção: catálogos pequenos e fixos, documentados. | Teste do limite máximo. |
| P5 | DEVE criar índice para coluna usada em `WHERE`/`JOIN` frequente ou em job periódico, junto da migration que introduz a consulta. | Revisão da migration. |
| P6 | NÃO DEVE chamar `SaveChanges` por item em laço; agrupe ou use `ExecuteUpdate`/`ExecuteDelete`. | Revisão. |

## 2. Chamadas externas

| # | Regra | Verificar |
|---|---|---|
| P7 | DEVE usar `IHttpClientFactory` com timeout explícito vindo de configuração. | Registro em `Add*Clients`. |
| P8 | DEVE ter retry com backoff só para erro transitório (5xx, timeout, 429) e circuit breaker em integração que costuma cair. NÃO DEVE repetir POST não idempotente. | Política Polly revisada. |
| P9 | DEVE tirar da requisição o que é lento e não precisa de resposta imediata (e-mail, push, processamento de mídia): fila ou `BackgroundService` com canal **limitado**. | Revisão; `Channel.CreateBounded`. |
| P10 | RECOMENDADO: chamadas independentes em paralelo (`Task.WhenAll`) com limite de concorrência quando o destino tem cota. | Revisão. |

## 3. Servidor HTTP

| # | Regra | Verificar |
|---|---|---|
| P11 | DEVE ativar compressão (no proxy, ex. `encode gzip zstd` no Caddy, ou `AddResponseCompression`). | `curl -H "Accept-Encoding: gzip" -I`. |
| P12 | DEVE servir estáticos com cache longo e versão na URL (`?v=` ou hash). | Inspeção do `Cache-Control`. |
| P13 | RECOMENDADO: `IMemoryCache` para dados lidos com frequência e que mudam pouco (permissões, catálogos), com expiração definida. | Revisão. |

## 4. Frontend

| # | Regra | Verificar |
|---|---|---|
| P14 | DEVE mostrar estado de carregamento (spinner ou skeleton) em operação acima de ~300 ms e desabilitar o botão durante a operação. | Revisão / E2E. |
| P15 | DEVE aplicar debounce (~300 ms) em busca que dispara requisição a cada tecla. | Revisão. |
| P16 | DEVE redimensionar imagens enviadas no servidor e usar `loading="lazy"` em listas. | Revisão. |
| P17 | RECOMENDADO (Blazor): evitar renderização desnecessária com `@key` em listas e `ShouldRender` em componentes pesados. | Profiling quando houver lentidão. |

## 5. Quando NÃO otimizar

- NÃO DEVE introduzir Redis, cache distribuído, fila externa nova, CQRS, read replica ou CDN sem medição mostrando o gargalo (latência p95, CPU, cota) e sem ADR.
- NÃO DEVE trocar código legível por micro-otimização sem benchmark (BenchmarkDotNet) que mostre ganho relevante.
- NÃO DEVE paralelizar acesso ao mesmo `DbContext` (não é thread-safe).
- Preparar para várias instâncias (estado fora do processo, uploads em storage compartilhado, locks distribuídos) só quando houver plano de escalar. Até lá, registre os impedimentos conhecidos num ADR.

## 6. Como medir

- Log de duração por requisição (middleware ou OpenTelemetry) e por chamada externa.
- Em Development, log de SQL do EF Core (`LogTo` / `EnableSensitiveDataLogging` só local).
- Antes de otimizar, registre no commit o número de antes e o de depois.
