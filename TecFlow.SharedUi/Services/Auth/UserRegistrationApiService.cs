using System.Net.Http.Json;
using System.Text.Json;
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

    public UserRegistrationApiService(
        IHttpClientFactory httpClientFactory,
        ILoadingService loadingService)
    {
        _httpClientFactory = httpClientFactory;
        _loadingService = loadingService;
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
        catch (TaskCanceledException)
        {
            return new UserResponseDto
            {
                Status = false,
                Descricao = "Tempo limite excedido ao contactar o servidor."
            };
        }
        catch (HttpRequestException)
        {
            return new UserResponseDto
            {
                Status = false,
                Descricao = "Não foi possível contactar o servidor. Verifique se a API está em execução."
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
