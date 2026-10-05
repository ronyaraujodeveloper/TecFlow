namespace TecFlow.Database.Filter;

public class GroupCapturedMessageFilter
{
    public int Hours { get; set; } = 24;

    public string? GroupKey { get; set; }

    public string? Channel { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 50;

    public bool Ignored { get; set; }
}
