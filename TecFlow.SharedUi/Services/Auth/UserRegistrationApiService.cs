using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Auth;

public class UserRegistrationApiService : IUserRegistrationApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoadingService _loadingService;
    private readonly ILogger<UserRegistrationApiService> _logger;

    public UserRegistrationApiService(
        IHttpClientFactory httpClientFactory,
        ILoadingService loadingService,
        ILogger<UserRegistrationApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _loadingService = loadingService;
        _logger = logger;
    }

    public async Task<UserResponseDto> RegisterAsync(UserDto request, CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Criando sua conta...");

        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            using var response = await client.PostAsJsonAsync("api/auth/register", request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var envelope = TryDeserialize<UserResponseDto>(content);
            if (envelope is not null)
            {
                return envelope;
            }

            return new UserResponseDto
            {
                Status = false,
                Descricao = "Não foi possível interpretar a resposta do servidor."
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout no registro de usuario via api/auth/register.");
            return new UserResponseDto
            {
                Status = false,
                Descricao = "Tempo limite excedido ao contactar o servidor."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha HTTP no registro de usuario via api/auth/register.");
            return new UserResponseDto
            {
                Status = false,
                Descricao = "Não foi possível contactar o servidor. Verifique se a API está em execução."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no registro de usuario via api/auth/register.");
            return new UserResponseDto
            {
                Status = false,
                Descricao = "Ocorreu um erro inesperado ao criar a conta."
            };
        }
    }

    private static T? TryDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
