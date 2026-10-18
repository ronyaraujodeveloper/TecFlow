namespace Ofertio.Business.Interfaces.Services;

public interface IUploadStorage
{
    /// <summary>Pasta física servida em <c>/uploads</c> (ex.: <c>C:\inetpub\ofertio\uploads</c>).</summary>
    string UploadsRoot { get; }

    /// <summary>Raiz onde as URLs <c>/uploads/...</c> resolvem; argumento <c>webRootPath</c> de <c>ProductImageStorageRules</c>.</summary>
    string MediaRoot { get; }
}
