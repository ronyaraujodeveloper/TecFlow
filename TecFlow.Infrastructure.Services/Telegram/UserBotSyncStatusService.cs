using System.Collections.Concurrent;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Telegram;

public sealed class UserBotSyncStatusService : IUserBotSyncStatusService
{
    private readonly ConcurrentDictionary<int, UserBotSyncStatusDto> _byUser = new();
    private UserBotSyncStatusDto _global = Idle();
    private readonly object _globalGate = new();

    public void MarkRunning(int userId, string? message = null) =>
        Set(userId, UserBotSyncPhases.Running, string.IsNullOrWhiteSpace(message)
            ? "Conexão estabelecida! Varrendo histórico de grupos..."
            : message.Trim());

    public void MarkCompleted(int userId, string? message = null) =>
        Set(userId, UserBotSyncPhases.Completed, string.IsNullOrWhiteSpace(message)
            ? "Varredura do histórico concluída."
            : message.Trim());

    public void MarkFailed(int userId, string message)
    {
        var text = string.IsNullOrWhiteSpace(message)
            ? "Erro crítico na execução em segundo plano do UserBot."
            : $"Falha na captura: {message.Trim()}";
        Set(userId, UserBotSyncPhases.Failed, text);
    }

    public UserBotSyncStatusDto Get(int userId)
    {
        if (userId > 0 && _byUser.TryGetValue(userId, out var current))
        {
            return Clone(current);
        }

        lock (_globalGate)
        {
            return Clone(_global);
        }
    }

    private void Set(int userId, string phase, string message)
    {
        var snapshot = new UserBotSyncStatusDto
        {
            Status = phase != UserBotSyncPhases.Failed,
            Phase = phase,
            Message = message,
            UpdatedAt = DateTime.UtcNow
        };

        if (userId > 0)
        {
            _byUser[userId] = snapshot;
            return;
        }

        lock (_globalGate)
        {
            _global = snapshot;
        }
    }

    private static UserBotSyncStatusDto Idle() =>
        new()
        {
            Status = true,
            Phase = UserBotSyncPhases.Idle,
            Message = string.Empty
        };

    private static UserBotSyncStatusDto Clone(UserBotSyncStatusDto item) =>
        new()
        {
            Status = item.Status,
            Phase = item.Phase,
            Message = item.Message,
            UpdatedAt = item.UpdatedAt
        };
}
