using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services
{
    public interface ITikTokShopApi
    {
        Task<List<Product>> BuscarProdutosAsync(string query, int pageNumber = 1);
        Task<bool> PublicarAnuncioAsync(Product produto);
        Task<object> ObterPerfilConectadoAsync();
        Task<string?> GerarLinkAfiliadoAsync(Product produto);
        Task<string> TrocarCodigoPorTokenAsync(string code);
    }
}
