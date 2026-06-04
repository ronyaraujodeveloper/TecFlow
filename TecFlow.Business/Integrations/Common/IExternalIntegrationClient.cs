namespace TecFlow.Business.Integrations.Common;

/// <summary>Contrato base para clientes HTTP de plataformas externas de e-commerce.</summary>
public interface IExternalIntegrationClient
{
    string PlatformName { get; }

    Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> PostAsync(
        string relativePath,
        HttpContent content,
        CancellationToken cancellationToken = default);
}
