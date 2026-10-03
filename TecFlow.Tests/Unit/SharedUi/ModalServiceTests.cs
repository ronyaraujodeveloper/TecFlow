using TecFlow.SharedUi.Services.UI;

namespace TecFlow.Tests.Unit.SharedUi;

public class ModalServiceTests
{
    [Fact]
    public async Task ConfirmDeleteAsync_ShouldCompleteWhenUserConfirms()
    {
        var service = new ModalService();
        var task = service.ConfirmDeleteAsync("Oferta relâmpago", "agendamento", "Excluir Agendamento");

        Assert.NotNull(service.Current);
        Assert.Equal("Confirmar exclusão", service.Current!.Title);
        Assert.Contains("Oferta relâmpago", service.Current.Message);
        Assert.Equal("Excluir Agendamento", service.Current.ConfirmText);
        Assert.Equal(ConfirmDialogKind.Danger, service.Current.Kind);

        service.Complete(true);
        Assert.True(await task);
        Assert.Null(service.Current);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldReturnFalseWhenCancelled()
    {
        var service = new ModalService();
        var task = service.ConfirmAsync(new ConfirmDialogRequest { Title = "Editar", Message = "Salvar?" });

        service.Complete(false);
        Assert.False(await task);
    }
}
