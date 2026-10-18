using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ofertio.Business.Interfaces.Services
{
    public interface ITikTokAdsApiService
    {
        Task<List<object>> SearchAdsAsync(string query);
        Task<bool> PublishAdAsync(object anuncio);
    }
}
