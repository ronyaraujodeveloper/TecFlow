using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;

namespace Ofertio.Infrastructure.Services.Email;

/// <summary>
/// Envia pelo SMTP quando <see cref="EmailSmtpOptions.Host"/> está preenchido.
/// Sem host, só registra o aviso: é o comportamento de desenvolvimento até existir um provedor.
/// </summary>
public sealed class TransactionalEmailSender : ITransactionalEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<TransactionalEmailSender> _logger;

    public TransactionalEmailSender(IOptions<EmailOptions> options, ILogger<TransactionalEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(TransactionalEmail message, CancellationToken cancellationToken = default)
    {
        var smtp = _options.Smtp ?? new EmailSmtpOptions();
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            if (_options.LogBody)
            {
                _logger.LogInformation(
                    "E-mail transacional sem SMTP. Para {To} Assunto {Subject} Corpo {Body}",
                    message.To,
                    message.Subject,
                    message.TextBody);
            }
            else
            {
                _logger.LogInformation(
                    "E-mail transacional sem SMTP. Para {To} Assunto {Subject}. Corpo omitido.",
                    message.To,
                    message.Subject);
            }

            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false
        };
        mail.To.Add(message.To);
        if (!string.IsNullOrWhiteSpace(_options.ReplyToAddress))
        {
            mail.ReplyToList.Add(new MailAddress(_options.ReplyToAddress));
        }

        using var client = CreateClient(smtp);
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(mail, cancellationToken);
        _logger.LogInformation("E-mail transacional enviado. Para {To} Assunto {Subject}.", message.To, message.Subject);
    }

#pragma warning disable SYSLIB0014
    private static SmtpClient CreateClient(EmailSmtpOptions smtp)
    {
        var client = new SmtpClient(smtp.Host, smtp.Port > 0 ? smtp.Port : 587)
        {
            EnableSsl = smtp.EnableSsl
        };
        if (!string.IsNullOrWhiteSpace(smtp.User))
        {
            client.Credentials = new NetworkCredential(smtp.User, smtp.Password);
        }

        return client;
    }
#pragma warning restore SYSLIB0014
}
