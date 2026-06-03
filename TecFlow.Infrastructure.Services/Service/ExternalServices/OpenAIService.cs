// Caminho Completo: TecFlow.Infrastructure\Services\ExternalServices\OpenAIService.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading; // Necessário para CancellationToken
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Service.ExternalServices
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string? _apiKey;

        // Endpoints REST da API OpenAI
        private const string ApiUrlCompletions = "https://api.openai.com/v1/completions";
        private const string ApiUrlChatCompletions = "https://api.openai.com/v1/chat/completions"; // Para modelos como gpt-3.5-turbo

        // Modelos recomendados (pode ser configurável)
        private const string ModelTextDavinci003 = "text-davinci-003"; // Modelo mais antigo, mas poderoso para completions
        private const string ModelGpt35Turbo = "gpt-3.5-turbo"; // Modelo de chat, geralmente mais econômico e eficiente

        public OpenAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogError("OpenAI API key not configured. Please set OpenAI:ApiKey in configuration.");
                throw new InvalidOperationException("OpenAI API key not configured.");
            }

            // Configuração padrão do HttpClient
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // --- IMPLEMENTAÇÃO DOS MÉTODOS DA IAIService ---

        /// <summary>
        /// Gera uma descrição de produto usando a entidade `Produto`.
        /// </summary>
        public async Task<string> GerarDescricaoProdutoAsync(Product produto, string contextoAdicional = "")
        {
            if (produto == null) return "Produto não especificado.";

            var prompt = BuildDescricaoProdutoPrompt(produto, contextoAdicional);
            // Usar o modelo de Completions pode ser mais direto para descrições textuais
            var result = await CallCompletionsApiAsync(prompt, maxTokens: 150, temperature: 0.7f);
            return result ?? "Falha ao gerar descrição do produto.";
        }

        /// <summary>
        /// Gera variações de títulos para um produto usando a entidade `Produto`.
        /// </summary>
        public async Task<List<string>> GerarVariacoesTitulosAsync(Product produto, int quantidade = 5, string contexto = "")
        {
            if (produto == null) return new List<string> { "Produto não especificado." };

            var prompt = BuildTitulosPrompt(produto, quantidade);
            var result = await CallCompletionsApiAsync(prompt, maxTokens: 200, temperature: 0.8f, frequencyPenalty: 0.1f, presencePenalty: 0.1f);

            if (string.IsNullOrWhiteSpace(result))
                return new List<string> { "Erro ao gerar títulos." };

            // Parse personalizado para listas quando o resultado esperado é uma lista
            var titles = ParseStringListResponse(result, quantidade);
            return titles.Any() ? titles : new List<string> { "Nenhum título gerado." };
        }

        /// <summary>
        /// Gera um script de vídeo.
        /// </summary>
        public async Task<string> GerarScriptVideoAsync(Product produto, string formato, int duracaoEstimadaSegundos)
        {
            if (produto == null) return "Produto não especificado.";

            var prompt = BuildScriptVideoPrompt(produto, formato, duracaoEstimadaSegundos);
            // Scripts de vídeo podem se beneficiar de um modelo de Chat mais conversacional
            var result = await CallChatCompletionsApiAsync(prompt, maxTokens: 800, temperature: 0.7f);
            return result ?? "Falha ao gerar script de vídeo.";
        }

        /// <summary>
        /// Gera um prompt criativo visual.
        /// </summary>
        public async Task<string> GerarPromptCriativoVisualAsync(string descricaoConteudo, string estiloVisual, string plataformaAlvo)
        {
            var prompt = BuildPromptCriativoVisualPrompt(descricaoConteudo, estiloVisual, plataformaAlvo);
            // Para prompts visuais, um modelo de chat pode ser bom.
            var result = await CallChatCompletionsApiAsync(prompt, maxTokens: 300, temperature: 0.8f);
            return result ?? "Falha ao gerar prompt visual.";
        }

        /// <summary>
        /// Gera um resumo de texto simples.
        /// </summary>
        public async Task<string> GerarResumoAsync(string text)
        {
            // Usando a versão com prompt customizado implicitamente, passando o texto como prompt.
            // CancellationToken padrão será usado.
            return await GerarResumoAsync(text, default);
        }

        /// <summary>
        /// Gera um resumo de texto com prompt customizado e cancellation token.
        /// </summary>
        public async Task<string> GerarResumoAsync(string prompt, CancellationToken cancellationToken = default)
        {
            // Prompt genérico de resumo, que pode ser refinado pelo prompt passado
            var finalPrompt = $"Resuma o seguinte texto de forma concisa e objetiva.:\n\n{prompt}";
            // Modelos de Chat são ótimos para tarefas de resumo.
            var result = await CallChatCompletionsApiAsync(finalPrompt, maxTokens: 200, temperature: 0.5f, cancellationToken: cancellationToken);
            return result ?? "Falha ao gerar resumo.";
        }

        // --- Builders de Prompt ---

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
            return $@"Gere um prompt **detalhado e criativo** para criação de imagem/visual em uma IA generativa (como Midjourney, DALL-E).
Descrição do Conteúdo Desejado: {descricaoConteudo}
Estilo Visual de Referência: {estiloVisual} (ex: Fotorrealista, Ilustração vetorial, Arte conceitual, Pastel, Cyberpunk)
Plataforma de Exibição: {plataformaAlvo} (ex: Instagram Feed, Pinterest, Website Banner)

O prompt gerado deve ser otimizado para a plataforma alvo e incluir detalhes sobre iluminação, composição, paleta de cores, e elementos chave para guiar a IA.
Exemplo de estrutura: Uma cena[descrição do conteúdo], em estilo[estilo visual], com iluminação[tipo de iluminação], composição[tipo de composição], paleta de cores[cores], elementos visuais adicionais[etc.] para exibição em[plataforma].
Retorne APENAS o prompt completo.";
        }

        // --- Métodos de Chamada à API ---

        // Chamada Genérica para o Endpoint de Completions (para modelos mais antigos como davinci)
        private async Task<string?> CallCompletionsApiAsync(string prompt, int maxTokens = 150, float temperature = 0.7f, float topP = 1.0f, double frequencyPenalty = 0.0, double presencePenalty = 0.0)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                _logger.LogWarning("Attempted to call OpenAI Completions API with an empty prompt.");
                return null;
            }

            var requestBody = new
            {
                model = ModelTextDavinci003, // Ou outro modelo de Completions disponível
                prompt,
                max_tokens = maxTokens,
                temperature,
                top_p = topP,
                frequency_penalty = frequencyPenalty,
                presence_penalty = presencePenalty
            };

            return await ExecuteApiCallAsync(ApiUrlCompletions, requestBody);
        }

        // Chamada Genérica para o Endpoint de Chat Completions (para modelos como gpt-3.5-turbo)
        private async Task<string?> CallChatCompletionsApiAsync(string prompt, int maxTokens = 150, float temperature = 0.7f, float topP = 1.0f, double frequencyPenalty = 0.0, double presencePenalty = 0.0, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                _logger.LogWarning("Attempted to call OpenAI Chat Completions API with an empty prompt.");
                return null;
            }

            // Para Chat Completions, a estrutura é diferente: um array de mensagens com roles
            var messages = new[]
            {
                new { role = "system", content = "Você é um assistente útil." }, // Role do sistema para definir o comportamento
                new { role = "user", content = prompt } // O prompt do usuário
            };

            var requestBody = new
            {
                model = ModelGpt35Turbo, // Use o modelo de Chat desejado
                messages,
                max_tokens = maxTokens,
                temperature,
                top_p = topP,
                frequency_penalty = frequencyPenalty,
                presence_penalty = presencePenalty
            };

            // Usamos um método que pode aceitar CancellationToken
            return await ExecuteApiCallAsync(ApiUrlChatCompletions, requestBody, cancellationToken);
        }

        // Método genérico para executar a chamada REST e retornar o texto da resposta
        private async Task<string?> ExecuteApiCallAsync<TBody>(string apiUrl, TBody requestBody, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(requestBody);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            string? content = null; // --- DECLARAÇÃO FORA DO TRY ---

            try
            {
                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                content = await response.Content.ReadAsStringAsync(); // Agora content está acessível no try e catch

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API request failed. URL: {Url}, Status: {StatusCode}, Content: {Content}", apiUrl, response.StatusCode, content);
                    return null;
                }

                // Desserializa a resposta. A estrutura varia entre Completions e Chat Completions.
                if (apiUrl.Contains("/v1/chat/completions"))
                {
                    var chatResp = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(content);
                    return chatResp?.Choices?.FirstOrDefault()?.Message?.Content;
                }
                else // Assume que é /v1/completions
                {
                    var completionResp = JsonSerializer.Deserialize<OpenAICompletionResponse>(content);
                    return completionResp?.Choices?.FirstOrDefault()?.Text;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request to OpenAI API failed. URL: {Url}. Attempted content: {Content}", apiUrl, content);
                return null;
            }
            catch (JsonException jsonEx)
            {
                // Agora 'content' é acessível aqui!
                _logger.LogError(jsonEx, "Error deserializing OpenAI API response. URL: {Url}. Content: {Content}", apiUrl, content);
                return null;
            }
            catch (TaskCanceledException tce) when (tce.CancellationToken == cancellationToken)
            {
                _logger.LogInformation("OpenAI API call was cancelled. URL: {Url}. Attempted content: {Content}", apiUrl, content);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while calling OpenAI API. URL: {Url}. Attempted content: {Content}", apiUrl, content);
                return null;
            }
        }

        // --- Modelos de Resposta da API OpenAI ---
        // Estes modelos são simplificados e adequados para JSON deserialization via System.Text.Json.
        // Não dependem de OpenAI.ObjectModels.

        private class OpenAICompletionResponse
        {
            [JsonPropertyName("choices")]
            public List<OpenAIChoice> Choices { get; set; } = new List<OpenAIChoice>();
        }

        private class OpenAIChoice
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private class OpenAIChatCompletionResponse
        {
            [JsonPropertyName("choices")]
            public List<OpenAIChatChoice> Choices { get; set; } = new List<OpenAIChatChoice>();
        }

        private class OpenAIChatChoice
        {
            [JsonPropertyName("message")]
            public OpenAIMessage? Message { get; set; }
        }

        private class OpenAIMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
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

            // Opcional: Tentar limitar ou expandir se o número não for o esperado, mas geralmente
            // confiar no `take(expectedCount)` após obter a lista é suficiente.
            return titles;
        }
    }
}