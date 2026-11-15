using Ofertio.Business.Service.Mail;

namespace Ofertio.Business.Interfaces.Services;

public interface ITransactionalEmailSender
{
    Task SendAsync(TransactionalEmail message, CancellationToken cancellationToken = default);
}
