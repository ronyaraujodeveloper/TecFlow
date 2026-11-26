# Preferências pessoais (todos os meus projetos)

> Destino: `~/.claude/CLAUDE.md` (no Windows, `%USERPROFILE%\.claude\CLAUDE.md`).
> Este arquivo guarda só preferências minhas de comunicação e um padrão mínimo de segurança para projetos que ainda **não** têm `CLAUDE.md`.
> Nos projetos com o kit base, as regras de trabalho da IA ficam versionadas em `docs/padroes/trabalho-com-ia.md` e prevalecem sobre a seção "Projeto sem CLAUDE.md" abaixo.

## Comunicação

- Responda sempre em português do Brasil, inclusive em planos, relatórios e mensagens de commit.
- Primeiro a resposta ou a recomendação, depois o detalhe. Sem rodeios.
- Ao usar Docker, Git ou outro recurso de infraestrutura, explique o conceito em uma frase. Ainda estou aprendendo Docker.
- Ao afirmar algo sobre o código, cite `caminho:linha`.
- Quando houver uma decisão que só eu posso tomar, pergunte com opções e a sua recomendação, em vez de escolher em silêncio.

## Projeto sem CLAUDE.md (padrão mínimo)

Vale só quando o repositório não tem `CLAUDE.md`:

- Planeje antes de codar e mostre o plano quando a tarefa tocar mais de um arquivo.
- Só diga que terminou depois de compilar e testar; relate falhas e etapas puladas.
- Pergunte antes de: apagar arquivos, comandos destrutivos, commit/push, adicionar pacote, qualquer ação contra produção ou serviço externo.
- Nunca exiba segredos; informe só o local.
