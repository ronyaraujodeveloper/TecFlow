namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Exceção amigável para falhas de geração de link de afiliado (mapeada para o ResponseDto).
/// </summary>
public sealed class AffiliateLinkGenerationException : Exception
{
    public AffiliateLinkGenerationException(string message)
        : base(message)
    {
    }

    public AffiliateLinkGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
