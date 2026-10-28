using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;
using Ofertio.SharedUi.Services.UI;

namespace Ofertio.SharedUi.Services.Account;

/// <summary>Cliente das telas Meus dados, Meu plano e pagamento (rotas api/conta).</summary>
public interface IMyAccountApiService
{
    Task<ApiResult<UserProfileResponseDto>> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<UserProfileResponseDto>> SaveProfileAsync(UserProfileDto profile, CancellationToken cancellationToken = default);

    Task<ApiResult<MyPlanResponseDto>> GetPlanAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<PlanCheckoutResponseDto>> CheckoutAsync(PlanCheckoutDto request, CancellationToken cancellationToken = default);

    Task<ApiResult<PlanCheckoutResponseDto>> GetCheckoutAsync(int checkoutId, CancellationToken cancellationToken = default);
}

public sealed class MyAccountApiService : IMyAccountApiService
{
    public const string ProfilePath = "api/conta/dados";
    public const string PlanPath = "api/conta/plano";
    public const string CheckoutPath = "api/conta/plano/pagamento";

    private readonly IHttpService _httpService;
    private readonly ILoadingService _loadingService;

    public MyAccountApiService(IHttpService httpService, ILoadingService loadingService)
    {
        _httpService = httpService;
        _loadingService = loadingService;
    }

    public async Task<ApiResult<UserProfileResponseDto>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando seus dados...");
        return await _httpService.GetAsync<UserProfileResponseDto>(ProfilePath, cancellationToken: cancellationToken);
    }

    public async Task<ApiResult<UserProfileResponseDto>> SaveProfileAsync(UserProfileDto profile, CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Salvando seus dados...");
        return await _httpService.PutAsync<UserProfileDto, UserProfileResponseDto>(ProfilePath, profile, cancellationToken);
    }

    public async Task<ApiResult<MyPlanResponseDto>> GetPlanAsync(CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando planos...");
        return await _httpService.GetAsync<MyPlanResponseDto>(PlanPath, cancellationToken: cancellationToken);
    }

    public async Task<ApiResult<PlanCheckoutResponseDto>> CheckoutAsync(PlanCheckoutDto request, CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Processando pagamento...");
        return await _httpService.PostAsync<PlanCheckoutDto, PlanCheckoutResponseDto>(CheckoutPath, request, cancellationToken);
    }

    public async Task<ApiResult<PlanCheckoutResponseDto>> GetCheckoutAsync(int checkoutId, CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando pedido...");
        return await _httpService.GetAsync<PlanCheckoutResponseDto>($"{CheckoutPath}/{checkoutId}", cancellationToken: cancellationToken);
    }
}
