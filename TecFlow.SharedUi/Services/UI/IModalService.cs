namespace TecFlow.SharedUi.Services.UI;

public enum ConfirmDialogKind
{
    Danger,
    Warning,
    Info
}

public sealed class ConfirmDialogRequest
{
    public string Title { get; init; } = "Confirmar";

    public string Message { get; init; } = string.Empty;

    public string ConfirmText { get; init; } = "Confirmar";

    public string CancelText { get; init; } = "Cancelar";

    public ConfirmDialogKind Kind { get; init; } = ConfirmDialogKind.Danger;
}

public interface IModalService
{
    ConfirmDialogRequest? Current { get; }

    event Action? OnChange;

    Task<bool> ConfirmAsync(ConfirmDialogRequest request, CancellationToken cancellationToken = default);

    Task<bool> ConfirmDeleteAsync(
        string itemTitle,
        string itemKind = "item",
        string confirmText = "Excluir",
        CancellationToken cancellationToken = default);

    void Complete(bool confirmed);
}
