# Logs, observabilidade e operação (base universal)

## 1. Logs estruturados

- DEVE: usar message template com propriedades nomeadas: `_logger.LogInformation("Order {OrderId} invoiced", orderId)`. NÃO DEVE: interpolar (`$"Order {orderId}"`). Motivo: busca e agregação por propriedade. Verificar: analisador CA2254.
- DEVE: mensagens de log em inglês (`codigo-csharp.md` §1).
- DEVE: níveis com significado fixo:
  - `Error`: falhou e alguém precisa agir (exceção não tratada, integração fora, job abortado).
  - `Warning`: falhou mas o sistema se recuperou, ou situação suspeita (retry, token recusado, acesso negado, tentativa de login falha).
  - `Information`: evento de negócio relevante (pedido criado, conta conectada, job concluído com totais).
  - `Debug`: detalhe para diagnóstico, desligado em produção.
- NÃO DEVE: `Information` dentro de laço por item, nem log de corpo inteiro de requisição/resposta em produção.

## 2. O que nunca logar

Senha, hash, token (acesso, refresh, id_token, API key), `client_secret`, código OAuth, `code_verifier`, cartão, CVV, documento (CPF/CNPJ) completo, URL com credencial na query, corpo de resposta de endpoint de token. E-mail e telefone só mascarados (`j***@d***.com`).

- DEVE: handler de log de HTTP externo remover query e cabeçalhos sensíveis antes de registrar.

## 3. Correlação

- DEVE: cada requisição ter um id de correlação (o `TraceIdentifier`/`Activity.TraceId`) presente em todo log do escopo e devolvido no `ProblemDetails.traceId`.
- DEVE: job em segundo plano abrir escopo de log com o nome do job e o id da execução.

## 4. Saúde e métricas

- DEVE: health checks em cada host: `/health/live` (processo vivo) e `/health/ready` (banco, fila e dependências essenciais). Usados pelo `healthcheck` do Docker.
- NÃO DEVE: health check ou `/metrics` com dado sensível acessível pela internet; restrinja no proxy.
- DEVE: `BackgroundService` que falha registrar `Error` e ficar visível (health check degradado ou alerta). NÃO DEVE: morrer em silêncio.

## 5. Alertas (proporcional ao porte)

- DEVE: pelo menos um alerta para cada um destes casos, enviado para um canal que eu leio (e-mail ou Telegram):
  1. aplicação fora do ar (health check falhando);
  2. pico de erros 5xx;
  3. job de cobrança/renovação falhou;
  4. backup não rodou;
  5. certificado TLS perto de vencer.
- RECOMENDADO: um monitor externo simples (ex.: verificação HTTP a cada 5 min) em vez de montar Alertmanager próprio.
