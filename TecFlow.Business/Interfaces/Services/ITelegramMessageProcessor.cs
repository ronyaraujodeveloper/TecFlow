using System.Text.Json;

namespace TecFlow.Business.Interfaces.Services;

public interface ITelegramMessageProcessor
{
    Task ProcessAsync(int? userId, JsonElement payload, CancellationToken cancellationToken = default);
}
