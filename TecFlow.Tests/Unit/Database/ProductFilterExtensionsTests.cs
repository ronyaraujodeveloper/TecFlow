using TecFlow.Core.Entities;
using TecFlow.Database.Filter;

namespace TecFlow.Tests.Unit.Database;

public class ProductFilterExtensionsTests
{
    [Fact]
    public void ApplyFilter_ShouldReturnProductResponseDtoShape_WhenFilterByOwnerAndName()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, OwnerId = 10, Name = "Camiseta Azul", Category = "Roupas", Price = 50 },
            new() { Id = 2, OwnerId = 10, Name = "Calça Jeans", Category = "Roupas", Price = 120 },
            new() { Id = 3, OwnerId = 20, Name = "Camiseta Preta", Category = "Roupas", Price = 45 }
        };

        var filter = new ProductFilter { OwnerId = 10, Name = "Camiseta" };

        // Act
        var filtered = products.ApplyFilter(filter).ToList();
        var response = new TecFlow.Business.Dto.ProductResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = filtered
        };

        // Assert
        Assert.True(response.Status);
        Assert.Single(response.DataList!);
        Assert.All(response.DataList, p => Assert.Equal(10, p.OwnerId));
        Assert.All(response.DataList, p => Assert.Contains("Camiseta", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplyFilter_ShouldReturnEmptyDataList_WhenNoProductsMatch()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, OwnerId = 1, Name = "Boné", Price = 30 }
        };

        var filter = new ProductFilter { Name = "Inexistente" };

        // Act
        var response = new TecFlow.Business.Dto.ProductResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = products.ApplyFilter(filter).ToList()
        };

        // Assert
        Assert.True(response.Status);
        Assert.Empty(response.DataList!);
    }
}
