# Design de API HTTP (base universal)

> O envelope de resposta e o idioma das rotas de cada projeto ficam em `docs/projeto/arquitetura.md`. Aqui ficam as regras que valem para qualquer API.

## 1. Rotas

- DEVE: prefixo `api/`, recurso no plural, minúsculo e em kebab-case (`api/sales-orders`, `api/sales-orders/{id:int}`).
- DEVE: um único idioma para rotas novas, definido no projeto. NÃO DEVE: misturar idiomas dentro do mesmo recurso.
- DEVE: restrição de tipo na rota (`{id:int}`, `{id:guid}`).
- DEVE: ação que não é CRUD como sub-recurso com verbo (`POST api/orders/{id}/cancel`).
- NÃO DEVE: `[Route("[controller]")]` (o nome da classe vira contrato e muda com refatoração).

## 2. Verbos e códigos HTTP

| Situação | Código |
|---|---|
| Leitura com sucesso | 200 |
| Criação | 201 com `Location` (ou 200 com o envelope, se o projeto decidir) |
| Sucesso sem corpo | 204 |
| Entrada inválida | 400 com detalhes por campo |
| Sem autenticação | 401 |
| Autenticado sem permissão | 403 |
| Recurso inexistente **ou de outro dono** | 404 (não revela que existe) |
| Conflito de concorrência / duplicidade | 409 |
| Regra de negócio violada com entrada válida | 422 |
| Integração externa fora / lenta | 502 / 504 |
| Limite de taxa | 429 |
| Erro inesperado | 500 com `ProblemDetails` sem detalhes internos |

- NÃO DEVE: responder erro com HTTP 200 (ex.: `{ "status": false }`). O código HTTP é o primeiro sinal do resultado para o cliente, o proxy e o monitoramento.
- DEVE: GET sem efeito colateral. PUT e DELETE idempotentes.
- DEVE: POST que cria cobrança, pedido, crédito ou envio aceitar chave de idempotência (cabeçalho `Idempotency-Key` ou id do evento) quando o cliente puder repetir a chamada.

## 3. Contratos

- DEVE: entrada e saída por DTO. NÃO DEVE: expor entidade do banco na resposta.
- DEVE: datas em ISO 8601 UTC (`2026-11-25T14:00:00Z`).
- DEVE: enum serializado como texto quando o cliente for externo.
- NÃO DEVE: remover ou renomear campo de resposta usado por cliente publicado (Mobile) sem versão nova do endpoint.

## 4. Erros

- DEVE: todo erro sai como `ProblemDetails` (RFC 9457) com as extensões `errorCode` (constante estável que o cliente pode testar) e `traceId`. A tabela de `errorCode` fica no projeto.
- DEVE: erro inesperado sem `exception.Message` fora de Development.
- DEVE: o serviço devolve resultado tipado com código de erro; a tradução para HTTP fica num único ponto do controller (extensão ou classe base).
- DEVE: erro de validação lista os campos (`ValidationProblemDetails` ou equivalente do envelope).

## 5. Listagens

- DEVE: paginação com `page`/`pageSize` (ou cursor), tamanho máximo fixo no servidor e total ou indicador de próxima página na resposta.
- DEVE: filtros por query string em objeto `Filter` tipado; ordenação por lista permitida (nunca nome de coluna livre).

## 6. Documentação

- DEVE: OpenAPI (Swagger) gerado em Development e Homologação, com `[ProducesResponseType]` nos endpoints públicos e de integração.
- NÃO DEVE: Swagger acessível publicamente em produção.
- RECOMENDADO: versionar (`api/v1/`) só quando houver cliente externo ou app publicado que não atualiza junto.
