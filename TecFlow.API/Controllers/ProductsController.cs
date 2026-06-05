using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Service.Application;
using TecFlow.Core.Entities;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/Produtos")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IMarketplaceStockService _marketplaceStockService;
    private readonly ILogger<ProductsController> _logger;
    private readonly AIApplicationService _aiService;

    public ProductsController(
        IProductRepository productRepository,
        IMarketplaceStockService marketplaceStockService,
        ILogger<ProductsController> logger,
        AIApplicationService aiService)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _marketplaceStockService = marketplaceStockService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
    }

    [HttpGet]
    public async Task<ActionResult<ProductResponseDto>> GetByFilterAsync([FromQuery] ProductFilter filter)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        filter.OwnerId ??= userId;

        var filtered = (await _productRepository.GetByOwnerIdAsync(userId)).ApplyFilter(filter);
        var (items, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new ProductResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound(new ProductResponseDto { Status = false, Descricao = "Produto não encontrado." });
        }

        return Ok(new ProductResponseDto { Status = true, Descricao = "OK", Data = product });
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> CreateAsync([FromBody] ProductDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var product = new Product
        {
            Name = dto.Name,
            Summary = dto.Summary,
            Description = dto.Description,
            Features = dto.Features,
            Benefits = dto.Benefits,
            Category = dto.Category,
            TargetAudience = dto.TargetAudience,
            Price = dto.Price,
            MainImageUrl = dto.MainImageUrl,
            ImageUrls = dto.ImageUrls,
            SalesVolume = dto.SalesVolume,
            Rating = dto.Rating,
            Material = dto.Material,
            Stock = dto.Stock,
            Color = dto.Color,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow,
            ModifiedOn = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = product.Id },
            new ProductResponseDto { Status = true, Descricao = "Criado com sucesso.", Data = product });
    }

    [HttpPost("{id:int}/otimizar")]
    public async Task<IActionResult> OptimizeAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound(new ProductResponseDto { Status = false, Descricao = "Produto não encontrado." });
        }

        var description = await _aiService.GerarDescricaoProdutoAsync(product, "Otimizado para conversão no TikTok Shop");
        product.Description = description;
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product);

        return Ok(new ProductResponseDto
        {
            Status = true,
            Descricao = "Produto atualizado com IA",
            Data = product
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> UpdateAsync(int id, [FromBody] ProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound(new ProductResponseDto { Status = false, Descricao = $"Product com ID {id} não foi localizado." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new ProductResponseDto { Status = false, Descricao = "Usuário não autenticado ou token inválido." });
        }

        if (product.OwnerId != userId)
        {
            return Forbid();
        }

        product.Name = dto.Name;
        product.Summary = dto.Summary;
        product.Description = dto.Description;
        product.Features = dto.Features;
        product.Benefits = dto.Benefits;
        product.Category = dto.Category;
        product.TargetAudience = dto.TargetAudience;
        product.Price = dto.Price;
        product.MainImageUrl = dto.MainImageUrl;
        product.ImageUrls = dto.ImageUrls;
        product.SalesVolume = dto.SalesVolume;
        product.Rating = dto.Rating;
        product.Material = dto.Material;
        var previousStock = product.Stock;
        product.Stock = dto.Stock;
        product.Color = dto.Color;
        product.ModifiedOn = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);

        if (previousStock != product.Stock &&
            product.MarketplaceSource.HasValue &&
            !string.IsNullOrWhiteSpace(product.MarketplaceShopId) &&
            !string.IsNullOrWhiteSpace(product.SkuCode))
        {
            await _marketplaceStockService.UpdatePlatformStockAsync(
                product.MarketplaceShopId,
                product.SkuCode,
                product.Stock,
                product.MarketplaceSource.Value);
        }

        return Ok(new ProductResponseDto { Status = true, Descricao = "Atualizado com sucesso.", Data = product });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound(new ProductResponseDto { Status = false, Descricao = "Produto não encontrado." });
        }

        await _productRepository.DeleteAsync(id);
        return Ok(new ProductResponseDto { Status = true, Descricao = "Removido com sucesso." });
    }
}
