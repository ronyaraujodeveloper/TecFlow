// Arquivo: TecFlow.Infrastructure/Services/AppConfiguration.cs (ITEM 117)
// Corre��o para implementar todas as propriedades da interface IAppConfiguration atualizada.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Importar para o _logger
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TecFlow.Infrastructure.Interfaces; // Interface atualizada

namespace TecFlow.Infrastructure.Services
{
    // Adicionado ILogger como depend�ncia
    public class AppConfiguration : IAppConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfiguration> _logger; // Injetar o logger

        // Propriedades Existentes
        public string DatabaseProvider => _configuration.GetValue<string>("Database:Provider") ?? "PostgreSQL";
        public string DatabaseConnectionString => _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Database connection string is missing.");
        public string OpenAI_ApiKey => _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key is missing.");
        public string Gemini_ApiKey => _configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key is missing.");
        public string TikTok_ClientId => _configuration["TikTok:ClientId"] ?? throw new InvalidOperationException("TikTok Client ID is missing.");
        public string Shopee_PartnerId => _configuration["Shopee:PartnerId"] ?? throw new InvalidOperationException("Shopee Partner ID is missing.");
        public string AzureKeyVaultUri => _configuration["AzureKeyVault:Uri"] ?? throw new InvalidOperationException("Azure Key Vault URI is missing.");
        public string Shopee_ApiBaseUrl => _configuration["Shopee:ApiBaseUrl"] ?? throw new InvalidOperationException("Shopee API Base URL is missing.");

        // --- NOVAS PROPRIEDADES IMPLEMENTADAS CONFORME ITEM 2.1 ---

        // TikTok Ads
        public string TikTokAds_AccessToken => _configuration["TikTokAds:AccessToken"] ?? throw new InvalidOperationException("TikTok Ads Access Token is missing.");
        public string TikTokAds_ApiBaseUrl => _configuration["TikTokAds:ApiBaseUrl"] ?? throw new InvalidOperationException("TikTok Ads API Base URL is missing.");

        // TikTok Shop
        public string TikTokShop_AccessToken => _configuration["TikTokShop:AccessToken"] ?? throw new InvalidOperationException("TikTok Shop Access Token is missing.");
        public string TikTokShop_ShopId => _configuration["TikTokShop:ShopId"] ?? throw new InvalidOperationException("TikTok Shop Shop ID is missing.");
        // A Shopee_ApiBaseUrl j� � existente, mas vamos reclassificar a chave para TikTokShop_ApiBaseUrl, conforme pode ter sido discutido
        // Se a Shopee_ApiBaseUrl configurada for a MESMA base para TikTok Shop, ent�o ela j� est� coberta.
        // Se for diferente, precisamos adicionar uma nova. Assumindo que Shopee_ApiBaseUrl � **apenas** para Shopee.
        public string TikTokShop_ApiBaseUrl => _configuration["TikTokShop:ApiBaseUrl"] ?? throw new InvalidOperationException("TikTok Shop API Base URL is missing.");


        // Construtor atualizado para injetar ILogger e validar novas chaves
        public AppConfiguration(IConfiguration configuration, ILogger<AppConfiguration> logger) // Adicionado ILogger
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Inicializa o logger

            // Valida��es das propriedades existentes (chamando os getters para for�ar a valida��o)
            _ = DatabaseProvider;
            _ = DatabaseConnectionString;
            _ = OpenAI_ApiKey;
            _ = Gemini_ApiKey;
            _ = TikTok_ClientId;
            _ = Shopee_PartnerId;
            _ = AzureKeyVaultUri;
            _ = Shopee_ApiBaseUrl;

            // Valida��es das NOVAS propriedades
            _ = TikTokAds_AccessToken;
            _ = TikTokAds_ApiBaseUrl;
            _ = TikTokShop_AccessToken;
            _ = TikTokShop_ShopId;
            _ = TikTokShop_ApiBaseUrl;

            _logger.LogInformation("AppConfiguration loaded successfully with all required settings.");
        }

        // O m�todo GetSetting j� existe
        public string? GetSetting(string key)
        {
            return _configuration[key];
        }
    }
}