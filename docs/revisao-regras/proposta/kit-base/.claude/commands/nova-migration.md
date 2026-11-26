---
description: Gera, revisa e documenta uma migration do EF Core seguindo docs/padroes/banco-de-dados.md
argument-hint: <NomeEmPascalCase>
---

Migration pedida: $ARGUMENTS

1. Confirme que o nome está em PascalCase e descreve a mudança. Se não estiver, proponha um nome e pergunte.
2. Confira que as mudanças de modelo (entidade, configuração no `OnModelCreating`) já estão feitas e compilam.
3. Gere a migration com o comando de migrations do `CLAUDE.md` (seção "Comandos"). Use exatamente o projeto, o startup project e o contexto indicados lá.
4. Revise o arquivo gerado e responda:
   - Há `DropColumn`, `DropTable`, `AlterColumn` com perda ou `nullable: false` sem `defaultValue`? Se sim, é **destrutiva**: pare e proponha o plano em duas etapas.
   - O gerador transformou uma renomeação em drop + add? Se sim, troque por `RenameColumn`/`RenameTable`.
   - Toda consulta nova tem índice? (procure os `Where` novos do diff).
   - Dinheiro está em `decimal` com precisão definida? Datas em UTC?
5. Gere o SQL para revisão: `dotnet ef migrations script <migration anterior> $ARGUMENTS` (mesmos parâmetros de projeto e contexto). Mostre só o resumo do SQL.
6. **Não rode `dotnet ef database update`.** Diga ao usuário o comando e em que ambiente aplicar, e lembre do backup antes da produção.
7. Atualize o roteiro de deploy (`docs/EXECUCAO_PRODUCAO.md` ou o checklist) com a migration pendente.
8. Rode build e testes.

Nunca edite uma migration que já existia antes desta tarefa.
