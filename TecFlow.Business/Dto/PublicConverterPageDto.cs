using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class PublicConverterPageDto
{
    public int Id { get; set; }

    public Guid PublicCode { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? PublicUrl { get; set; }

    public List<MarketplaceType> ConnectedPlatforms { get; set; } = [];
}

public class PublicConverterPageFilter
{
    public string? Slug { get; set; }

    public bool? IsActive { get; set; }
}

public class ChangePublicConverterSlugDto
{
    public string Slug { get; set; } = string.Empty;
}

public class PublicConverterConvertDto
{
    public string OriginalUrl { get; set; } = string.Empty;
}

public class PublicConverterPageResponseDto : ResponseDto
{
    public PublicConverterPageDto? Data { get; set; }

    public List<PublicConverterPageDto>? DataList { get; set; }
}
