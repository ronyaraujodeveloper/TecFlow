using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services
{
    public interface IShopeeApi
    {
        Task<List<Product>> BuscarProdutosAfiliadosAsync(string keyword);
        Task<string> GerarLinkCurtoAsync(string longUrl);
        Task<bool> ValidarConfiguracaoContaAsync();
        Task<bool> PublicarAnuncioAsync(Product produto);
    }
}
