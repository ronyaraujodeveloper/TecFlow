# 0009. Quarto objeto por entidade: `[Nome]ReadDto` para os dados devolvidos

- **Status:** Aceita
- **Data:** 2026-11-25
- **Substitui:** 0002

## Contexto

A ADR 0002 definiu três objetos por entidade, com `ResponseDto.Data` tipado como a própria entidade (`docs/rules/backend-architecture.md:32`). Na prática, a API devolveu `UserAccount` com `PasswordHash` e tokens (`Ofertio.Business/Dto/UserAccountResponseDto.cs:9-10`; auditoria de 25/11, S8 e P2). Toda coluna nova da entidade passa a sair na API sem ninguém decidir.

## Decisão

Cada entidade passa a ter quatro objetos: `[Nome]Filter` (entrada de GET), `[Nome]Dto` (entrada de POST/PUT), `[Nome]ReadDto` (dados devolvidos) e `[Nome]ResponseDto` (envelope de sucesso com `Data: [Nome]ReadDto?` e `DataList: List<[Nome]ReadDto>?`). O `ReadDto` é montado por projeção (`Select`) ou por um mapeamento único. `ResponseDto` não pode ter propriedade do tipo de entidade; um teste de arquitetura garante isso.

## Alternativas descartadas

- **Manter três objetos e marcar campos sensíveis com `[JsonIgnore]` na entidade:** depende de lembrar o atributo em cada campo novo; continua serializando tudo o que não foi marcado e carrega navegações.
- **Projeção anônima montada no serviço:** sem tipo, não aparece no Swagger, não é testável por contrato e não é compartilhada com a `SharedUi`.
- **Reaproveitar o `[Nome]Dto` (escrita) como leitura:** mistura campos que o cliente envia com campos calculados (datas, nomes resolvidos), e reabre o risco de mass assignment.

## Consequências

- Um arquivo a mais por entidade; o `ReadDto` pode ser um `record` curto.
- Leituras ficam mais leves (projeção só com as colunas usadas), alinhado à regra P1 de `docs/padroes/performance.md`.
- Migração gradual: `UserAccountResponseDto` primeiro; os demais quando o controller for tocado. Enquanto isso, o teste de arquitetura mantém uma lista de exceções que só diminui.
- Regras: `docs/projeto/arquitetura.md` §3.
