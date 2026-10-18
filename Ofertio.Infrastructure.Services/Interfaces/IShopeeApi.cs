using System.Collections.Generic;
using System.Threading.Tasks;
using Ofertio.Core.Entities;
using Ofertio.Infrastructure.Services.Service.ExternalServices;

namespace Ofertio.Core.Interfaces.Services
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