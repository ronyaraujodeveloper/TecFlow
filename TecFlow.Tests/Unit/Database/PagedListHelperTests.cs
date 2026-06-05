using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.Tests.Unit.Database;

public class PagedListHelperTests
{
    [Fact]
    public void Slice_ShouldReturnRequestedPage_WithMeta()
    {
        var source = Enumerable.Range(1, 25).Select(i => $"item-{i}");
        var filter = new CampaignFilter { Page = 2, PageSize = 10 };

        var (items, meta) = PagedListHelper.Slice(source, filter);

        Assert.Equal(10, items.Count);
        Assert.Equal("item-11", items[0]);
        Assert.Equal(25, meta.TotalCount);
        Assert.Equal(2, meta.CurrentPage);
        Assert.Equal(10, meta.PageSize);
        Assert.Equal(3, meta.TotalPages);
        Assert.True(meta.HasNextPage);
    }

    [Fact]
    public void NormalizePageSize_ShouldCapAtThirty()
    {
        Assert.Equal(30, PagedListHelper.NormalizePageSize(100));
        Assert.Equal(20, PagedListHelper.NormalizePageSize(0));
    }
}
