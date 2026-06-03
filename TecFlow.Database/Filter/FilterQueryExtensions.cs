using TecFlow.Core.Entities;
using TecFlow.Database.Entity;

namespace TecFlow.Database.Filter;

/// <summary>Aplica filtros nullable em memória até os repositórios exporem query por Filter.</summary>
public static class FilterQueryExtensions
{
    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    public static IEnumerable<Affiliate> ApplyFilter(this IEnumerable<Affiliate> source, AffiliateFilter filter)
    {
        if (filter.Id.HasValue)
            source = source.Where(x => x.Id == filter.Id.Value);
        if (filter.CreatedAt.HasValue)
            source = source.Where(x => x.CreatedAt.Date == filter.CreatedAt.Value.Date);
        if (filter.UpdatedAt.HasValue)
            source = source.Where(x => x.UpdatedAt == filter.UpdatedAt);
        if (HasText(filter.Name))
            source = source.Where(x => x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Email))
            source = source.Where(x => x.Email.Contains(filter.Email!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.AffiliateCode))
            source = source.Where(x => x.AffiliateCode.Contains(filter.AffiliateCode!, StringComparison.OrdinalIgnoreCase));
        if (filter.Commission.HasValue)
            source = source.Where(x => x.Commission == filter.Commission.Value);
        if (filter.CampaignId.HasValue)
            source = source.Where(x => x.CampaignId == filter.CampaignId.Value);
        if (filter.ContentId.HasValue)
            source = source.Where(x => x.ContentId == filter.ContentId.Value);
        if (filter.OwnerId.HasValue)
            source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        return source;
    }

    public static IEnumerable<Campaign> ApplyFilter(this IEnumerable<Campaign> source, CampaignFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (HasText(filter.Name)) source = source.Where(x => x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Description)) source = source.Where(x => x.Description.Contains(filter.Description!, StringComparison.OrdinalIgnoreCase));
        if (filter.StartDate.HasValue) source = source.Where(x => x.StartDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) source = source.Where(x => x.EndDate <= filter.EndDate.Value);
        if (filter.Budget.HasValue) source = source.Where(x => x.Budget == filter.Budget.Value);
        if (filter.OwnerId.HasValue) source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        return source;
    }

    public static IEnumerable<Content> ApplyFilter(this IEnumerable<Content> source, ContentFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (HasText(filter.Name)) source = source.Where(x => x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Description)) source = source.Where(x => x.Description.Contains(filter.Description!, StringComparison.OrdinalIgnoreCase));
        if (filter.StartDate.HasValue) source = source.Where(x => x.StartDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) source = source.Where(x => x.EndDate <= filter.EndDate.Value);
        if (filter.Budget.HasValue) source = source.Where(x => x.Budget == filter.Budget.Value);
        if (filter.OwnerId.HasValue) source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        return source;
    }

    public static IEnumerable<Conversion> ApplyFilter(this IEnumerable<Conversion> source, ConversionFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (filter.AffiliateId.HasValue) source = source.Where(x => x.AffiliateId == filter.AffiliateId.Value);
        if (filter.SaleAmount.HasValue) source = source.Where(x => x.SaleAmount == filter.SaleAmount.Value);
        if (filter.Clicks.HasValue) source = source.Where(x => x.Clicks == filter.Clicks.Value);
        if (filter.Sales.HasValue) source = source.Where(x => x.Sales == filter.Sales.Value);
        if (filter.OwnerId.HasValue) source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        return source;
    }

    public static IEnumerable<Item> ApplyFilter(this IEnumerable<Item> source, ItemFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (HasText(filter.Name)) source = source.Where(x => x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Description)) source = source.Where(x => x.Description.Contains(filter.Description!, StringComparison.OrdinalIgnoreCase));
        if (filter.PopularityScore.HasValue) source = source.Where(x => x.PopularityScore == filter.PopularityScore.Value);
        if (filter.OwnerId.HasValue) source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        return source;
    }

    public static IEnumerable<Metric> ApplyFilter(this IEnumerable<Metric> source, MetricFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (filter.CampaignId.HasValue) source = source.Where(x => x.CampaignId == filter.CampaignId.Value);
        if (filter.Views.HasValue) source = source.Where(x => x.Views == filter.Views.Value);
        if (filter.Clicks.HasValue) source = source.Where(x => x.Clicks == filter.Clicks.Value);
        if (filter.Sales.HasValue) source = source.Where(x => x.Sales == filter.Sales.Value);
        if (filter.Investment.HasValue) source = source.Where(x => x.Investment == filter.Investment.Value);
        if (filter.Revenue.HasValue) source = source.Where(x => x.Revenue == filter.Revenue.Value);
        if (filter.OwnerId.HasValue) source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        if (filter.ParentMetricId.HasValue) source = source.Where(x => x.ParentMetricId == filter.ParentMetricId.Value);
        return source;
    }

    public static IEnumerable<Product> ApplyFilter(this IEnumerable<Product> source, ProductFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (HasText(filter.Name)) source = source.Where(x => x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Category)) source = source.Where(x => x.Category.Contains(filter.Category!, StringComparison.OrdinalIgnoreCase));
        if (filter.Price.HasValue) source = source.Where(x => x.Price == filter.Price.Value);
        if (filter.OwnerId.HasValue) source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        return source;
    }

    public static IEnumerable<RankedItem> ApplyFilter(this IEnumerable<RankedItem> source, RankedItemFilter filter)
    {
        if (filter.ItemId.HasValue) source = source.Where(x => x.ItemId == filter.ItemId.Value);
        if (filter.Score.HasValue) source = source.Where(x => x.Score == filter.Score.Value);
        if (HasText(filter.Name)) source = source.Where(x => x.Name != null && x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (filter.OwnerId.HasValue) source = source.Where(x => x.OwnerId == filter.OwnerId.Value);
        return source;
    }

    public static IEnumerable<UserAccount> ApplyFilter(this IEnumerable<UserAccount> source, UserAccountFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (HasText(filter.Name)) source = source.Where(x => x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Email)) source = source.Where(x => x.Email.Contains(filter.Email!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Plan)) source = source.Where(x => x.Plan.Contains(filter.Plan!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.WhatsAppPhone)) source = source.Where(x => x.WhatsAppPhone != null && x.WhatsAppPhone.Contains(filter.WhatsAppPhone!));
        return source;
    }

    public static IEnumerable<UserEntity> ApplyFilter(this IEnumerable<UserEntity> source, UserFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (HasText(filter.Name)) source = source.Where(x => x.Name.Contains(filter.Name!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.Email)) source = source.Where(x => x.Email.Contains(filter.Email!, StringComparison.OrdinalIgnoreCase));
        if (HasText(filter.PhoneNumber)) source = source.Where(x => x.PhoneNumber != null && x.PhoneNumber.Contains(filter.PhoneNumber!));
        if (filter.IsActive.HasValue) source = source.Where(x => x.IsActive == filter.IsActive.Value);
        return source;
    }

    public static IEnumerable<YourItemEntityType> ApplyFilter(this IEnumerable<YourItemEntityType> source, YourItemEntityTypeFilter filter)
    {
        if (filter.Id.HasValue) source = source.Where(x => x.Id == filter.Id.Value);
        if (HasText(filter.SomeProperty)) source = source.Where(x => x.SomeProperty.Contains(filter.SomeProperty!, StringComparison.OrdinalIgnoreCase));
        return source;
    }
}
