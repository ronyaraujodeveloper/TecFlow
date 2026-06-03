using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace TecFlow.Database.Pagin
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public PagedResult()
        {
            Items = new List<T>();
        }
    }
}