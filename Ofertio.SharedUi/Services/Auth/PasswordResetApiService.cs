using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto.Auth;
using Ofertio.Business.Service.Mail;
using Ofertio.SharedUi.Services.UI;

namespace Ofertio.SharedUi.Services.Auth;

public interface IPasswordResetApiService
{
    Task<AuthProviderResponseDto> RequestAsync(string email, CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> ConfirmAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}

public class PasswordResetApiService : IPasswordResetApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoadingService _loadingService;
    private readonly ILogger<PasswordResetApiService> _logger;

    public PasswordResetApiService(
        IHttpClientFactory httpClientFactory,
        ILoadingService loadingService,
        ILogger<PasswordResetApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _loadingService = loadingService;
        _logger = logger;
    }

    public Task<AuthProviderResponseDto> RequestAsync(string email, CancellationToken cancellationToken = default) =>
        PostAsync("api/auth/password-reset/request", new PasswordResetRequestDto { Email = email }, "Enviando instruções...", cancellationToken);

    public Task<AuthProviderResponseDto> ConfirmAsync(string token, string newPassword, CancellationToken cancellationToken = default) =>
        PostAsync(
            "api/auth/password-reset/confirm",
            new PasswordResetConfirmDto { Token = token, NewPassword = newPassword },
            "Salvando a nova senha...",
            cancellationToken);

    private async Task<AuthProviderResponseDto> PostAsync(
        string path,
        object body,
        string loadingMessage,
        CancellationToken cancellationToken)
    {
        using var _ = _loadingService.BeginScope(loadingMessage);

        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            using var response = await client.PostAsJsonAsync(path, body, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var envelope = TryDeserialize(content);
            if (envelope is not null && !string.IsNullOrWhiteSpace(envelope.Descricao))
            {
                return envelope;
            }

            return Fail("Não foi possível interpretar a resposta do servidor. Escreva para " + OfertioMailAddresses.Contact + ".");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout em {Path}.", path);
            return Fail("Tempo limite excedido ao contactar o servidor. Escreva para " + OfertioMailAddresses.Contact + ".");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha HTTP em {Path}.", path);
            return Fail("Não foi possível contactar o servidor. Escreva para " + OfertioMailAddresses.Contact + ".");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado em {Path}.", path);
            return Fail("Ocorreu um erro inesperado. Escreva para " + OfertioMailAddresses.Contact + ".");
        }
    }

    private static AuthProviderResponseDto? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AuthProviderResponseDto>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static AuthProviderResponseDto Fail(string message) => new()
    {
        Status = false,
        Descricao = message
    };
}
