using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Infrastructure.Services.Service.ExternalServices;

namespace TecFlow.Core.Interfaces.Services
{
    public interface IShopeeApi
    {
        Task<List<Product>> BuscarProdutosAfiliadosAsync(string keyword);
        Task<string> GerarLinkCurtoAsync(string longUrl);
        Task<bool> ValidarConfiguracaoContaAsync();
        // Ajustado para bater com o padrão de tipo da classe
        Task<ShopeeAffiliateLinkResponse> PublicarAnuncioAsync(Product produto);
    }
}