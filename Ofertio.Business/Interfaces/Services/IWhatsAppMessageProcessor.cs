using System.Text.Json;

namespace Ofertio.Business.Interfaces.Services;

public interface IWhatsAppMessageProcessor
{
    Task ProcessAsync(JsonElement payload, CancellationToken cancellationToken = default);
}
