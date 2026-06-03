using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TecFlow.Infrastructure
{
    public class ShopeeProductResult
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
