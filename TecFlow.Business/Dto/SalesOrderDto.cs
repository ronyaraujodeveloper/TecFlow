using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class SalesOrderDto
{
    public int CustomerId { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = new();
}

public class SalesOrderStatusDto
{
    public OrderStatus Status { get; set; }
}
