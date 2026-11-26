# Kit base de regras (.NET + Claude Code)

> Conjunto reaproveitável em qualquer projeto .NET meu. **Fonte única: o repositório do Ofertio** (decisão de 25/11/2026).
> Destino no repositório final: `docs/kit-base/` guarda só este `LEIA-ME.md`, o `CLAUDE.modelo.md` e o `settings.base.json`. Os demais arquivos do kit ficam nos lugares de uso, na raiz do Ofertio. Assim nenhum padrão existe em duas cópias.
> Para começar um projeto novo, copie os arquivos da tabela abaixo para a raiz dele e preencha só o que é específico (`CLAUDE.md`, `README.md`, `docs/projeto/`, `docs/adr/`).
> Melhoria num padrão universal é feita no Ofertio. Os projetos derivados copiam de novo quando quiserem atualizar.

## Arquivos do kit (caminho no repositório do Ofertio)

| Arquivo | Para quê | Tipo | Ajuste ao copiar |
|---|---|---|---|
| `docs/padroes/codigo-csharp.md` | Convenções C#/.NET, async, DI, exceções | regra | — |
| `docs/padroes/seguranca.md` | Base OWASP com "como verificar" | regra | — |
| `docs/padroes/performance.md` | O que é obrigatório e quando **não** otimizar | regra | — |
| `docs/padroes/banco-de-dados.md` | EF Core, migrations, concorrência, backup | regra | — |
| `docs/padroes/api.md` | Rotas, códigos HTTP, erros, paginação | regra | — |
| `docs/padroes/testes.md` | O que é obrigatório testar e como | regra | — |
| `docs/padroes/observabilidade.md` | Logs, correlação, health checks, alertas | regra | — |
| `docs/padroes/git.md` | Branches, commits, merge local, tags (sem PR obrigatório) | regra | — |
| `docs/padroes/definicao-de-pronto.md` | Checklist de conclusão | regra | — |
| `docs/padroes/trabalho-com-ia.md` | Como a IA trabalha e o que nunca faz sem autorização | regra | — |
| `docs/padroes/documentacao.md` | O que documentar e onde | regra | — |
| `docs/adr/0000-modelo.md` | Modelo de ADR | modelo | — |
| `docs/kit-base/CLAUDE.modelo.md` | Esqueleto do `CLAUDE.md` | modelo | renomear para `CLAUDE.md` na raiz |
| `docs/kit-base/settings.base.json` | Permissões padrão do Claude Code | automação | renomear para `.claude/settings.json` |
| `.editorconfig` | Estilo e severidade dos analisadores | automação | apagar o bloco `[*.ps1]` se o projeto não usar PowerShell 5.1 |
| `Directory.Build.props` | Nullable, analisadores, warnings | automação | começar já com `TreatWarningsAsErrors=true` e `AnalysisLevel` recomendado |
| `.github/workflows/ci.yml` | Build, testes, formato, vulnerabilidades, segredos | automação | `SOLUTION` e `TEST_PROJECTS` |
| `.claude/commands/*.md` | `/nova-feature`, `/revisar-pr`, `/auditoria-seguranca`, `/auditoria-performance`, `/nova-migration`, `/atualizar-docs` | automação | — |

**Não fazem parte do kit** (são do Ofertio): `CLAUDE.md`, `README.md`, `docs/projeto/`, `docs/adr/0001+`, `.claude/settings.json`, `.claude/hooks/`, `.gitleaks.toml`.

## Como adotar num projeto novo

1. Copie os arquivos da tabela. Faça os ajustes da última coluna.
2. Preencha no `CLAUDE.md`: stack, comandos, camadas, áreas sensíveis e os links de `docs/projeto/`.
3. Registre as decisões iniciais em `docs/adr/` (banco, autenticação, hospedagem).
4. Confira que o CI roda em todo push (GitHub Actions habilitado no repositório).

## Convenção de escrita das regras

- **DEVE / NÃO DEVE / RECOMENDADO** (RFC 2119). DEVE é verificado em revisão ou por automação; RECOMENDADO admite exceção sem justificativa escrita.
- Uma regra por item, imperativa, com o motivo em uma linha (`Motivo:`) e a verificação (`Verificar:`) quando existir.
- Regra que aparece no `CLAUDE.md` não é repetida nos documentos detalhados; eles trazem exceções, exemplos e verificação.
