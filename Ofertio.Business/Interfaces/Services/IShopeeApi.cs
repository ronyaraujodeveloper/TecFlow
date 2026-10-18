using System.Collections.Generic;
using System.Threading.Tasks;
using Ofertio.Core.Entities;

namespace Ofertio.Business.Interfaces.Services
{
    public interface IShopeeApi
    {
        Task<List<Product>> BuscarProdutosAfiliadosAsync(string keyword);
        Task<string> GerarLinkCurtoAsync(string longUrl);
        Task<bool> ValidarConfiguracaoContaAsync();
        Task<bool> PublicarAnuncioAsync(Product produto);
    }
}
