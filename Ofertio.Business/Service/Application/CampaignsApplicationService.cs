using Microsoft.Extensions.Logging;
using Ofertio.Core.Entities;
using Ofertio.Business.Interfaces.Repositories;

namespace Ofertio.Business.Service.Application;

public class CampaignsApplicationService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ILogger<CampaignsApplicationService> _logger;

    public CampaignsApplicationService(
        ICampaignRepository campaignRepository,
        ILogger<CampaignsApplicationService> logger)
    {
        _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IEnumerable<Campaign>> GetAllAsync() => _campaignRepository.GetAllAsync();

    public Task<Campaign?> GetByIdAsync(int id) => _campaignRepository.GetByIdAsync(id);

    public Task AddAsync(Campaign entity) => _campaignRepository.AddAsync(entity);

    public Task UpdateAsync(Campaign entity) => _campaignRepository.UpdateAsync(entity);

    public Task DeleteAsync(int id) => _campaignRepository.DeleteAsync(id);
}
