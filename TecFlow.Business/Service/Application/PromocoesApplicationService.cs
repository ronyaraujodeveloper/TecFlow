using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Repositories;

namespace TecFlow.Business.Service.Application
{
    // Serviço de aplicação responsável por promoções (Promocoes)
    public class PromocoesApplicationService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly ILogger<PromocoesApplicationService> _logger;

        public PromocoesApplicationService(
            ICampaignRepository campanhaRepository,
            ILogger<PromocoesApplicationService> logger)
        {
            _campaignRepository = campanhaRepository;
            _logger = logger;
        }

        // Placeholder: retorna uma lista vazia. Substitua pela lógica real quando necessário.
        public virtual Task<IEnumerable<object>> ListPromocoesAsync()
        {
            _logger.LogInformation("Listando promoções via PromocoesApplicationService.");
            return Task.FromResult<IEnumerable<object>>(new List<object>());
        }
    }
}