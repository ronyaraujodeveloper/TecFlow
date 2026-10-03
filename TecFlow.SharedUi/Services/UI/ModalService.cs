namespace TecFlow.SharedUi.Services.UI;

public sealed class ModalService : IModalService
{
    private TaskCompletionSource<bool>? _pending;

    public ConfirmDialogRequest? Current { get; private set; }

    public event Action? OnChange;

    public Task<bool> ConfirmAsync(ConfirmDialogRequest request, CancellationToken cancellationToken = default)
    {
        _pending?.TrySetResult(false);
        Current = request ?? new ConfirmDialogRequest();
        _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => Complete(false));
        }

        OnChange?.Invoke();
        return _pending.Task;
    }

    public Task<bool> ConfirmDeleteAsync(
        string itemTitle,
        string itemKind = "item",
        string confirmText = "Excluir",
        CancellationToken cancellationToken = default)
    {
        var kind = string.IsNullOrWhiteSpace(itemKind) ? "item" : itemKind.Trim();
        var title = string.IsNullOrWhiteSpace(itemTitle) ? kind : itemTitle.Trim();
        return ConfirmAsync(
            new ConfirmDialogRequest
            {
                Title = "Confirmar exclusão",
                Message = $"Tem certeza que deseja excluir o {kind} '{title}'? Esta ação não poderá ser desfeita.",
                ConfirmText = string.IsNullOrWhiteSpace(confirmText) ? "Excluir" : confirmText.Trim(),
                CancelText = "Cancelar",
                Kind = ConfirmDialogKind.Danger
            },
            cancellationToken);
    }

    public void Complete(bool confirmed)
    {
        Current = null;
        var pending = _pending;
        _pending = null;
        pending?.TrySetResult(confirmed);
        OnChange?.Invoke();
    }
}
