# Segurança específica do Ofertio

> Complementa `docs/padroes/seguranca.md` (base universal). Substitui `docs/rules/backend-architecture.md` §6.

## 1. Credenciais em repouso

- DEVE: senha de integração, `AccessToken`, `RefreshToken`, `ClientSecret`/`AppSecret`, chave de API e documento (`PerfisCadastrais.Document`) gravados pelo conversor `EncryptedStringConverter.CreateProtected` / `ISecurityEncryptionService`. Verificar: ao mexer em entidade com segredo, a conversão no `OnModelCreating` continua ativa (teste de mapeamento).
- Cadeia de leitura (`Unprotect`): `DP1:` (Data Protection) → `ENC1:` (AES legado) → texto puro.

## 2. Data Protection

- DEVE: `AddOfertioDataProtection` com chaves em `DataProtection:KeysPath`; API e Worker compartilham o mesmo anel. Nos containers, volume `api_appdata`, sem DPAPI.
- NÃO DEVE: renomear `SetApplicationName("TecFlow")` nem o purpose `TecFlow.MarketplaceTokens.v1` (ADR 0004). Motivo: tokens gravados ficam ilegíveis e todas as contas precisam ser reautorizadas.
- DEVE: deploy e limpeza preservam o volume das chaves; backup das chaves junto com o do banco.

## 3. Segredos por ambiente

| Ambiente | Onde fica o segredo | Onde NÃO pode ficar |
|---|---|---|
| Produção (VPS) | `.env` da VPS (lido pelo compose) e Cofre do Painel Master (integrações) | `appsettings*.json`, compose, git |
| Homologação (Docker local) | `.env` local (fora do git) | idem |
| Desenvolvimento (Visual Studio) | User Secrets do projeto | idem |
| E2E | User Secrets do `Ofertio.E2E` | idem |

- DEVE: `appsettings*.json` versionados com placeholder vazio para toda chave secreta (`Jwt:Key`, `Encryption:Key`, `Integrations:Webhook:Secret`, `EvolutionApi:ApiKey`, chaves de IA, senhas de banco).
- DEVE: toda chave do `.env` documentada no `.env.example` com valor fictício.
- DEVE: segredo que já esteve no git é rotacionado conforme `docs/auditoria/ROTEIRO_BACKUP_E_SEGREDOS.md`. Trocar `Encryption:Key` exige recriptografar os segredos gravados (`LegacyCredentialReEncryptService`) antes.

## 4. Multi-inquilino

- DEVE: toda entidade de cliente tem `TenantId` e entra no filtro global do `AppDbContext`.
- DEVE: o filtro falha fechado: sem inquilino resolvido, nada é devolvido. Job em segundo plano que precisa ver todos os inquilinos usa escopo de sistema explícito. ⚠️ Situação atual: o filtro é aberto quando o inquilino é nulo (`AppDbContext.cs:997-1000`, segundo `docs/auditoria/RELATORIO.md` F1).
- NÃO DEVE: usuário sem inquilino ser associado a outro inquilino existente.
- Rotinas em segundo plano nunca usam a conta de marketplace de outro inquilino.

## 5. Pagamento e plano

- DEVE: número, CVV e validade do cartão só trafegam na requisição (`CreditCardDto`) até o `IPlanPaymentGateway`; nunca gravados, logados nem devolvidos. O pedido guarda só bandeira e 4 últimos dígitos.
- DEVE: preço do plano lido do banco (`MyPlanService`). O checkout registra o pedido e não altera `Tenant.PlanCode`/`BillingCycle`; quem grava é o Master (`TenantAdminService`).
- DEVE: webhook do Asaas autenticado por token em tempo constante, idempotente por id do evento (um pagamento = uma renovação) e com valor conferido contra o pedido, não lido do payload.
- DEVE: crédito (`DealCreditsService`) só somado com pagamento confirmado; débito com controle de concorrência.
- Enquanto o gateway for o simulado (`SimulatedPlanPaymentGateway`), as telas mostram o aviso de modo de teste.

## 6. Termos e LGPD

- DEVE: aceite gravado com versão, data UTC e IP (`CF-Connecting-IP`, depois o primeiro `X-Forwarded-For`); o IP nunca vem do corpo.
- DEVE: publicar termos no Master gera nova versão e exige reaceite no login. Primeiro vínculo de loja pede o aceite da finalidade de API externa.
- DEVE: existir fluxo de exclusão/anonimização da conta a pedido do titular. ⚠️ Situação atual: ainda não existe (auditoria S28).

## 7. Integrações

Limites com marketplaces (só API oficial, sem contornar anti-robô, OAuth com PKCE e state selado): [integracoes-marketplaces.md](./integracoes-marketplaces.md) §1–§3.
