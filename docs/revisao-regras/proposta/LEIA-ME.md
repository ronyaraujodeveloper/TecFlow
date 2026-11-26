# Proposta de nova estrutura de regras

> Rascunho para revisão. Nada aqui foi aplicado. A ordem de implantação está em [`../PLANO.md`](../PLANO.md).

## Os três níveis

| Nível | Onde mora | O que contém | Pasta nesta proposta |
|---|---|---|---|
| 1. Universal / pessoal | `~/.claude/CLAUDE.md` | Só preferências pessoais (idioma, como quero ser consultado). Vale para qualquer projeto, inclusive os que não seguem o kit. | [`usuario/`](./usuario/) |
| 1. Universal / kit base | neste repositório (fonte única), copiado para projetos novos conforme `docs/kit-base/LEIA-ME.md` | Padrões C#/.NET, segurança base, performance, testes, Git, Definição de Pronto, trabalho com IA, `.editorconfig`, `Directory.Build.props`, CI, comandos do Claude Code. | [`kit-base/`](./kit-base/) |
| 2. Do projeto | este repositório | Domínio, arquitetura em camadas do Ofertio, decisões (ADRs), integrações, UI, ambientes Docker, áreas sensíveis. | [`repo/`](./repo/) |
| 3. Automação | arquivos de configuração | O que deixou de ser só texto: analisadores, formatação, CI, varredura de segredos, permissões e hook do Claude Code. | dentro de `kit-base/` e `repo/` (marcados com ⚙️ abaixo) |

## Como fica o repositório depois de aplicado

O repositório final é **`kit-base/` + `repo/`** copiados na raiz (o `repo/` sobrescreve o `kit-base/` quando o mesmo arquivo existe nos dois, caso só de `.claude/settings.json`). Exceção: `kit-base/LEIA-ME.md`, `CLAUDE.modelo.md` e `.claude/settings.base.json` vão para `docs/kit-base/` (modelos e lista de cópia, decisão de 25/11).

```text
/
├─ CLAUDE.md                         índice para a IA (repo/)          ~150 linhas
├─ README.md                         para humanos (repo/)
├─ .editorconfig                  ⚙️ kit-base/
├─ Directory.Build.props          ⚙️ kit-base/ (ativação gradual)
├─ .gitleaks.toml                 ⚙️ repo/
├─ .github/
│  └─ workflows/ci.yml            ⚙️ kit-base/
├─ .claude/
│  ├─ settings.json               ⚙️ repo/ (base do kit + itens do Ofertio)
│  ├─ hooks/garantir-bom.ps1      ⚙️ repo/ (só .ps1)
│  └─ commands/*.md                  kit-base/ (6 comandos)
└─ docs/
   ├─ kit-base/                      LEIA-ME (lista de cópia), CLAUDE.modelo.md, settings.base.json
   ├─ padroes/                       kit-base/ (universal, igual em todo projeto)
   │  ├─ codigo-csharp.md
   │  ├─ seguranca.md
   │  ├─ performance.md
   │  ├─ banco-de-dados.md
   │  ├─ api.md
   │  ├─ testes.md
   │  ├─ observabilidade.md
   │  ├─ git.md
   │  ├─ definicao-de-pronto.md
   │  ├─ trabalho-com-ia.md
   │  └─ documentacao.md
   ├─ projeto/                       repo/ (só do Ofertio)
   │  ├─ arquitetura.md              substitui docs/rules/backend-architecture.md (§2 a §5, §8, §9)
   │  ├─ seguranca-ofertio.md        substitui backend-architecture.md §6 e §7.6
   │  ├─ menu-e-permissoes.md        substitui backend-architecture.md §7
   │  ├─ encoding-e-legado.md        substitui docs/rules/encoding-standards.md
   │  ├─ frontend-ui.md              move docs/rules/frontend-ui.md (com ajustes listados)
   │  ├─ integracoes-marketplaces.md move docs/rules/marketplaces-oauth.md (com ajustes listados)
   │  └─ ambientes-docker.md         move docs/rules/ambientes-docker.md (com ajustes listados)
   └─ adr/                           repo/ (decisões já tomadas, registradas)
      ├─ 0000-modelo.md
      └─ 0001 … 0010 (0001–0008 retroativas; 0009 ReadDto e 0010 códigos HTTP, decididas em 25/11)
```

`docs/CHECKLIST_ATIVO.md`, `docs/HISTORICO_FASES_CONCLUIDAS.md`, `docs/EXECUCAO_*.md`, `docs/CONFIGURACAO_PRODUCAO_DOCKER.md`, `docs/DIAGRAMAS_ARQUITETURA.md` e `docs/PLANO_RPA.md` **continuam onde estão**: são roteiros e registros, não regras.

## Princípios usados na proposta

1. **Cada regra mora em um único lugar.** O `CLAUDE.md` traz as regras essenciais em uma linha cada; o documento detalhado traz o "como verificar", exceções e exemplos, sem repetir a regra com outras palavras.
2. **DEVE / NÃO DEVE / RECOMENDADO**, no sentido da RFC 2119. Cada regra tem um motivo de uma linha e, quando possível, a forma de verificar.
3. **Regra importante vira automação.** Onde existe ferramenta (analisador, CI, gitleaks, permissão, hook), a regra aponta para ela. Texto sozinho fica para o que não dá para automatizar.
4. **Proporcional ao porte.** Projeto de um desenvolvedor, uma VPS, uma instância. Nada de Redis, Kubernetes, cobertura global ou processo de equipe grande.
5. **Links em vez de imports.** A documentação oficial confirma que um import `@arquivo` no `CLAUDE.md` é carregado sempre, no início da sessão ("are expanded and loaded into context alongside the CLAUDE.md that references them", https://code.claude.com/docs/en/memory). Importar todos os padrões anularia o ganho de um `CLAUDE.md` enxuto. Por isso o índice usa links comuns, e a IA abre o documento da área em que estiver trabalhando, como já acontece hoje (`README.md:104`).
6. **Regras por pasta para a IA (opcional, etapa 9 do plano).** O Claude Code carrega `.claude/rules/*.md` com `paths:` só quando a IA mexe em arquivos daquele caminho. É o mecanismo automático para "abra só a regra da camada". Fica como etapa opcional, depois de validado num piloto.

## Itens que não ficaram nesta proposta (de propósito)

- Redis, fila distribuída, CDN, testes de carga: sem necessidade medida (ver `kit-base/docs/padroes/performance.md` §"Quando não otimizar").
- Cobertura global mínima: substituída por obrigatoriedade de teste nos fluxos críticos (`testes.md`).
- Versionamento de API (`/v1`): só há um cliente (a própria WebUi/Mobile). Fica como RECOMENDADO para quando surgir cliente externo.
- Changelog manual: o histórico é o `git log` com Conventional Commits, como já decidido (`docs/rules/encoding-standards.md:37`).
