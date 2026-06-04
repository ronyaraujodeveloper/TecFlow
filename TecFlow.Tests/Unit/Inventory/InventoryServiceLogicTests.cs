using TecFlow.Business.Domain.Inventory;

namespace TecFlow.Tests.Unit.Inventory;

public class InventoryServiceLogicTests
{
    [Fact]
    public void InsufficientStockException_ContainsQuantities()
    {
        var ex = new InsufficientStockException(10, 5, 2);

        Assert.Equal(10, ex.ProductId);
        Assert.Equal(5, ex.RequestedQuantity);
        Assert.Equal(2, ex.AvailableQuantity);
        Assert.Contains("insuficiente", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
