namespace Ofertio.E2E;

public class RobotBoundaryTests
{
    [Fact]
    public void SourcesStayClearOfLiveSessionsAndTheBulkLoadUsesAnotherPrefix()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        Assert.Empty(RobotBoundary.FindViolations(root));
        Assert.True(RobotBoundary.ForbiddenText.Length >= 8);
        Assert.False(RobotBoundary.BulkPrefix.StartsWith(E2ESettings.RecordPrefix, StringComparison.Ordinal));

        var script = Path.GetFullPath(Path.Combine(root, "..", "scripts", "carga-homolog-rpa.sql"));
        var sql = File.ReadAllText(script);
        Assert.Contains(RobotBoundary.BulkPrefix, sql, StringComparison.Ordinal);
        Assert.Contains("LIKE N'" + E2ESettings.RecordPrefix + "%'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LIKE N'" + RobotBoundary.BulkPrefix + "%'", sql, StringComparison.Ordinal);
        Assert.Contains("@ExecutarLimpeza", sql, StringComparison.Ordinal);
        Assert.Equal(2, Count(sql, "TOP (200)"));
        Assert.DoesNotContain("Password=", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var start = 0;
        while ((start = text.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }
}
