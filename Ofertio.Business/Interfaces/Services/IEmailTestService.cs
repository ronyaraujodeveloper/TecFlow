using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

/// <summary>Envia o e-mail de teste do Cofre para o master logado e traduz a falha do SMTP.</summary>
public interface IEmailTestService
{
    Task<MasterOperationResultDto> SendTestAsync(MasterActor actor, CancellationToken cancellationToken = default);
}
