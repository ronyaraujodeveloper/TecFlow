// Caminho Completo: TecFlow.Infrastructure\Services\ExternalServices\GeminiService.cs

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
using System.Threading;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Service.ExternalServices
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;
        private readonly string? _apiKey;
        private readonly string _geminiApiUrl;
        //private readonly string _geminiModel = "gemini-pro";

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _apiKey = _configuration["GoogleAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogError("Google Gemini API key not configured. Please set GoogleAI:ApiKey in configuration.");
                throw new InvalidOperationException("Google Gemini API key not configured.");
            }

            _geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // --- IMPLEMENTAÇÃO DOS MÉTODOS GENÉRICOS QUE FALTAVAM ---

        /// <summary>
        /// Gera uma descrição genérica baseada em um prompt usando o Google Gemini.
        /// </summary>
        public async Task<string> GenerateDescriptionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return "Prompt de descrição vazio.";
            // Podemos usar um prompt base mais genérico aqui, ou apenas usar o prompt passado.
            // Para simplicidade, usaremos o prompt passado diretamente.
            var result = await CallGeminiApiAsync(prompt, maxOutputTokens: 150, temperature: 0.7f, cancellationToken: cancellationToken);
            return result ?? "Falha ao gerar descrição genérica com Gemini.";
        }

        /// <summary>
        /// Gera um script genérico baseado em um prompt usando o Google Gemini.
        /// </summary>
        public async Task<string> GenerateScriptAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return "Prompt de script vazio.";
            var result = await CallGeminiApiAsync(prompt, maxOutputTokens: 800, temperature: 0.7f, cancellationToken: cancellationToken);
            return result ?? "Falha ao gerar script genérico com Gemini.";
        }

        /// <summary>
        /// Gera um título genérico baseado em um prompt usando o Google Gemini.
        /// </summary>
        public async Task<string> GenerateTitleAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return "Prompt de título vazio.";
            // O Gemini pode retornar uma lista formatada ou apenas um título.
            // Para manter a consistência com a interface (espera string), pegamos o primeiro resultado.
            var result = await CallGeminiApiAsync(prompt, maxOutputTokens: 50, temperature: 0.8f, cancellationToken: cancellationToken);
            return result ?? "Falha ao gerar título genérico com Gemini.";
        }

        /// <summary>
        /// Gera um resumo de texto genérico usando o Google Gemini.
        /// </summary>
        public async Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Texto para resumir vazio.";
            // Usa uma lógica de prompt similar a GerarResumoAsync, mas como método separado da interface.
            var finalPrompt = $"Resuma o seguinte texto de forma concisa e objetiva:\n\n{text}";
            var result = await CallGeminiApiAsync(finalPrompt, maxOutputTokens: 200, temperature: 0.5f, cancellationToken: cancellationToken);
            return result ?? "Falha ao gerar resumo genérico com Gemini.";
        }


        // --- IMPLEMENTAÇÃO DOS MÉTODOS ESPECÍFICOS (JÁ EXISTENTES NA VERSÃO ANTERIOR) ---

        public async Task<string> GerarDescricaoProdutoAsync(Product produto, string contextoAdicional = "")
        {
            if (produto == null) return "Produto não especificado.";
            var prompt = BuildDescricaoProdutoPrompt(produto, contextoAdicional);
            var result = await CallGeminiApiAsync(prompt, maxOutputTokens: 150, temperature: 0.7f);
            return result ?? "Falha ao gerar descrição do produto com Gemini.";
        }

        public async Task<List<string>> GerarVariacoesTitulosAsync(Product produto, int quantidade = 5, string contexto = "")
        {
            if (produto == null) return new List<string> { "Produto não especificado." };
            var prompt = BuildTitulosPrompt(produto, quantidade);
            var result = await CallGeminiApiAsync(prompt, maxOutputTokens: 200, temperature: 0.8f, topP: 0.9f);

            if (string.IsNullOrWhiteSpace(result))
                return new List<string> { "Erro ao gerar títulos com Gemini." };

            var titles = ParseStringListResponse(result, quantidade);
            return titles.Any() ? titles : new List<string> { "Nenhum título gerado com Gemini." };
        }

        public async Task<string> GerarScriptVideoAsync(Product produto, string formato, int duracaoEstimadaSegundos)
        {
            if (produto == null) return "Produto não especificado.";
            var prompt = BuildScriptVideoPrompt(produto, formato, duracaoEstimadaSegundos);
            var result = await CallGeminiApiAsync(prompt, maxOutputTokens: 800, temperature: 0.7f);
            return result ?? "Falha ao gerar script de vídeo com Gemini.";
        }

        public async Task<string> GerarPromptCriativoVisualAsync(string descricaoConteudo, string estiloVisual, string plataformaAlvo)
        {
            var prompt = BuildPromptCriativoVisualPrompt(descricaoConteudo, estiloVisual, plataformaAlvo);
            var result = await CallGeminiApiAsync(prompt, maxOutputTokens: 300, temperature: 0.8f);
            return result ?? "Falha ao gerar prompt visual com Gemini.";
        }

        public async Task<string> GerarResumoAsync(string text)
        {
            return await GerarResumoAsync(text, default);
        }

        public async Task<string> GerarResumoAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var finalPrompt = $"Resuma o seguinte texto de forma concisa e objetiva.:\n\n{prompt}";
            var result = await CallGeminiApiAsync(finalPrompt, maxOutputTokens: 200, temperature: 0.5f, cancellationToken: cancellationToken);
            return result ?? "Falha ao gerar resumo com Gemini.";
        }

        // --- Builders de Prompt ---
        // Adicionamos builders básicos para os métodos Generate...Async, caso o prompt passado não seja suficiente.
        // Se a INTENÇÃO for usar o prompt passado sem modificação, remova este builder e use o prompt diretamente no CallGeminiApiAsync.

        // Não criamos builders específicos para Generate...Async pois eles esperam um prompt direto.
        // Se você quiser que `GenerateDescriptionAsync` use um prompt template como os de Produto,
        // por exemplo, você precisaria de lógica adicional aqui. Por enquanto, usa-se o prompt direto.

        // Builders para métodos específicos (mantidos)
        private string BuildDescricaoProdutoPrompt(Product produto, string contextoAdicional)
        {
            return $@"Crie uma descrição de produto **persuasiva e concisa** para o seguinte item:
Produto: {produto?.Name ?? "Nome não disponível"}
Características principais: {produto?.Features ?? "Não listado"}
Benefícios: {produto?.Benefits ?? "Não listado"}
Público-alvo: {produto?.TargetAudience ?? "Geral"}
Contexto adicional: {contextoAdicional}

A descrição deve ter entre 50 e 100 palavras, com tom amigável e profissional e focada nos benefícios para o cliente. Destaque o valor único do produto.
Retorne APENAS o texto da descrição, sem introduções ou conclusões.";
        }

        private string BuildTitulosPrompt(Product produto, int quantidade)
        {
            return $@"Gere {quantidade} títulos **únicos e atraentes** para o seguinte produto:
Produto: {produto?.Name ?? "Nome não disponível"}
Categoria: {produto?.Category ?? "Não especificada"}
Breve descrição: {produto?.Summary ?? "Não disponível"}

Os títulos devem ser curtos (idealmente até 60 caracteres), otimizados para SEO, e despertar o interesse do comprador.
Retorne os títulos como uma lista separada por quebra de linha. Cada título deve começar com um hífen (-).";
        }

        private string BuildScriptVideoPrompt(Product produto, string formato, int duracaoEstimadaSegundos)
        {
            return $@"Crie um script de vídeo **dinâmico e envolvente** para promover o produto:
Produto: {produto?.Name ?? "Nome não disponível"}
Formato do vídeo: {formato} (ex: Reel, TikTok, Anúncio Curto)
Duração estimada: {duracaoEstimadaSegundos} segundos

O script deve incluir:
1.  Uma introdução cativante (primeiros 3-5 segundos).
2.  Apresentação clara do produto e seus principais benefícios.
3.  Um call-to-action (CTA) forte e claro.
4.  Sugestões de elementos visuais ou de narração.
O resultado deve ser um script completo, pronto para gravação, sem introduções ou conclusões sobre a tarefa.";
        }

        private string BuildPromptCriativoVisualPrompt(string descricaoConteudo, string estiloVisual, string plataformaAlvo)
        {
            return $@"Gere um prompt **detalhado e criativo** para criação de imagem/visual em uma IA generativa.
Descrição do Conteúdo Desejado: {descricaoConteudo}
Estilo Visual de Referência: {estiloVisual} (ex: Fotorrealista, Ilustração vetorial, Arte conceitual, Pastel, Cyberpunk)
Plataforma de Exibição: {plataformaAlvo} (ex: Instagram Feed, Pinterest, Website Banner)

O prompt gerado deve ser otimizado para a plataforma alvo e incluir detalhes sobre iluminação, composição, paleta de cores, e elementos chave para guiar a IA.
Retorne APENAS o prompt completo.";
        }

        // Chamada para Gemini (REST)
        private async Task<string?> CallGeminiApiAsync(string prompt, int maxOutputTokens = 150, float temperature = 0.7f, float topP = 1.0f, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                _logger.LogWarning("Attempted to call Gemini API with an empty prompt.");
                return null;
            }

            // Estrutura da requisição para o Gemini API (pode variar ligeiramente entre modelos/versões)
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens,
                    temperature,
                    topP
                }
            };

            var url = $"{_geminiApiUrl}?key={_apiKey}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            string content = "";
            try
            {
                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API request failed. URL: {Url}, Status: {StatusCode}, Content: {Content}", url, response.StatusCode, content);
                    return null;
                }

                var geminiResp = JsonSerializer.Deserialize<GeminiResponse>(content);
                var generatedText = geminiResp?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    _logger.LogWarning("Gemini API returned no text content. Response: {Content}", content);
                    return null;
                }

                return generatedText;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request to Gemini API failed. URL: {Url}. Attempted content: {Content}", url, content);
                return null;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing Gemini API response. URL: {Url}. Content: {Content}", url, content);
                return null;
            }
            catch (TaskCanceledException tce) when (tce.CancellationToken == cancellationToken)
            {
                _logger.LogInformation("Gemini API call was cancelled. URL: {Url}", url);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while calling Gemini API. URL: {Url}. Attempted content: {Content}", url, content);
                return null;
            }
        }

        // --- Modelos de Resposta da API Gemini (Simplificados) ---
        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidate> Candidates { get; set; } = new List<GeminiCandidate>();
        }

        private class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public required GeminiContent Content { get; set; }
        }

        private class GeminiContent
        {
            [JsonPropertyName("parts")]
            public required List<GeminiPart> Parts { get; set; }
        }

        private class GeminiPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        // --- Método de Helper para Parsing de Listas ---
        private List<string> ParseStringListResponse(string responseText, int expectedCount)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return new List<string>();

            var lines = responseText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var titles = lines
                .Select(l => l.TrimStart('-', ' ', '\t').Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            return titles;
        }
    }
}