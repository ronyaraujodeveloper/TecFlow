namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Codifica/decodifica o escopo da loja ativa entre int (IntegracaoLoja.Id) e Guid (GerarLinkAfiliadoDto.StoreId).
/// </summary>
public static class IntegracaoLojaScopeHelper
{
    public static Guid EncodeStoreScope(int lojaId)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, lojaId);
        return new Guid(bytes);
    }

    public static int? TryDecodeStoreScope(Guid storeScopeId)
    {
        var bytes = storeScopeId.ToByteArray();
        var lojaId = BitConverter.ToInt32(bytes, 0);
        return lojaId > 0 ? lojaId : null;
    }
}
