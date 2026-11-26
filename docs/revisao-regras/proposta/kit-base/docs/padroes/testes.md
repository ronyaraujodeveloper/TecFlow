# Testes (base universal)

> Estratégia: teste é obrigatório onde o erro custa caro, não em busca de um percentual global.

## 1. O que DEVE ter teste

| Área | Tipo mínimo | Por quê |
|---|---|---|
| Regra de negócio pura (cálculo, decisão, validação) | unitário | barato e rápido; é onde nascem os bugs de lógica |
| Autenticação e autorização (quem acessa o quê, IDOR, endpoint anônimo) | **integração** (`WebApplicationFactory`) | só o pipeline real prova que o atributo/política está ativo |
| Pagamento, crédito, saldo, cobrança e webhooks (incluindo repetição do mesmo evento) | integração ou unitário com banco | dinheiro e acesso |
| Bug corrigido | teste que falhava antes da correção | evita regressão |
| Fluxo de tela crítico (cadastro, login, checkout, publicação) | ponta a ponta | prova a integração com a interface |

- DEVE: todo endpoint novo ter pelo menos um teste de autorização (sem token → 401; outro dono → 404/403).
- RECOMENDADO: serviços de orquestração (que só chamam outros) testados pelo comportamento observável, não por `Verify` de cada chamada.
- NÃO DEVE: testar getter/setter, mapeamento trivial ou código de framework.

## 2. Tipos e onde ficam

| Tipo | Projeto/pasta | Dependências | Roda no CI |
|---|---|---|---|
| Unitário | `*.Tests/Unit/<Área>/` | sem rede, sem disco, banco InMemory ou SQLite só para repositório simples | sempre |
| Integração | `*.Tests/Integration/<Área>/` | `WebApplicationFactory`, banco real em container (Testcontainers) ou SQLite | sempre |
| Ponta a ponta | projeto E2E | ambiente de homologação | manual ou agendado |

- DEVE: uma pasta por tipo. NÃO DEVE: criar pastas paralelas com o mesmo papel (`UnitTests` e `Unit`).
- RECOMENDADO: banco real (Testcontainers) para testes que dependem de transação, concorrência ou SQL específico; InMemory não simula nenhum dos três.

## 3. Nomenclatura e estrutura

- DEVE: método `Metodo_Cenario_ResultadoEsperado` (ex.: `Unlock_SaldoInsuficiente_RetornaErro`).
- DEVE: classe `<ClasseTestada>Tests`.
- DEVE: corpo em Arrange / Act / Assert, um comportamento por teste.
- RECOMENDADO: `[Theory]` com `[InlineData]` para variações do mesmo comportamento.

## 4. Dados de teste

- DEVE: cada teste cria os próprios dados (builder ou fábrica) e não depende da ordem de execução.
- DEVE: dado criado em ambiente compartilhado (E2E em homologação) tem prefixo identificável (ex.: `E2E-`) e é limpo ou reaproveitado.
- NÃO DEVE: dado real de cliente, segredo real ou e-mail real em teste.
- DEVE: relógio e aleatoriedade controlados (`TimeProvider` falso, semente fixa).

## 5. Cobertura

- DEVE: 100% dos itens da tabela do §1 com teste antes do merge. Verificar: `/revisar-pr` antes do merge.
- RECOMENDADO: medir cobertura (coverlet) só das pastas críticas (autorização, pagamento, regras) e acompanhar tendência. NÃO DEVE: meta global como critério de bloqueio.

## 6. Proibições

- NÃO DEVE: alterar o teste para ele passar sem explicar por que o comportamento esperado mudou.
- NÃO DEVE: `[Fact(Skip = ...)]` sem link para a pendência no checklist.
- NÃO DEVE: teste que depende de serviço externo real (API de marketplace, gateway) rodar no CI.
