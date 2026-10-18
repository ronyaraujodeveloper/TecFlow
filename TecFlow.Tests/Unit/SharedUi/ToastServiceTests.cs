using TecFlow.SharedUi.Services.UI;

namespace TecFlow.Tests.Unit.SharedUi;

public class ToastServiceTests
{
    [Fact]
    public void ShowSuccess_ShouldUseTenSecondsAsDefaultDuration()
    {
        using var service = new ToastService();

        var toast = service.ShowSuccess("Status atualizado: Ativo.");

        Assert.NotNull(toast);
        Assert.Equal(ToastType.Success, toast.Type);
        Assert.Equal("Status atualizado: Ativo.", toast.Message);
        Assert.Equal(TimeSpan.FromSeconds(10), toast.Duration);
        Assert.Single(service.Toasts);
    }

    [Fact]
    public void Show_ShouldMapEachHelperToItsType()
    {
        using var service = new ToastService();

        Assert.Equal(ToastType.Success, service.ShowSuccess("a")!.Type);
        Assert.Equal(ToastType.Error, service.ShowError("b")!.Type);
        Assert.Equal(ToastType.Warning, service.ShowWarning("c")!.Type);
        Assert.Equal(ToastType.Info, service.ShowInfo("d")!.Type);
        Assert.Equal(4, service.Toasts.Select(t => t.Id).Distinct().Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Show_ShouldIgnoreEmptyMessages(string? message)
    {
        using var service = new ToastService();

        Assert.Null(service.ShowError(message));
        Assert.Empty(service.Toasts);
    }

    [Fact]
    public void Show_ShouldReplaceDuplicateMessageOfSameType()
    {
        using var service = new ToastService();

        var first = service.ShowSuccess("Oferta ignorada.");
        var second = service.ShowSuccess("Oferta ignorada.");

        var toast = Assert.Single(service.Toasts);
        Assert.Equal(second!.Id, toast.Id);
        Assert.NotEqual(first!.Id, second.Id);
    }

    [Fact]
    public void Show_ShouldKeepOnlyTheMostRecentToasts()
    {
        using var service = new ToastService();

        for (var i = 1; i <= ToastService.MaxVisibleToasts + 2; i++)
        {
            service.ShowInfo($"Mensagem {i}");
        }

        Assert.Equal(ToastService.MaxVisibleToasts, service.Toasts.Count);
        Assert.Equal("Mensagem 3", service.Toasts[0].Message);
    }

    [Fact]
    public void Dismiss_ShouldRemoveToastAndNotifyListeners()
    {
        using var service = new ToastService();
        var toast = service.ShowWarning("Atenção")!;
        var notifications = 0;
        service.OnChange += () => notifications++;

        service.Dismiss(toast.Id);
        service.Dismiss(toast.Id);

        Assert.Empty(service.Toasts);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task Show_ShouldAutoDismissAfterDuration()
    {
        using var service = new ToastService();

        service.ShowInfo("Some rápido", TimeSpan.FromMilliseconds(50));

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (service.Toasts.Count > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Empty(service.Toasts);
    }
}
