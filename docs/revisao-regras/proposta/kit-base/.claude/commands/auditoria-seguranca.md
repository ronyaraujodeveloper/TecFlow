---
description: Audita a segurança do escopo indicado contra docs/padroes/seguranca.md, com evidências e sem alterar nada
argument-hint: [escopo: pasta, controller, feature ou "tudo"]
---

Escopo: $ARGUMENTS (vazio = repositório inteiro).

## Regras desta auditoria

- Somente leitura. Não altere arquivos, não instale ferramentas, não rode nada contra produção ou serviço externo.
- Toda afirmação com evidência `arquivo:linha` ou com o comando usado para verificar.
- Nunca exiba segredo encontrado: informe só o arquivo e a linha, com os 4 primeiros caracteres seguidos de `****`.

## Passos

1. Inventário do escopo: endpoints (rota, verbo, atributo de autorização, política), webhooks, jobs, uploads, chamadas a URL informada pelo usuário.
2. Percorra cada item S1 a S32 de `docs/padroes/seguranca.md` e os itens de `docs/projeto/seguranca-ofertio.md`. Status: OK / PARCIAL / ERRADO / AUSENTE / N.A. / NÃO VERIFICÁVEL.
3. Rode o que for local e seguro:
   - `dotnet list package --vulnerable --include-transitive`
   - `git grep` por padrões de segredo em arquivos versionados (sem exibir valores)
   - `gitleaks detect --no-banner --redact` se estiver instalado (não instale)
4. Para cada achado ERRADO/AUSENTE: severidade (Crítica/Alta/Média/Baixa), cenário de exploração em uma frase, correção proposta e esforço (P/M/G).

## Entrega

Gere `docs/auditoria/AUDITORIA-SEGURANCA-<AAAA-MM-DD>.md` **somente se o usuário pedir**. Caso contrário, responda no chat com:
- nota 0–10 e veredito "apto para produção: sim/não";
- os 5 riscos mais graves;
- matriz completa dos itens;
- plano em três blocos: corrigir agora, próximas 2 semanas, backlog.
