using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TecFlow.Database.Pagin
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
        {
            // Forçar pageSize para 30 se maior do que 30 ou menor ou igual a 0
            if (pageSize > 30 || pageSize <= 0) 
            {
                pageSize = 30;
            }

            // Forçar page para 1 se menor ou igual a 0
            if (page <= 0)
            {
                page = 1;
            }

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}