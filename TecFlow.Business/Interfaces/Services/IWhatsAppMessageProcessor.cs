using System.Text.Json;

namespace TecFlow.Business.Interfaces.Services;

public interface IWhatsAppMessageProcessor
{
    Task ProcessAsync(JsonElement payload, CancellationToken cancellationToken = default);
}
