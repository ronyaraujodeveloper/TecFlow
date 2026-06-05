// Arquivo: TecFlow.Infrastructure/Interfaces/IAppConfiguration.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TecFlow.Infrastructure.Interfaces
{
    public interface IAppConfiguration
    {
        // Propriedades existentes que foram validadas nos arquivos fornecidos
        string DatabaseProvider { get; }
        string DatabaseConnectionString { get; }
        string OpenAI_ApiKey { get; }
        string Gemini_ApiKey { get; }
        string TikTok_ClientId { get; } // J� existia e foi validada
        string Shopee_PartnerId { get; } // J� existia e foi validada
        string AzureKeyVaultUri { get; }
        string Shopee_ApiBaseUrl { get; } // J� existia e foi validada

        // --- NOVAS PROPRIEDADES ADICIONADAS CONFORME ITEM 2.1 ---

        // TikTok Ads
        string TikTokAds_AccessToken { get; }
        string TikTokAds_ApiBaseUrl { get; }

        // TikTok Shop (propriedades adicionais al�m do ShopId j� presente em outras configura��es)
        string TikTokShop_AccessToken { get; }
        string TikTokShop_ShopId { get; } // J� est� implicitamente usado em ShopeeApiService, mas vamos adicionar aqui para clareza se necess�rio
        string TikTokShop_ApiBaseUrl { get; }

        // --- M�todo Existente ---
        /// <summary>
        /// Obt�m uma configura��o espec�fica pelo nome da chave. �til para chaves adicionais n�o mapeadas diretamente.
        /// </summary>
        string? GetSetting(string key);
    }
}