# Segurança (base universal)

> Base: checklist de 27 itens + OWASP Top 10. Cada regra diz **o que é obrigatório** e **como verificar**.
> Regras específicas do projeto (criptografia de tokens, Data Protection, OAuth, LGPD do domínio) ficam em `docs/projeto/seguranca-*.md`.

## 1. Autenticação e sessão

| # | Regra | Verificar |
|---|---|---|
| S1 | DEVE haver política de autorização padrão fechada (`FallbackPolicy = RequireAuthenticatedUser`). Endpoint público só com `[AllowAnonymous]` explícito. Motivo: esquecer o atributo não pode abrir dados. | Teste de integração que percorre todos os endpoints e falha se algum responder 2xx sem token e não estiver na lista de anônimos aprovados. |
| S2 | DEVE justificar cada `[AllowAnonymous]` em comentário de uma linha no código e incluir o endpoint na lista de anônimos aprovados do teste global de autorização. | Teste global de autorização (S1); `/revisar-pr`. |
| S3 | DEVE guardar senha só como hash adaptativo (BCrypt, Argon2 ou PBKDF2 do Identity). NÃO DEVE usar MD5/SHA puro. | Busca no CI por `MD5`, `SHA1`, `SHA256` aplicados a senha. |
| S4 | DEVE haver bloqueio após tentativas falhas (lockout) e rate limit no login, cadastro e reset de senha. | Teste de integração: N tentativas → 429/bloqueio. |
| S5 | DEVE responder igual para e-mail existente e inexistente em login, cadastro e reset. Motivo: enumeração de contas. | Teste comparando as duas respostas. |
| S6 | DEVE validar emissor, audiência, assinatura e validade de todo token externo (JWT próprio, id_token de Google/Apple/Facebook). NÃO DEVE decodificar token sem validar assinatura. | Teste com token de assinatura inválida → 401. |
| S7 | DEVE ter validade curta para token de acesso e forma de revogação (security stamp, refresh rotativo ou lista de revogação) quando o token vale horas. | Revisão de `JwtTokenService`. |
| S8 | Cookies de autenticação DEVEM ser `HttpOnly`, `Secure` e `SameSite=Lax` ou mais restrito. | Teste ou inspeção do `Set-Cookie`. |

## 2. Autorização

| # | Regra | Verificar |
|---|---|---|
| S9 | DEVE checar papel/permissão **no servidor** em toda ação que expõe dados ou altera estado. Esconder botão na tela não é controle. | Teste de integração por papel. |
| S10 | DEVE checar posse do objeto (IDOR) em todo endpoint com `{id}`: a consulta filtra pelo dono/inquilino do usuário logado, em GET, PUT **e** DELETE. | Teste: usuário B tenta ler/alterar/apagar recurso do usuário A → 404. |
| S11 | Filtros globais de multi-inquilino DEVEM falhar fechados: sem inquilino resolvido, a consulta não devolve nada (exceto em escopo de sistema explícito). | Teste do filtro com inquilino nulo. |
| S12 | NÃO DEVE devolver segredo, hash, token de terceiros ou dado de outro usuário em resposta. | Teste de serialização das respostas sensíveis. |

## 3. Entrada e regras de negócio

| # | Regra | Verificar |
|---|---|---|
| S13 | DEVE receber DTO na escrita, nunca entidade (`[FromBody] Entidade` proibido). Campos sensíveis (papel, plano, saldo, dono, inquilino) NÃO DEVEM vir do cliente. | Analisador/busca no CI: `[FromBody]` com tipo de `Entities`. |
| S14 | DEVE calcular preço, desconto, crédito e saldo no servidor a partir do banco. Valor vindo do cliente ou de webhook é só conferido. | Teste de serviço. |
| S15 | DEVE validar toda entrada no servidor (tamanho, formato, faixa). Validação na tela é só conforto. | DataAnnotations/FluentValidation + teste. |
| S16 | DEVE usar consultas parametrizadas. NÃO DEVE concatenar entrada em SQL (`FromSqlRaw`/`ExecuteSqlRaw` com interpolação manual). | Busca no CI por `FromSqlRaw(`/`ExecuteSqlRaw(` + `$"`. |
| S17 | Ao baixar URL informada pelo usuário (imagem, vídeo, metadados), DEVE bloquear IP privado, loopback, link-local e metadados de nuvem, inclusive após redirect, e limitar tamanho e tempo. Motivo: SSRF. | Teste com `http://169.254.169.254` e `http://127.0.0.1` → recusado. |
| S18 | Upload DEVE validar tipo pelo conteúdo (magic bytes), limitar tamanho, gerar nome no servidor e gravar fora da pasta de código. | Teste com arquivo renomeado. |
| S19 | Redirect para URL externa DEVE usar lista permitida ou destino salvo no servidor (sem open redirect). | Teste. |

## 4. Superfície HTTP

| # | Regra | Verificar |
|---|---|---|
| S20 | DEVE ter rate limiting (`AddRateLimiter`) em autenticação, endpoints públicos e operações caras. | Teste ou configuração revisada. |
| S21 | CORS DEVE listar origens explícitas. NÃO DEVE `AllowAnyOrigin` em produção. | Busca no CI. |
| S22 | Onde há cookie de sessão e formulário, DEVE haver antiforgery. | Revisão. |
| S23 | DEVE enviar HSTS, `X-Content-Type-Options: nosniff`, `X-Frame-Options`/`frame-ancestors` e CSP (no proxy ou na aplicação). | `curl -I` no deploy; checagem no roteiro de produção. |
| S24 | Webhook DEVE autenticar a origem (assinatura HMAC do corpo ou token comparado em tempo constante) **e** ser idempotente (id do evento gravado; reprocessar não repete efeito). | Teste: mesmo evento duas vezes → um efeito. Teste sem assinatura → 401. |
| S25 | Erro em produção NÃO DEVE expor stack trace, mensagem de exceção, SQL ou caminho de arquivo. | Teste de integração em ambiente `Production`. |

## 5. Segredos e dependências

| # | Regra | Verificar |
|---|---|---|
| S26 | NÃO DEVE haver segredo em arquivo versionado (`appsettings*.json`, compose, scripts, docs). Versionado só placeholder (`""` ou `__DEFINIR__`). | **gitleaks no CI e em pre-commit.** |
| S27 | Segredo que chegou ao git DEVE ser considerado vazado: revogar/rotacionar no provedor, e só depois limpar o arquivo. Apagar do histórico não basta. | Procedimento em `docs/projeto/` (roteiro de segredos). |
| S28 | DEVE manter dependências sem vulnerabilidade conhecida Alta/Crítica e runtime dentro do suporte. | `dotnet list package --vulnerable --include-transitive` no CI (falha em High/Critical); `npm audit --audit-level=high` se houver `package.json`. |
| S29 | NÃO DEVE desligar verificação de assinatura de pacotes nem rodar container como root sem justificativa registrada em ADR. | Revisão de `nuget.config` e Dockerfile. |

## 6. Logs e dados pessoais

| # | Regra | Verificar |
|---|---|---|
| S30 | NÃO DEVE registrar senha, token, `client_secret`, código OAuth, cartão, documento completo nem URL com credencial na query. | Teste de logging nos handlers HTTP; revisão. |
| S31 | DEVE registrar eventos de segurança: login falho, bloqueio, acesso negado, mudança de papel/plano, uso de emulação. | Revisão. |
| S32 | LGPD: DEVE haver forma de o titular pedir exclusão/anonimização da conta, e retenção definida para dados operacionais. | Fluxo existente + teste. |

## 7. Quando revisar segurança

- DEVE rodar `/auditoria-seguranca` (ou checklist equivalente) antes de cada deploy de produção que mexa em autenticação, pagamento, webhook, upload ou endpoint anônimo.
- DEVE tratar achado Crítico como bloqueio de deploy.
