# 📝 TecFlow - Roadmap, Arquitetura & Contexto Geral

> **Painel principal do projeto** (antigo `TODO.md`). Tarefas, regras de código e links para `docs/`. A IA deve marcar checkboxes aqui ao concluir implementações (ver `.cursorrules`).

## 🎯 Objetivo do Projeto
Plataforma de **automação e inteligência para afiliados de alta escala**: orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com TikTok Shop e Shopee. O ecossistema combina backend robusto em C# (`TecFlow.API`, `TecFlow.Worker`, `TecFlow.Orquestrador`) com o frontend **`TecFlow.WebUi`** para controle, auditoria e monitoramento em produção.

## 🛠️ Stack Tecnológica Definida
- **Backend:** .NET 8.0 Web API (C#)
- **Frontend:** Blazor WebApp — projeto **`TecFlow.WebUi`** (ASP.NET Core .NET 8.0)
- **Banco de Dados:** PostgreSQL (Instalado localmente via EDB, Porta 5432)
- **ORM:** Entity Framework Core (EF Core) com Npgsql

## 📚 Documentação Complementar
* Veja a [Lista de Mudanças de Arquivos](./docs/LISTA_ARQUIVOS_MUDANCAS.md)
* Veja a [ANALISE WORKSPACE COMPLETA](./docs/ANALISE_WORKSPACE_COMPLETA.md)
* Veja a [INDICE COMPLETO](./docs/INDICE_COMPLETO.md)
* Veja a [RESUMO EXECUTIVO](./docs/RESUMO_EXECUTIVO.md)
* Veja a [DIAGRAMAS ARQUITETURA](./docs/DIAGRAMAS_ARQUITETURA.md)
* Consulte os [Comandos Úteis do Git](./docs/ComandosGit.txt)

## 📐 Padrões de Código e Nomenclatura (Strict Rules)
Sempre que criar ou editar código neste projeto, você DEVE seguir estes padrões estritos:

1. **Idioma:** Código em inglês (classes, métodos, variáveis), comentários e documentação em português.

2. **Arquitetura Racional de Dados (Apenas 3 Objetos por Entidade):** Para evitar redundância de arquivos (como Create/Update Dtos), cada entidade deve possuir estritamente:
   - **`[Nome]Filter`** (na pasta `TecFlow.Database/Filter/`): Contém as propriedades escalares da Entity como opcionais/nullable (`?`). Usado **EXCLUSIVAMENTE** para parâmetros de busca em listagens e consultas (**GET**). Não incluir objetos de navegação — apenas IDs (`CampaignId`, `OwnerId`, etc.).
   - **`[Nome]Dto`** (na pasta `TecFlow.Business/Dto/`): Contém as propriedades limpas que a tela envia para o formulário. Usado **EXCLUSIVAMENTE** para receber dados de escrita (**POST** e **PUT**). Não deve conter objetos complexos de navegação inteiros, apenas seus IDs correspondentes.
   - **`[Nome]ResponseDto`** (na pasta `TecFlow.Business/Dto/`): Envelope padrão de retorno que encapsula propriedades de controle (`Status` [bool], `Descricao` [string]) e a Entity real (`Data` [[Nome]?] e `DataList` [List<[Nome]>?]).

   > A **Entity** continua em `TecFlow.Core/Entities/` ou `TecFlow.Database/Entity/`. Exemplo `Estoque`: `EstoqueFilter`, `EstoqueDto`, `EstoqueResponseDto` + Entity.

3. **Padrão de Retorno de Métodos (Controllers e Services):** Métodos **GET** retornam `[Nome]ResponseDto` com `Data` ou `DataList`. Métodos **POST/PUT** recebem `[Nome]Dto` e retornam `[Nome]ResponseDto` quando aplicável.

4. **Filtros e Consultas:** Toda listagem (**GET**) deve aceitar `[FromQuery] [Nome]Filter filter`, aplicar o filtro na query (repositório ou extensão `ApplyFilter`) e paginar via `Pagin` (máximo 30 registros).

5. **Criptografia de credenciais:** Qualquer campo, propriedade ou dado que se refira a senhas ou tokens de acesso (como senhas de usuários, tokens do TikTok ou Shopee) DEVE ser criptografado antes de ser salvo no banco de dados e descriptografado apenas no momento do uso.

6. **Validações Obrigatórias:** Cadastros de e-mail, celular/WhatsApp, CPF, CNPJ alfanumérico e CEP devem passar estritamente pelos métodos do `ValidationHelper` em `TecFlow.Business/Service` antes de qualquer persistência.

7. **Design Responsivo & Mentalidade Mobile First:** Qualquer novo componente visual, página ou layout criado no projeto `TecFlow.WebUi` DEVE obrigatoriamente contemplar design responsivo (Mobile First). É proibido o uso de larguras fixas em pixels para containers principais e tabelas densas; utilize grids fluidos, flexbox e classes utilitárias responsivas (Bootstrap/Tailwind) para garantir que a interface se adapte nativamente a PCs, Celulares e Tablets.

8. **Auto-atualização do Roadmap:** Toda vez que eu te pedir para executar uma tarefa descrita neste arquivo, assim que você concluir a implementação do código com sucesso e sem erros de compilação, você DEVE marcar automaticamente a respectiva tarefa como concluída mudando de `[ ]` para `[x]` **neste `README.md`**, sem que eu precise te pedir explicitamente. Sincronize também `docs/LISTA_ARQUIVOS_MUDANCAS.md` e `docs/DIAGRAMAS_ARQUITETURA.md` conforme `.cursorrules`.

## 🚀 Fases do Desenvolvimento e Reestruturação (Checklist)

### Fase 1: Transição de Arquitetura e Renomeação (Foco Atual) 📂
- [x] Mudar o nome da Solution e dos projetos de 'Tecso' para 'TecFlow'.
- [x] Estruturar o projeto **`TecFlow.Business`** com as pastas:
  - `Dto/` (Transições e as classes padrão de Response).
  - `Enum/` (Enumeradores separados por conceitos de domínio).
  - `Service/` (Serviços, regras de negócio e onde ficarão as classes herdadas de Criptografia e Validação).
- [x] Estruturar o projeto **`TecFlow.Database`** com as pastas:
  - `Entity/` (O coração do projeto).
  - `Filter/` (Objetos de busca baseados nas Entities).
  - `Interface/` (Contratos dos repositórios).
  - `Pagin/` (Classe de controle de paginação limitada a 30 registros).
  - `Repositorio/` (Implementação das consultas e queries que consomem os Filters).
- [x] Mover o `DbContext` existente para a raiz ou pasta adequada do `TecFlow.Database`.

### Fases Concluídas e Preservadas (A serem validadas na nova estrutura) ✅
- [x] Instalação e configuração do PostgreSQL local (Porta 5432, base: `automacaosociais`).
- [x] Criação do serviço de criptografia (agora alocado em `TecFlow.Business/Service`).
- [x] Implementação no `ValidationHelper` das validações de E-mail, CPF, CNPJ Alfanumérico, CEP (ViaCEP) e Força de Senhas.
- [x] Implementação da validação rigorosa de celular brasileiro (`IsValidBrazilianCellPhone`).
- [x] Estrutura base do Frontend criada (agora migrando para o projeto **`TecFlow.WebUi`**).
- [x] Telas iniciais de escolha (TikTok/Shopee) e cards de login mockados.
- [x] Implementar fluxo de captura e persistência de Telefone/WhatsApp (com aplicação da validação do helper) para alertas do Orquestrador.

### Fase 2: Implementação dos Componentes Base do Banco e Modelagem ⚙️
- [x] Criar a classe base de paginação na pasta `2.Database/Pagin/` travando os resultados em 30 registros.
- [x] Criar a primeira Entity oficial (`User`) na pasta `Entity/`.
- [x] Criar o objeto espelho `UserFilter` na pasta `Filter/`.
- [x] Criar o `UserDto` e `UserResponseDto` na pasta `1.Business/Dto/`.
- [x] Configurar a Connection String do PostgreSQL e aplicar a primeira Migration do ecossistema TecFlow.

### Fase 3: Regras de Negócio, Telas e APIs Externas 🔄
- [x] Refatorar Controllers para arquitetura de 3 objetos (Filter / Dto / ResponseDto) e remover Create/Update Dtos redundantes.
- [x] Migrar o projeto 'TecFlow.Portal' e adaptar o projeto `TecFlow.WebUi` para consumir os novos Services que retornam o padrão `ResponseDto`.
- [x] Descontinuar `TecFlow.Portal` e `TecFlow.Dashboard`; **`TecFlow.WebUi`** é o frontend canônico (Blazor).
- [x] Integrar os componentes de tela aos filtros de listagem compostos pela pasta `Filter`.
  - [ ] Integração real com as APIs de produção do TikTok Shop e Shopee.
  - [x] 3.1. Infraestrutura Core de Integração: Criar os HttpClient específicos, handlers de resiliência (Polly) e logs de requisições.
  - [x] 3.2. Fluxo de Autenticação & OAuth2: Implementar a geração de URL de autorização, captura do Authorization Code e armazenamento seguro/renovação automática do Access Token e Refresh Token para ambas as plataformas.
  - [x] 3.3. Sincronização de Catálogo (Produtos): Implementar mapeamento de payloads, busca de produtos das plataformas e conversão para o nosso padrão [Nome]ResponseDto.
  - [x] 3.4. Gestão de Pedidos & Estoque: Estruturar os endpoints/serviços para receber webhooks ou realizar polling de novos pedidos e atualizar estoque de forma bidirecional.

### Fase 4: Estratégia Mobile Híbrida (Android e iOS) 📱
- [x] 4.1. Fundação Mobile-First no TecFlow.WebUi e Contratos de API
  - [x] Auditar e refatorar layouts existentes do `TecFlow.WebUi` para conformidade Mobile First (breakpoints, tabelas responsivas, navegação touch-friendly).
  - [x] Padronizar contratos REST (`*Filter`, `*ResponseDto`) e autenticação para consumo estável por clientes móveis futuros.

- [x] 4.2. Shell Híbrido Nativo (.NET MAUI / Blazor Hybrid)
  - [x] Avaliar e estruturar projeto compartilhado (RCL) reutilizando componentes Blazor do `TecFlow.WebUi`.
  - [x] Configurar pipeline de build e publicação para Android e iOS (lojas ou distribuição interna).

- [x] 4.3. Engajamento Móvel em Tempo Real
  - [x] Integrar notificações push (FCM/APNs) para alertas de comentários, comissões e falhas de webhook.
  - [x] Implementar deep links para abrir diretamente painéis de conciliação e fila de engajamento no app.



### Fase 5: Visão de Negócio - Plataforma de Automação e Inteligência para Afiliados de Alta Escala 🚀
Orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com TikTok Shop e Shopee. O ecossistema combina backend robusto em C# (`TecFlow.API`, `TecFlow.Worker`, `TecFlow.Orquestrador`) com o frontend `TecFlow.WebUi` para controle, auditoria e monitoramento em produção.

### Fase 6: Engenharia e Infraestrutura para Afiliados, Mensageria e Escala 🛠️
- [ ] 6.1. Sistema de Filas e Mensageria (Foco em Automação de Engajamento)
  - [ ] Implementar infraestrutura com RabbitMQ ou Azure Service Bus no ecossistema TecFlow.
  - [ ] Criar Webhooks/BackgroundServices para monitorar em tempo real mensagens e comentários postados em suas publicações nas redes/marketplaces.
  - [ ] Estruturar fila de processamento assíncrono para triagem de palavras-chave (ex: identificar comentários como "eu quero" ou "link") e preparar a entrega automatizada do link de afiliado correto sem sobrecarregar o banco de dados.

- [ ] 6.2. Painel de Conciliação Financeira de Afiliado (TecFlow.WebUi)
  - [ ] Criar módulo visual no 'TecFlow.WebUi' focado no rastreio e auditoria de comissões de afiliado.
  - [ ] Integrar com as APIs de performance de afiliados da Shopee e TikTok Shop para buscar o relatório de cliques, conversões e comissões geradas.
  - [ ] Desenvolver lógica de conciliação para bater as vendas rastreadas pelos seus links com os pagamentos reais efetuados pelas plataformas, detalhando quais produtos divulgados geraram a comissão correta e alertando sobre divergências de valores.

- [ ] 6.3. Mecanismo de Mapeamento de Produtos e Atributos Globais para Propaganda
  - [ ] Adaptar a estrutura de catálogos para mapear "Produtos de Propaganda/Divulgação" em vez de estoque físico próprio.
  - [ ] Criar uma estrutura de "Dados Globais do Produto" (Nome Amigável, Categoria Global, Imagens de Destaque, Preço Médio e Seus Links de Afiliado Shopee/TikTok).
  - [ ] Desenvolver um gerador de metadados baseado nesses atributos para alimentar automaticamente suas ferramentas de postagem, garantindo que as informações do produto saiam padronizadas e corretas em qualquer canal de divulgação.

- [ ] 6.4. Observabilidade e Telemetria (Monitoramento de Produção)
  - [ ] Instalar o OpenTelemetry nos projetos centrais do TecFlow.
  - [ ] Configurar exportadores de logs e métricas (como Seq ou Prometheus/Grafana) para monitorar a saúde do ecossistema em produção.
  - [ ] Criar um mini-dashboard de saúde técnica para acompanhar taxas de sucesso das requisições de webhook de comentários, tempo de resposta das APIs de comissão e alertas de falhas em segundo plano.

### Fase 7: Módulo de Vendas Diretas e Gestão de Estoque (Futuro) 📦
- [ ] 7.1. Arquitetura Multi-Tenant / Multi-Conta por Marketplace (SaaS Ready)
  - [ ] Ajustar a modelagem do banco de dados na camada 'Database' para suportar o conceito de inquilinos (Tenants) e vinculação de múltiplos 'ShopId' por usuário.
  - [ ] Adaptar as queries e repositórios para isolar os dados de cada loja, permitindo que o lojista gerencie múltiplos CNPJs/Contas da Shopee e TikTok Shop no mesmo painel.

- [ ] 7.2. Core de Vendas, Faturamento e ERP Local
  - [ ] Criar entidades de 'Pedido de Venda' (Order), 'Cliente' (Customer) e 'Item do Pedido' para registrar vendas próprias.
  - [ ] Estruturar o fluxo de estados do pedido (Pendente, Pago, Faturado, Enviado, Concluído) e preparar ganchos para futura integração com emissão de Notas Fiscais Eletrônicas (NF-e).

- [ ] 7.3. Controle Avançado de Estoque Próprio (Estoque Físico)
  - [ ] Implementar tabelas de movimentação de estoque (Entradas por compra, Saídas por venda, Ajustes manuais, Estoque Mínimo e Alertas).
  - [ ] Desenvolver serviço de reserva de estoque para garantir que, no momento em que um pedido de venda direta for gerado, as unidades fiquem bloqueadas temporariamente até a confirmação do pagamento, evitando o Overbooking (vender o que não tem).
  
---
*Nota para a IA: Sempre siga este roadmap passo a passo e use a nova estrutura de pastas estabelecida. Não pule etapas e preze pela preservação do código de validação já existente.*