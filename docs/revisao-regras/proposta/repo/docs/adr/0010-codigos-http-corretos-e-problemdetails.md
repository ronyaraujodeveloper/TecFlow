# 0010. Códigos HTTP corretos e `ProblemDetails` para todo erro

- **Status:** Aceita
- **Data:** 2026-11-25

## Contexto

Conviviam três formatos de erro: envelope `ResponseDto` com `Status=false` e HTTP 200 (`docs/rules/backend-architecture.md:32,38`), `ProblemDetails` só para exceções (`Ofertio.API/Middlewares/ExceptionHandlingMiddleware.cs:80`) e `{ status: false, errorCode: "ACCESS_DENIED" }` com HTTP 200 (`docs/rules/backend-architecture.md:77`; `Ofertio.API/Security/RequiresAccessAttribute.cs:16`). O HTTP 200 em erro escondia falhas do Caddy, dos logs e de qualquer monitoramento. O motivo do 200 no `ACCESS_DENIED` era o cliente tratar todo 403 como sessão expirada.

## Decisão

Toda resposta usa o código HTTP correto (200, 201, 204, 400, 401, 403, 404, 409, 422, 429, 500, 502, 504). Todo erro sai como `ProblemDetails` (RFC 9457) com as extensões `errorCode` (constante em `UPPER_SNAKE_CASE`) e `traceId`, e o `detail` traz a mensagem em português. O serviço devolve resultado tipado com código de erro, e o controller traduz para HTTP num único ponto. O cliente trata 401 como sessão expirada e 403 como "sem acesso". A tabela completa fica em `docs/projeto/arquitetura.md` §4.

## Alternativas descartadas

- **Manter o envelope com HTTP 200 para falha de negócio:** é o padrão atual, mas esconde erro do monitoramento e obriga todo cliente a ler o corpo para saber se deu certo.
- **Híbrido (negócio em 200, só exceção em 500):** mantém dois jeitos de sinalizar erro e a ambiguidade de qual usar.

## Consequências

- O cliente HTTP da `SharedUi` (e o Mobile, se estiver ativo) precisa entender `ProblemDetails` e distinguir 401 de 403 **antes** de os endpoints mudarem. A migração vai controller por controller, com o cliente aceitando os dois formatos durante a transição.
- `Status` do `ResponseDto` passa a ser sempre `true`; fica por compatibilidade.
- Monitoramento passa a contar erros pelo código HTTP (base para o alerta de pico de 4xx/5xx em `docs/padroes/observabilidade.md` §5).
- Testes de controller passam a afirmar o código HTTP, não só o `Status` do corpo.
