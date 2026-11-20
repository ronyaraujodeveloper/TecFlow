using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
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
    private readonly IOptionsMonitor<EmailOptions> _monitor;
    private readonly ILogger<TransactionalEmailSender> _logger;

    /// <summary>
    /// Lê a configuração atual a cada envio: o Cofre do Painel Master recarrega as chaves ao salvar,
    /// então servidor, usuário e senha novos valem sem reiniciar a API.
    /// </summary>
    public TransactionalEmailSender(IOptionsMonitor<EmailOptions> options, ILogger<TransactionalEmailSender> logger)
    {
        _monitor = options;
        _logger = logger;
    }

    public async Task SendAsync(TransactionalEmail message, CancellationToken cancellationToken = default)
    {
        var options = _monitor.CurrentValue;
        var smtp = options.Smtp ?? new EmailSmtpOptions();
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            if (options.LogBody)
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

        using var mail = BuildMessage(options, message);
        using var client = CreateClient(smtp);
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(mail, cancellationToken);
        _logger.LogInformation("E-mail transacional enviado. Para {To} Assunto {Subject}.", message.To, message.Subject);
    }

    /// <summary>Monta a mensagem: texto como corpo principal e, quando houver, a versão HTML como alternativa.</summary>
    public static MailMessage BuildMessage(EmailOptions options, TransactionalEmail message)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName, Encoding.UTF8),
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = message.TextBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        mail.To.Add(message.To);
        if (!string.IsNullOrWhiteSpace(options.ReplyToAddress))
        {
            mail.ReplyToList.Add(new MailAddress(options.ReplyToAddress));
        }

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mail.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(message.HtmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));
        }

        return mail;
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
            client.Credentials = new NetworkCredential(smtp.User.Trim(), NormalizePassword(smtp.Password));
        }

        return client;
    }
#pragma warning restore SYSLIB0014

    /// <summary>O Gmail exibe a senha de app em blocos ("abcd efgh ijkl mnop"); o SMTP espera sem espaços.</summary>
    public static string NormalizePassword(string? password) =>
        string.IsNullOrEmpty(password) ? string.Empty : password.Replace(" ", string.Empty).Trim();

    /// <summary>True quando há servidor configurado; sem ele o envio só vai para o log.</summary>
    public static bool IsConfigured(EmailOptions options) =>
        !string.IsNullOrWhiteSpace(options.Smtp?.Host);
}
