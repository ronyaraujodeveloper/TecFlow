# Definição de Pronto (DoD)

> Toda tarefa, feita por pessoa ou por IA, só está concluída quando todos os itens aplicáveis estão marcados. O comando `/revisar-pr` confere esta lista antes do merge.

## Sempre

- [ ] `dotnet build` da solução sem erros e **sem warnings novos**.
- [ ] `dotnet test` (unitários e integração) passando localmente; CI verde no push.
- [ ] Teste novo ou ajustado para o comportamento alterado (ver `testes.md` §1).
- [ ] Nenhum teste alterado só para passar.
- [ ] `dotnet format --verify-no-changes` sem diferenças.
- [ ] Nenhum segredo, `.env`, backup ou arquivo temporário no diff (gitleaks verde).
- [ ] Documentação afetada atualizada **na mesma tarefa** (mesma branch) (ver `documentacao.md`).
- [ ] Commit(s) em Conventional Commits.

## Quando aplicável

- [ ] **Endpoint novo ou alterado:** autorização definida (`[Authorize]`/política ou `[AllowAnonymous]` justificado), checagem de dono do objeto, DTO na entrada e na saída, paginação em listagem.
- [ ] **Dinheiro, crédito, saldo, plano ou webhook:** valor calculado no servidor, idempotência testada, concorrência tratada.
- [ ] **Migration:** SQL revisado, não edita migration aplicada, destrutiva sinalizada e em duas etapas, índice para consultas novas.
- [ ] **Configuração nova:** chave no `appsettings.json` só com valor não sensível; segredo documentado no `.env.example`/cofre; opção validada na subida.
- [ ] **Pacote novo:** aprovado, sem vulnerabilidade conhecida, licença compatível.
- [ ] **Tela nova ou alterada:** regras de UI do projeto e teste ponta a ponta quando houver interação.
- [ ] **Mudança de comportamento em produção:** passo no roteiro de deploy e forma de reverter.
