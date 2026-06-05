using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization; // Para atributos de serialização
using System.Threading.Tasks;
using System.Web; // Para Uri.EscapeDataString
using TecFlow.Core.Entities; // Para 'Produto'
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Interfaces; // Para 'IAppConfiguration'

namespace TecFlow.Infrastructure.Services.Service.ExternalServices
{
    /// <summary>
    /// Implementação para o serviço externo Shopee API.
    /// </summary>
    public class ShopeeApiService : IShopeeApi
    {
        private readonly HttpClient _httpClient;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger<ShopeeApiService> _logger;

        private const string SHOPEE_API_BASE_URL_SANDBOX = "https://partner.shopeesandbox.io/open/v1/";


        public ShopeeApiService(HttpClient httpClient, IAppConfiguration appConfiguration, ILogger<ShopeeApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _appConfiguration = appConfiguration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogWarning("ShopeeApiService instantiated without IAppConfiguration. Certain functionalities might fail.");

            // BaseAddress é definido aqui se não vier do registro
            if (_httpClient.BaseAddress == null)
                _httpClient.BaseAddress = new Uri(_appConfiguration.Shopee_ApiBaseUrl);
        }

        // --- Implementação dos Métodos da IShopeeApi ---

        /// <summary>
        /// Busca produtos na Shopee com base em critérios de pesquisa.
        /// </summary>
        /// <param name="query">Termo de busca.</param>
        /// <param name="limit">Número máximo de resultados.</param>
        /// <param name="offset">Índice inicial dos resultados.</param>
        /// <returns>Uma lista de produtos encontrados (ShopeeProductResult).</returns>
        public async Task<IEnumerable<ShopeeProductResult>> BuscarProdutosAsync(string query, int limit = 10, int offset = 0)
        {
            _logger.LogInformation("Buscando produtos na Shopee para query: '{Query}' (Limit: {Limit}, Offset: {Offset})", query, limit, offset);

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("BuscarProdutosAsync chamado com query em branco.");
                return Enumerable.Empty<ShopeeProductResult>();
            }

            var escapedQuery = Uri.EscapeDataString(query);
            var requestUrl = $"v1/search/items?keyword={escapedQuery}&limit={limit}&offset={offset}";

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // --- DESERIALIZAÇÃO REAL E RETORNO GARANTIDO ---
                    IEnumerable<ShopeeProductResult>? productResults = null; // Inicializa como null

                    // Tenta desserializar o wrapper principal
                    var searchResponseWrapper = JsonConvert.DeserializeObject<ShopeeSearchResponseWrapper>(content);

                    if (searchResponseWrapper != null && searchResponseWrapper.Success && searchResponseWrapper.Data != null && searchResponseWrapper.Data.Items != null)
                    {
                        // A desserialização para ShopeeSearchResponseWrapper e, em seguida, para a lista de Items (que é List<ShopeeProductResult>)
                        // deve funcionar.
                        productResults = searchResponseWrapper.Data.Items; // searchResponseWrapper.Data.Items é do tipo List<ShopeeProductResult>, que é compatível com IEnumerable<ShopeeProductResult>
                        _logger.LogInformation("Busca de produtos Shopee bem-sucedida. Encontrados {Count} itens.", productResults.Count());
                    }
                    else
                    {
                        // Se falhou na desserialização ou resposta inválida, loga o erro.
                        var errorMsg = searchResponseWrapper?.Error?.Message ?? "Resposta de busca inválida ou vazia.";
                        _logger.LogWarning("Shopee API (buscar produtos) retornou '{Status}' com erro: {ErrorMsg}. Conteúdo: {Content}", response.StatusCode, errorMsg, content);
                        // Neste ponto, productResults ainda é null.
                    }

                    // Retorna os resultados obtidos, ou uma lista vazia se null.
                    return productResults ?? Enumerable.Empty<ShopeeProductResult>();
                    // --- FIM DA GARANTIA DE RETORNO ---

                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Shopee API (buscar produtos) falhou: {StatusCode}. Resposta: {ErrorContent}", response.StatusCode, errorContent);
                    return Enumerable.Empty<ShopeeProductResult>(); // Retorna lista vazia em caso de falha HTTP.
                }
            }
            catch (JsonException jEx) { _logger.LogError(jEx, "Erro ao desserializar JSON para BuscarProdutosAsync."); return Enumerable.Empty<ShopeeProductResult>(); }
            catch (HttpRequestException httpEx) { _logger.LogError(httpEx, "Erro HTTP em BuscarProdutosAsync."); throw; }
            catch (Exception ex) { _logger.LogError(ex, "Erro inesperado em BuscarProdutosAsync."); throw; }
        }
        public async Task<string> GerarLinkCurtoAsync(string longUrl)
        {
            // Implementar lógica de encurtador aqui ou retornar NotImplementedException
            return await Task.FromResult("link_encurtado_exemplo");
        }

        public async Task<bool> ValidarConfiguracaoContaAsync()
        {
            // Implementar validação de token/config
            return await Task.FromResult(true);
        }

        public async Task<List<Product>> BuscarProdutosAfiliadosAsync(string keyword)
        {
            // A Shopee exige assinatura via HMAC-SHA256
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GerarAssinatura("api/v2/product/search", timestamp);
            var _baseUrl = "";

            var url = $"{_baseUrl}/api/v2/product/search?keyword={keyword}&timestamp={timestamp}&sign={sign}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<Product>();

            var json = await response.Content.ReadAsStringAsync();
            // Desserialização e MapToTecFlowProduto...
            return new List<Product>();
        }

        private string GerarAssinatura(string path, long timestamp)
        {
            // Lógica de criptografia HMAC SHA256 com sua PartnerKey
            // Use System.Security.Cryptography.HMACSHA256
            return "assinatura_hash_aqui";
        }

        // --- Outros Métodos (PublicarAnuncioAsync, ObterPerfilConectadoAsync, GerarLinkAfiliadoAsync) ---
        // Mantidos da última correção (assumindo que estes estão corretos agora)

        /// <summary>
        /// Publica um anúncio/link de afiliado de um produto específico na Shopee.
        /// </summary>
        async Task<bool> IShopeeApi.PublicarAnuncioAsync(Product produto)
        {
            var result = await PublicarAnuncioDetalhadoAsync(produto);
            return result.Success;
        }

        public async Task<ShopeeAffiliateLinkResponse> PublicarAnuncioDetalhadoAsync(Product produto)
        {
            _logger.LogInformation("Publicando anúncio na Shopee para produto: '{NomeProduto}'", produto.Name);

            var requestUrl = $"v1/product/create"; // Exemplo de endpoint de criação
            var jsonPayload = MapProdutoToShopeeApiPayload(produto);

            var defaultErrorResponse = new ShopeeAffiliateLinkResponse
            {
                Success = false,
                Error = new ShopeeError { Code = "PUBLISH_FAILED", Message = "Falha ao publicar anúncio." }
            };

            try
            {
                var response = await _httpClient.PostAsync(requestUrl, new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ShopeeAffiliateLinkResponse>(content);

                    if (result == null || !result.Success)
                    {
                        _logger.LogWarning("Shopee API (publicar) retornou sucesso HTTP mas falhou internamente. Erro: {Error}", result?.Error?.Message ?? "Resposta nula.");
                        return result ?? defaultErrorResponse;
                    }

                    _logger.LogInformation("Anúncio para produto '{NomeProduto}' publicado com (aparente) sucesso na Shopee.", produto.Name);
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Shopee API (publish) falhou: {StatusCode}. Resposta: {ErrorContent}", response.StatusCode, errorContent);
                    try
                    {
                        var errorDetail = JsonConvert.DeserializeObject<ShopeeError>(errorContent);

                        if (errorDetail != null)
                        {
                            defaultErrorResponse.Error = errorDetail;
                            defaultErrorResponse.Error.Message = $"Shopee API Error: {errorDetail.Message}";
                        }
                        else
                        {
                            defaultErrorResponse.Error = new ShopeeError
                            {
                                Code = "PUBLISH_FAILED",
                                Message = "Shopee API Error: resposta inválida."
                            };
                        }
                    }
                    catch { /* Ignora erro ao tentar desserializar o erro */ }
                    return defaultErrorResponse;
                }
            }
            catch (JsonException jEx) { _logger.LogError(jEx, "Erro ao desserializar JSON em PublicarAnuncioAsync."); return defaultErrorResponse; }
            catch (HttpRequestException httpEx) { _logger.LogError(httpEx, "Erro HTTP em PublicarAnuncioAsync."); throw; }
            catch (Exception ex) { _logger.LogError(ex, "Erro inesperado em PublicarAnuncioAsync."); throw; }
        }

        /// <summary>
        /// Obtém as informações de perfil conectado (credenciais).
        /// </summary>
        /// <returns>Informações do perfil conectado (como object).</returns>
        public async Task<object> ObterPerfilConectadoAsync()
        {
            _logger.LogInformation("Obtendo perfil conectado da Shopee API...");

            var requestUrl = $"v1/user/me"; // Exemplo de endpoint do perfil

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var profileWrapper = JsonConvert.DeserializeObject<ShopeeApiResponse<ShopeeProfileData>>(content);
                    if (profileWrapper != null && profileWrapper.Success && profileWrapper.Data != null)
                    {
                        _logger.LogInformation("Perfil Shopee obtido com sucesso.");
                        return profileWrapper.Data;
                    }
                    else
                    {
                        var errorMsg = profileWrapper?.Error?.Message ?? "Resposta de perfil inválida ou vazia.";
                        _logger.LogWarning("Shopee API (perfil) retornou '{Status}' com erro: {ErrorMsg}.", response.StatusCode, errorMsg);
                        return new ShopeeError { Code = "PROFILE_FETCH_ERROR", Message = errorMsg };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Shopee API (profile) falhou: {StatusCode}. Resposta: {ErrorContent}", response.StatusCode, errorContent);
                    return new ShopeeError { Code = response.StatusCode.ToString(), Message = $"Failed to get profile: {errorContent}" };
                }
            }
            catch (JsonException jEx)
            {
                _logger.LogError(jEx, "Erro ao desserializar JSON em ObterPerfilConectadoAsync.");
                return new ShopeeError { Code = "JSON_PARSE_ERROR", Message = "Failed to parse JSON response." };
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Erro HTTP em ObterPerfilConectadoAsync.");
                throw; // Relança exceção HTTP
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado em ObterPerfilConectadoAsync.");
                throw;
            }
        }

        // Método para gerar link de afiliado (assinatura correta da interface)
        public Task<ShopeeAffiliateLinkResponse> GerarLinkAfiliadoAsync(string productId, string? campaignId = null)
        {
            _logger.LogInformation("Gerando link de afiliado Shopee para productId: '{ProductId}'", productId);

            if (string.IsNullOrWhiteSpace(productId))
            {
                _logger.LogWarning("GerarLinkAfiliadoAsync chamado com productId em branco.");
                return Task.FromResult(new ShopeeAffiliateLinkResponse { Success = false, Error = new ShopeeError { Code = "INVALID_PARAM", Message = "productId cannot be empty." } });
            }
            if (_appConfiguration == null)
            {
                _logger.LogError("IAppConfiguration não está disponível.");
                return Task.FromResult(new ShopeeAffiliateLinkResponse { Success = false, Error = new ShopeeError { Code = "CONFIG_ERROR", Message = "Shopee API configuration is missing." } });
            }

            var requestUrl = $"v1/affiliate/link"; // Exemplo de endpoint
            var requestBody = new
            {
                partner_id = _appConfiguration.Shopee_PartnerId,
                item_id = productId,
                campaign_id = campaignId ?? "",
            };

            try
            {
                return CallShopeeApiAsync<ShopeeAffiliateLinkResponse>(requestUrl, HttpMethod.Post, requestBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao chamar ShopeeApiService.GerarLinkAfiliadoAsync.");
                return Task.FromResult(new ShopeeAffiliateLinkResponse { Success = false, Error = new ShopeeError { Code = "UNEXPECTED_ERROR", Message = "An unexpected error occurred." } });
            }
        }

        // --- Métodos Auxiliares ---
        private string MapProdutoToShopeeApiPayload(Product produto)
        {
            var payload = new
            {
                item_name = produto.Name ?? "Produto Sem Nome",
                description = produto.Description ?? produto.Summary ?? "Descrição não fornecida.",
                price = produto.Price.ToString("F2"),
                stock_info = new { stock_count = produto.Stock > 0 ? produto.Stock : 100 },
                category_id = GetShopeeCategoryId(produto.Category),
                images = MapProdutoImagesToShopee(produto),
                attributes = new[] {
                    new { name = "Cor", value = produto.Color ?? "Preto" },
                    new { name = "Material", value = produto.Material ?? "Plástico" }
                }
            };
            return JsonConvert.SerializeObject(payload);
        }

        private List<string> MapProdutoImagesToShopee(Product produto)
        {
            var images = new List<string>();
            if (!string.IsNullOrWhiteSpace(produto.MainImageUrl)) images.Add(produto.MainImageUrl);
            if (produto.ImageUrls != null) images.AddRange(produto.ImageUrls.Where(url => !string.IsNullOrWhiteSpace(url)));
            return images.Take(8).ToList();
        }

        private string GetShopeeCategoryId(string? TecFlowCategory)
        {
            switch (TecFlowCategory?.Trim().ToLower())
            {
                case "eletrônicos": return "100101";
                case "moda": return "100202";
                case "casa e decoração": return "100303";
                default:
                    _logger.LogWarning("Categoria TecFlow '{TecFlowCategory}' não mapeada. Usando ID padrão '0'.", TecFlowCategory);
                    return "0";
            }
        }

        private async Task<TResponse> CallShopeeApiAsync<TResponse>(string requestUrl, HttpMethod httpMethod, object? requestBody = null) where TResponse : class
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, requestUrl);

            if (requestBody != null)
            {
                var jsonPayload = JsonConvert.SerializeObject(requestBody);
                requestMessage.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var successResponse = JsonConvert.DeserializeObject<TResponse>(content);
                if (successResponse == null) throw new JsonException("Deserialized response is null.");

                if (successResponse is IShopeeErrorResponse errorAwareResponse && !errorAwareResponse.Success)
                {
                    _logger.LogWarning("Shopee API ({Endpoint}) retornou sucesso HTTP mas falhou internamente. Erro: {Error}", requestUrl, errorAwareResponse.Error?.Message ?? "Mensagem de erro não disponível.");
                    return successResponse;
                }

                _logger.LogInformation("Shopee API ({Endpoint}) chamada com sucesso.", requestUrl);
                return successResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Shopee API ({Endpoint}) falhou. Status: {StatusCode}. Resposta: {ErrorContent}", requestUrl, response.StatusCode, errorContent);

                if (typeof(TResponse).GetInterfaces().Contains(typeof(IShopeeErrorResponse)))
                {
                    try
                    {
                        var shopeeError = JsonConvert.DeserializeObject<ShopeeError>(errorContent) ?? new ShopeeError { Code = "UNKNOWN_API_ERROR", Message = "Unknown API error occurred." };
                        var errorResult = Activator.CreateInstance<TResponse>();
                        typeof(TResponse).GetProperty("Success")?.SetValue(errorResult, false);
                        typeof(TResponse).GetProperty("Error")?.SetValue(errorResult, shopeeError);
                        return errorResult;
                    }
                    catch { }
                }

                throw new HttpRequestException($"Shopee API request failed for {requestUrl} with status {response.StatusCode}. Error: {errorContent}");
            }
        }
    }

    // --- DTOs de Resposta da Shopee API ---

    public interface IShopeeErrorResponse
    {
        bool Success { get; set; }
        ShopeeError? Error { get; set; }
    }

    /// <summary>
    /// DTO para um item de produto retornado na lista de busca.
    /// </summary>
    public class ShopeeProductResult : IShopeeErrorResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; } = true;
        // NOTE: Para resultados de busca, a SHOPEE API V1/V2 frequentemente retorna um wrapper.
        // Se o método BuscarProdutosAsync retorna um wrapper, a deserialização deve ser para ele.
        // Se o método *espera* retornar uma lista direta de ShopeeProductResult, então este DTO
        // precisa CORRESPONDER ao que está DENTRO da lista retornada pela API.
        // Assumindo que ShopeeSearchResponseWrapper.Data.Items retorna objetos com estas propriedades.
        [JsonPropertyName("item_id")] public string ItemId { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("price")] public decimal Price { get; set; }
        [JsonPropertyName("stock_info")] public StockInfoResult? StockInfo { get; set; }
        [JsonPropertyName("error")] public ShopeeError? Error { get; set; }
    }

    /// <summary>
    /// Wrapper para a resposta geral de busca de produtos.
    /// </summary>
    public class ShopeeSearchResponseWrapper
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public ShopeeSearchData? Data { get; set; }
        [JsonPropertyName("error")] public ShopeeError? Error { get; set; }
    }

    /// <summary>
    /// Dados contendo a lista de itens na resposta de busca.
    /// </summary>
    public class ShopeeSearchData
    {
        [JsonPropertyName("items")] public List<ShopeeProductResult>? Items { get; set; } // Lista de DTOs de itens individuais.
        [JsonPropertyName("total_count")] public int TotalCount { get; set; }
        [JsonPropertyName("next_cursor")] public string? NextCursor { get; set; }
    }

    /// <summary>
    /// DTO para informações de estoque.
    /// </summary>
    public class StockInfoResult
    {
        [JsonPropertyName("stock_count")] public int StockCount { get; set; }
    }

    /// <summary>
    /// DTO para a resposta de geração de link de afiliado.
    /// </summary>
    public class ShopeeAffiliateLinkResponse : IShopeeErrorResponse // Já implementa a interface.
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public ShopeeAffiliateLinkData? Data { get; set; }
        [JsonPropertyName("error")] public ShopeeError? Error { get; set; }
    }

    /// <summary>
    /// DTO com os dados do link de afiliado gerado.
    /// </summary>
    public class ShopeeAffiliateLinkData
    {
        [JsonPropertyName("affiliate_url")] public string? AffiliateUrl { get; set; }
        [JsonPropertyName("campaign_id")] public string? CampaignId { get; set; }
    }

    /// <summary>
    /// DTO para erros genéricos retornados pela Shopee API.
    /// </summary>
    public class ShopeeError
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("details")] public object? Details { get; set; }
    }

    /// <summary>
    /// DTO para dados do perfil do parceiro (utilizado em ObterPerfilConectadoAsync).
    /// </summary>
    public class ShopeeProfileData
    {
        [JsonPropertyName("partner_id")] public string PartnerId { get; set; } = string.Empty;
        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wrapper genérico usado para respostas que seguem o padrão { success: ..., data: ..., error: ... }.
    /// </summary>
    public class ShopeeApiResponse<T>
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public T? Data { get; set; }
        [JsonPropertyName("error")] public ShopeeError? Error { get; set; }
    }
}