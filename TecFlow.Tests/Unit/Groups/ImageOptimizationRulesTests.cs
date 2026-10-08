using TecFlow.Business.Service.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class ImageOptimizationRulesTests
{
    [Fact]
    public void StrictPolicy_ShouldAllowOnlyUploadsProductsAndFrames()
    {
        Assert.True(StrictImageIngestionPolicy.AllowsWrite("/uploads/products/1/2026/10/9.webp"));
        Assert.True(StrictImageIngestionPolicy.AllowsWrite("/uploads/frames/1-abc.webp"));
        Assert.False(StrictImageIngestionPolicy.AllowsWrite("/uploads/../secrets.txt"));
        Assert.False(StrictImageIngestionPolicy.AllowsWrite("/wwwroot/logo.png"));
    }

    [Fact]
    public void NeedsNeutralCanvas_ShouldFlagExtremeRatios()
    {
        Assert.True(ImageOptimizationRules.NeedsNeutralCanvas(2000, 200));
        Assert.True(ImageOptimizationRules.NeedsNeutralCanvas(200, 2000));
        Assert.False(ImageOptimizationRules.NeedsNeutralCanvas(1080, 1080));
        Assert.False(ImageOptimizationRules.NeedsNeutralCanvas(1080, 1350));
    }

    [Fact]
    public void ChooseQuality_ShouldDropToSeventyFiveWhenOverBudget()
    {
        Assert.Equal(75, ImageOptimizationRules.ChooseQuality(ImageOptimizationRules.MaxTargetBytes + 1, 80));
        Assert.Equal(80, ImageOptimizationRules.ChooseQuality(120_000, 80));
        Assert.Equal("9.webp", ImageOptimizationRules.BuildOutputFileName(9));
    }
}
