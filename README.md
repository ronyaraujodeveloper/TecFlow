Documento de Controle: Projeto TecFlow.Automacao
1. Contexto do Projeto
Sistema de automação de marketing e vendas para e-commerce (TikTok Shop/Shopee), focado em gestão de produtos, campanhas, análise de métricas via IA e publicação automatizada.

2. Arquitetura Técnica
Framework: ASP.NET Core 8.0+
ORM: Entity Framework Core (SQL Server)
Padrão: Clean Architecture (API, Application, Core, Infrastructure, Orquestrador)
IA de Suporte: Google Gemini API / OpenAI API
Segurança: JWT Authentication.
3. Estado Atual (O que já foi feito)
✅ Camada de Infraestrutura e Core
 Definição de Entidades Base (BaseEntity, Produto, Campanha, Metrica, Afiliado, Usuario).
 Configuração de AppDbContext e Repositórios (AfiliadoRepository, CampanhaRepository, ProdutoRepository, etc).
 Registro de DI configurado em CoreServiceRegistrationExtensions e ExternalServiceRegistrationExtensions.
 Implementação de IAnaliseCalculoService (Cálculo de métricas para Dashboard).
 Implementação de IGeminiService e IAIService (Integração com LLMs).
 Implementação de IShopeeApi e ITikTokShopApi (Clientes HTTP estruturados com Polly para resiliência).
✅ API e Orquestração
 Controladores de API (Afiliados, Campanhas, Produtos, Dashboard, Orquestrador, Usuarios).
 ExceptionMiddleware padronizado para tratamento de erros.
 OrquestradorPrincipal.cs (Lógica de pipeline: Coleta -> IA -> Publicação).
 WorkerService configurado para execução automática do pipeline.
4. O que falta (Checklist de Desenvolvimento)
🚀 Fase 1: Autenticação Social (TikTok OAuth)
 Implementar a troca de code por access_token no TikTokShopApiService (Passo iniciado).
 Criar TikTokAuthController.cs para gerenciar o callback.
 Atualizar a entidade Usuario no banco para comportar vincular o token social (TikTokShop_AccessToken).
 Criar fluxo frontend para redirecionar o usuário para o TikTok e receber o callback.
📊 Fase 2: Dashboard e Métricas
 Finalizar o consumo do DashboardController pelo Front-end (exibir métricas calculadas).
 Implementar cronômetro ou listener para atualizar métricas via API automaticamente.
📈 Fase 3: Frontend e Portal do Usuário
 Estruturar a página de Login/Cadastro.
 Criar a página de listagem de Produtos (com botão "Otimizar via IA").
 Criar o "Dashboard Gerencial" (Cards de métricas).
🛠 Fase 4: Produção e Segurança
 Configuração de Secrets (Azure Key Vault ou Environment Variables para chaves de API).
 Implementar testes de carga mínimos no OrquestradorPrincipal.