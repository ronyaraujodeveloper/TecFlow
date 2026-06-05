// Arquivo: TecFlow.Infrastructure.Services.ExternalServices/TikTokShopApiService.cs
// CORRIGINDO A IMPLEMENTAÇÃO DA INTERFACE ITikTokShopApi

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.ExternalServices
{
    public class TikTokShopApiService : ITikTokShopApi
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TikTokShopApiService> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _accessToken;
        private readonly string _shopApiBaseUrl;
        private readonly string _shopId;

        // Endpoints da API do TikTok Shop
        private const string EndpointSearchProducts = "products";
        private const string EndpointCreateProduct = "products";
        private const string EndpointGenerateAffiliateLink = "affiliate/links"; // Endpoint para gerar link

        public TikTokShopApiService(HttpClient httpClient, IConfiguration configuration, ILogger<TikTokShopApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // LEITURA DAS CONFIGURAÇÕES
            _accessToken = _configuration["TikTokShop:AccessToken"] ?? string.Empty; // Pode ser vazio se for OAuth
            _shopId = _configuration["TikTokShop:ShopId"] ?? string.Empty;
            _shopApiBaseUrl = _configuration["TikTokShop:ApiBaseUrl"] ?? throw new InvalidOperationException("TikTokShop:ApiBaseUrl is missing.");

            // NOVAS PROPRIEDADES DE OAUTH
            _clientId = _configuration["TikTokShop:ClientId"] ?? throw new InvalidOperationException("TikTokShop:ClientId is missing.");
            _clientSecret = _configuration["TikTokShop:ClientSecret"] ?? throw new InvalidOperationException("TikTokShop:ClientSecret is missing.");

            _httpClient.BaseAddress = new Uri(_shopApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Só adiciona o Token fixo se ele existir (para não quebrar chamadas OAuth)
            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }

            if (!string.IsNullOrWhiteSpace(_shopId))
            {
                _httpClient.DefaultRequestHeaders.Add("X-TT-SHOP-ID", _shopId);
            }
        }

        public async Task<List<Product>> BuscarProdutosAsync(string query, int pageNumber = 1)
        {
            _logger.LogInformation("Searching TikTok Shop for products with query: {Query}, page: {Page}", query, pageNumber);

            var url = $"{EndpointSearchProducts}?keyword={Uri.EscapeDataString(query)}&page_size=10&page={pageNumber}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("TikTok Shop Search API failed. Status: {StatusCode}", response.StatusCode);
                    return new List<Product>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<TikTokShopProductSearchResponse>(content);
                return apiResponse?.Products?.Select(p => MapToTecFlowProduto(p)).ToList() ?? new List<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado na busca da TikTok Shop.");
                return new List<Product>();
            }
        }

        // --- MÉTODO 2: RENOMEADO PARA CORRESPONDER À INTERFACE 'PublicarAnuncioAsync' ---
        public async Task<bool> PublicarAnuncioAsync(Product produto)
        {
            _logger.LogInformation("Publishing product '{ProductName}' to TikTok Shop.", produto?.Name ?? "Unknown");

            if (produto == null)
            {
                throw new ArgumentNullException(nameof(produto), "Produto não pode ser nulo.");
            }

            var apiProdutoListing = MapToApiProductListing(produto);
            var url = EndpointCreateProduct; // Endpoint para criar listagem

            try
            {
                var jsonContent = JsonSerializer.Serialize(apiProdutoListing);
                var response = await _httpClient.PostAsync(url, new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("TikTok Shop PublicarAnuncioAsync API failed. URL: {Url}, Status: {StatusCode}, Content: {Content}", url, response.StatusCode, errorContent);
                    return false;
                }

                _logger.LogInformation("Product '{ProductName}' published successfully to TikTok Shop.", produto?.Name ?? "Unknown");
                return true;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request to TikTok Shop PublicarAnuncioAsync API failed. URL: {Url}", url);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during TikTok Shop product publishing. URL: {Url}", url);
                return false;
            }
        }

        // --- MÉTODO 3: ADICIONADO PARA CORRESPONDER À INTERFACE 'ObterPerfilConectadoAsync' ---
        // O tipo de retorno 'object' é um placeholder. Se a interface especifica um tipo concreto, use-o.
        public async Task<object> ObterPerfilConectadoAsync()
        {
            _logger.LogInformation("Fetching connected profile information from TikTok Shop.");

            // Endpoint para obter informações do perfil conectado. Ajuste conforme a documentação da API.
            var url = "user/profile"; // Exemplo de endpoint

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("TikTok Shop ObterPerfilConectadoAsync API failed. URL: {Url}, Status: {StatusCode}, Content: {Content}", url, response.StatusCode, errorContent);
                    // Dependendo da sua interface, pode ser null, um objeto vazio, ou lançar exceção.
                    // Assumindo que `object` genérico permite retorno nulo ou um objeto vazio.
                    return new { Message = "Failed to fetch profile" };
                }

                var content = await response.Content.ReadAsStringAsync();
                // Deserializar para um tipo de objeto conhecido, se houver.
                // Por enquanto, retornamos o conteúdo bruta para fins de depuração se não for desserializável.
                _logger.LogInformation("Successfully fetched TikTok Shop profile information.");
                return JsonSerializer.Deserialize<object>(content) ?? new { Message = "Profile fetched, but content is null." };
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request to TikTok Shop ObterPerfilConectadoAsync API failed. URL: {Url}", url);
                return new { Message = "HTTP request failed." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during TikTok Shop profile fetching. URL: {Url}", url);
                return new { Message = "An unexpected error occurred." };
            }
        }

        // --- MÉTODO 4: RENOMEADO E AJUSTADO PARA CORRESPONDER À INTERFACE 'GerarLinkAfiliadoAsync' ---
        // A interface pede um Product inteiro, enquanto o código original usava apenas o ID.
        // Se a interface REALMENTE pede apenas o ID, ajuste a interface e aqui.
        // Assumindo que a interface pede um Produto, e que precisamos extrair o ID dele.
        public async Task<string?> GerarLinkAfiliadoAsync(Product produto)
        {
            if (produto == null)
            {
                _logger.LogError("Produto is null when trying to generate affiliate link.");
                return null;
            }

            _logger.LogInformation("Generating affiliate link for product ID {ProductId} on TikTok Shop.", produto.Id);

            // O corpo da requisição para gerar link pode precisar de mais campos além do ID.
            // Verifique a documentação da API.
            var requestBody = new { product_id = produto.Id.ToString() /* , campaign_id: "optional_campaign_id", etc. */ };
            var url = EndpointGenerateAffiliateLink;

            try
            {
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var response = await _httpClient.PostAsync(url, new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("TikTok Shop GerarLinkAfiliadoAsync API failed. URL: {Url}, Status: {StatusCode}, Content: {Content}", url, response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<TikTokShopAffiliateLinkResponse>(content);

                var affiliateLink = apiResponse?.Data?.AffiliateLink;
                if (string.IsNullOrWhiteSpace(affiliateLink))
                {
                    _logger.LogWarning("TikTok Shop GerarLinkAfiliadoAsync API returned a link but it was empty or null. Response: {Content}", content);
                    return null;
                }

                _logger.LogInformation("Affiliate link generated successfully for product ID {ProductId}: {AffiliateLink}", produto.Id, affiliateLink);
                return affiliateLink;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request to TikTok Shop GerarLinkAfiliadoAsync API failed. URL: {Url}", url);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during TikTok Shop affiliate link generation. URL: {Url}", url);
                return null;
            }
        }

        public async Task<string> TrocarCodigoPorTokenAsync(string code)
        {
            _logger.LogInformation("Trocando código de autorização pelo Access Token do TikTok.");

            var url = "https://auth.tiktok-br.com/oauth/token";

            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "app_id", _clientId },        // Use a propriedade da classe
                { "app_secret", _clientSecret } // Use a propriedade da classe
            };

            var content = new FormUrlEncodedContent(requestBody);

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Falha ao trocar código TikTok: {Error}", error);
                throw new Exception("Falha na autenticação com TikTok.");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString() ?? throw new Exception("Token não encontrado.");
        }


        private class TikTokShopProductSearchResponse // ...
        {
            [JsonPropertyName("products")] public List<ApiProduct>? Products { get; set; }
            [JsonPropertyName("cursor")] public string? Cursor { get; set; }
            [JsonPropertyName("total")] public int Total { get; set; }
        }

        private class ApiProduct
        {
            [JsonPropertyName("id")] public string? Id { get; set; }
            [JsonPropertyName("title")] public string? Title { get; set; }
            [JsonPropertyName("description")] public string? Description { get; set; }
            [JsonPropertyName("price")] public decimal Price { get; set; }
            [JsonPropertyName("main_image_url")] public string? MainImageUrl { get; set; }
            [JsonPropertyName("category_id")] public string? CategoryId { get; set; }
            [JsonPropertyName("stock_info")] public StockInfo? StockInfo { get; set; }
        }

        private class StockInfo { [JsonPropertyName("stock_count")] public int StockCount { get; set; } }

        private class ApiProductListingRequest
        {
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
            [JsonPropertyName("price")] public PriceInfo? Price { get; set; }
            [JsonPropertyName("images")] public List<string> Images { get; set; } = new List<string>();
            [JsonPropertyName("category_id")] public string CategoryId { get; set; } = string.Empty;
        }

        private class PriceInfo
        {
            [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
            [JsonPropertyName("currency_code")] public string CurrencyCode { get; set; } = "USD";
        }

        private class TikTokShopAffiliateLinkResponse
        {
            [JsonPropertyName("data")] public AffiliateLinkData? Data { get; set; }
            [JsonPropertyName("code")] public string? Code { get; set; }
            [JsonPropertyName("message")] public string? Message { get; set; }
        }

        private class AffiliateLinkData
        {
            [JsonPropertyName("affiliate_link")] public string? AffiliateLink { get; set; }
        }

        // --- Métodos de Mapeamento --- (Mantenha a lógica original)

        private Product MapToTecFlowProduto(ApiProduct apiProduct)
        {
            return new Product
            {
                Id = int.TryParse(apiProduct.Id, out var id) ? id : 0,
                Name = apiProduct.Title ?? "Untitled Product",
                Summary = apiProduct.Description ?? string.Empty,
                Price = apiProduct.Price,
                MainImageUrl = apiProduct.MainImageUrl,
                Category = apiProduct.CategoryId ?? "Unknown",
                // ... outros campos
            };
        }

        private ApiProductListingRequest MapToApiProductListing(Product produto)
        {
            if (produto == null)
                throw new ArgumentNullException(nameof(produto));

            return new ApiProductListingRequest
            {
                Title = produto?.Name ?? "Product Name Not Available",
                Description = $"TecFlow Product: {produto?.Name}. {produto?.Summary ?? produto?.Features ?? ""}",
                Price = new PriceInfo { Value = (produto?.Price ?? 0m).ToString("F2") },
                Images = MapProductImages(produto),
                CategoryId = GetCategoryId(produto?.Category)
            };
        }

        private List<string> MapProductImages(Product? produto)
        {
            var images = new List<string>();
            if (produto?.ImageUrls != null && produto.ImageUrls.Any()) images.AddRange(produto.ImageUrls);
            if (!string.IsNullOrWhiteSpace(produto?.MainImageUrl) && !images.Contains(produto.MainImageUrl)) images.Insert(0, produto.MainImageUrl);
            return images.Take(5).ToList();
        }

        private string GetCategoryId(string? TecFlowCategory)
        {
            if (string.IsNullOrWhiteSpace(TecFlowCategory)) return "0";
            switch (TecFlowCategory.ToLower())
            {
                case "eletrônicos": return "1001";
                case "moda": return "1002";
                case "casa e decoração": return "1003";
                case "beleza e saúde": return "1004";
                default: return "0";
            }
        }
    }
}