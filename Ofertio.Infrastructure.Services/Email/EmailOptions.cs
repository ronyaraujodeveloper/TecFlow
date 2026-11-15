using Ofertio.Business.Service.Mail;

namespace Ofertio.Infrastructure.Services.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = OfertioMailAddresses.NoReply;

    public string FromName { get; set; } = OfertioMailAddresses.FromName;

    public string ReplyToAddress { get; set; } = OfertioMailAddresses.Contact;

    public string PublicBaseUrl { get; set; } = "https://ofertio.com.br";

    /// <summary>No transporte de log, grava o corpo (inclui o link de senha). Fica desligado fora de desenvolvimento.</summary>
    public bool LogBody { get; set; }

    public EmailSmtpOptions Smtp { get; set; } = new();
}

public sealed class EmailSmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;
}
