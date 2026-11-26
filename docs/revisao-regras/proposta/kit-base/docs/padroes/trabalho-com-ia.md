# Trabalho com IA (Claude Code)

> Universal (kit base). O `CLAUDE.md` do projeto resume as regras essenciais e lista as áreas sensíveis específicas. Este arquivo detalha o processo e a configuração.
> Referência oficial: https://code.claude.com/docs/en/memory, `/settings`, `/permissions`, `/hooks`, `/skills`.

## 1. Ciclo de uma tarefa

1. **Entender.** Ler o `CLAUDE.md`, o documento de `docs/padroes/` ou `docs/projeto/` da área e o código envolvido. Se faltar informação que só o dono sabe, perguntar antes de codar.
2. **Planejar.** Tarefa com mais de um arquivo, mudança de contrato, migration, segurança ou pagamento: mostrar o plano (arquivos, passos, riscos, testes) e esperar aprovação.
3. **Mudar pouco.** Uma tarefa por vez, diff pequeno. Não refatorar o que não foi pedido; anotar a sugestão no fim.
4. **Testar.** Escrever ou ajustar o teste junto com o código.
5. **Verificar.** Rodar build e testes. Só então dizer que terminou, citando o resultado (quantos testes, warnings).
6. **Relatar.** Arquivos alterados, documentação atualizada, o que ficou pendente ou foi pulado e por quê.

## 2. A IA DEVE

- Seguir a Definição de Pronto (`definicao-de-pronto.md`).
- Preservar validações, regras de negócio e comportamento fora do escopo. Remover algo existente exige pedido explícito.
- Citar `arquivo:linha` ao afirmar algo sobre o código.
- Apontar quando o pedido conflita com uma regra escrita, antes de seguir.
- Relatar falha de teste com a saída real, sem esconder.

## 3. A IA NÃO DEVE sem autorização explícita na conversa

| Ação | Motivo | Garantia automática |
|---|---|---|
| Apagar arquivo ou pasta | irreversível fora do git | `ask` em `Bash(rm *)` |
| Editar migration já aplicada | quebra bancos existentes | `ask` em `Edit` da pasta de migrations |
| Alterar autenticação, autorização, criptografia, pagamento ou webhook | risco de segurança e financeiro | lista de áreas sensíveis no `CLAUDE.md` |
| Adicionar, remover ou atualizar pacote | segurança da cadeia e licença | `ask` em `Bash(dotnet add *)`, `Bash(npm install *)` |
| Commit, push, merge, tag | publicação | `ask` em `Bash(git commit *)`, `deny` em `Bash(git push --force *)` |
| Comando contra produção (SSH na VPS, banco de produção, painel de terceiros) | impacto real em clientes | `deny`/`ask` em `Bash(ssh *)` |
| Comando destrutivo de dados (`docker compose down -v`, `docker volume rm`, `DROP`, `dotnet ef database drop`) | perda de dados | `deny` |
| Ler `.env`, User Secrets, chaves, `.pfx` ou backups | exposição de segredo | `deny` em `Read(...)` |
| Gravar segredo em arquivo versionado | vazamento | gitleaks |
| Desligar teste, analisador ou warning para "passar" | esconde problema | revisão |

> As regras de permissão por prefixo do Bash **não são barreira de segurança** (a documentação oficial avisa que `Bash(curl *)` não impede `/usr/bin/curl`, e que `deny` em `Read` não cobre `grep -r` nem scripts). Elas reduzem acidentes; a proteção real é não ter segredo no disco versionado e não ter credencial de produção na máquina de desenvolvimento.

## 4. Configuração do Claude Code no repositório

| Arquivo | Versionado | Conteúdo |
|---|---|---|
| `CLAUDE.md` | sim | índice e regras essenciais do projeto (até ~200 linhas, recomendação oficial) |
| `.claude/settings.json` | sim | permissões e hooks do time |
| `.claude/settings.local.json` | não | ajustes pessoais (o Claude Code o ignora no git ao criar) |
| `.claude/commands/*.md` | sim | comandos reutilizáveis (`/nome`) |
| `.claude/rules/*.md` | sim (opcional) | regras carregadas só quando a IA mexe em arquivos que casam com `paths:` |
| `~/.claude/CLAUDE.md` | não | preferências pessoais |

- DEVE: avaliar `deny` antes de `allow` (a ordem oficial é deny → ask → allow; a primeira que casar decide).
- DEVE: preferir links a imports `@arquivo` no `CLAUDE.md`. Imports são carregados sempre, no início da sessão, e não economizam contexto.
- RECOMENDADO: comandos novos podem virar skills (`.claude/skills/<nome>/SKILL.md`) quando precisarem de arquivos de apoio; a documentação oficial informa que comandos foram incorporados às skills e continuam funcionando.

## 5. Hooks: custo/benefício

| Hook | Benefício | Custo | Decisão |
|---|---|---|---|
| Formatar o arquivo editado (ex.: BOM/encoding, `dotnet format --include <arquivo>`) | evita diferença de formatação no PR | `dotnet format` carrega a solução: ~10–30 s por edição | só se for leve (script por arquivo); formatação completa fica no CI |
| `dotnet build` após cada edição | erro de compilação mais cedo | 20–60 s por edição, trava o fluxo | **não**; a IA roda o build ao final (DoD) |
| Bloquear edição em migrations aplicadas (PreToolUse) | proteção forte | script simples | opcional; a permissão `ask` já cobre |
| Bloquear leitura de segredos (PreToolUse) | cobre casos que `deny` não pega | script por padrão de caminho | opcional |

## 6. Comandos do kit

| Comando | Uso |
|---|---|
| `/nova-feature <descrição>` | planejar e implementar uma feature de ponta a ponta seguindo a arquitetura do projeto |
| `/revisar-pr [branch]` | revisar o diff contra a DoD, segurança e padrões |
| `/auditoria-seguranca [escopo]` | checklist de `seguranca.md` com evidências |
| `/auditoria-performance [escopo]` | checklist de `performance.md` com evidências |
| `/nova-migration <Nome>` | gerar, revisar e documentar uma migration |
| `/atualizar-docs` | conferir e atualizar a documentação afetada pelo diff atual |
