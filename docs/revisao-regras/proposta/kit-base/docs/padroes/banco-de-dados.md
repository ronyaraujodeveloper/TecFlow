# Banco de dados e EF Core (base universal)

> Provider, nomes de projeto, comandos e volumes do projeto ficam em `docs/projeto/arquitetura.md` e `docs/projeto/ambientes-docker.md`.

## 1. Migrations

- DEVE: nome em PascalCase descrevendo a mudança (`AddProductTags`, `RenameOrderStatusColumn`). Motivo: o nome vira histórico.
- DEVE: revisar o arquivo gerado **e** o SQL (`dotnet ef migrations script <anterior> <nova>`) antes do commit. Gerador erra em renomeação (vira drop + add).
- NÃO DEVE: editar, renomear ou apagar migration já aplicada em qualquer ambiente compartilhado (homologação ou produção). Correção é sempre uma migration nova. Verificar: permissão `ask` do Claude Code na pasta de migrations; revisão.
- DEVE: marcar no corpo do commit a migration **destrutiva** (drop de coluna/tabela, mudança de tipo com perda, `NOT NULL` sem default) e fazer em duas etapas quando houver dado: 1) nova estrutura + cópia; 2) remoção num deploy posterior.
- DEVE: aplicar em produção por script idempotente (`--idempotent`) gerado e revisado, depois do backup.
- NÃO DEVE: aplicar migration automaticamente na subida da aplicação em produção (`Database.Migrate()` no `Program.cs`).
- DEVE: uma migration por mudança lógica; não misture assuntos.

## 2. Convenções de modelo

- DEVE: tabela no plural e coluna com o nome da propriedade (padrão do EF), salvo legado documentado.
- DEVE: data/hora em UTC (`datetime2` no SQL Server). A conversão para fuso local é só na tela.
- DEVE: texto com tamanho máximo definido (`HasMaxLength`) quando tiver limite natural (e-mail, documento, código).
- DEVE: dinheiro em `decimal(18,2)` (ou precisão definida), nunca `float`/`double`.
- DEVE: entidade com `CreatedAt` e `UpdatedAt` preenchidos centralmente (interceptor ou `SaveChanges`), não à mão em cada serviço.
- RECOMENDADO: `CreatedBy`/`UpdatedBy` só em entidades administrativas e financeiras.
- Soft delete: NÃO DEVE ser padrão. Use só quando houver motivo (auditoria, restauração), com filtro global e índice que inclua a coluna.

## 3. Transações e concorrência

- DEVE: operação que altera mais de uma tabela de forma dependente roda numa transação (um `SaveChanges` já é transacional; várias chamadas exigem `BeginTransaction`).
- DEVE: saldo, estoque, crédito e contador usam controle de concorrência: token (`[Timestamp] RowVersion` / `IsConcurrencyToken`) com retry, ou atualização atômica (`ExecuteUpdate(x => x.Saldo - valor)` com condição `Saldo >= valor`). Motivo: duas requisições simultâneas não podem gastar o mesmo saldo.
- DEVE: job periódico que pode rodar em paralelo consigo mesmo (renovação, cobrança) ter trava (lock de aplicação no banco ou fila única).

## 4. Seeds

- DEVE: dado de referência (tabelas fixas, catálogo de menus) por seed idempotente ou na própria migration.
- NÃO DEVE: dado de teste ou de demonstração em migration. Use scripts separados marcados "não rodar em produção".

## 5. Backup e restauração

- DEVE: backup automático diário do banco e das chaves de criptografia, guardado fora do servidor, com retenção definida.
- DEVE: testar restauração pelo menos uma vez por trimestre e registrar a data.
- DEVE: backup manual antes de toda migration em produção.
