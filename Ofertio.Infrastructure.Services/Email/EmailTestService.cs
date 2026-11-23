using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;

namespace Ofertio.Infrastructure.Services.Email;

/// <summary>Botão "Enviar e-mail de teste" do Cofre: manda uma mensagem curta para o master logado.</summary>
public sealed class EmailTestService : IEmailTestService
{
    public const string AuditAction = "email.teste";

    private readonly ITransactionalEmailSender _sender;
    private readonly IOptionsMonitor<EmailOptions> _options;
    private readonly IMasterAuditService _audit;
    private readonly ILogger<EmailTestService> _logger;

    public EmailTestService(
        ITransactionalEmailSender sender,
        IOptionsMonitor<EmailOptions> options,
        IMasterAuditService audit,
        ILogger<EmailTestService> logger)
    {
        _sender = sender;
        _options = options;
        _audit = audit;
        _logger = logger;
    }

    public async Task<MasterOperationResultDto> SendTestAsync(MasterActor actor, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!TransactionalEmailSender.IsConfigured(options))
        {
            return Fail("SMTP não configurado: preencha \"SMTP — servidor\" (smtp.gmail.com), o usuário e a senha de app na seção E-mail e salve.");
        }

        if (string.IsNullOrWhiteSpace(actor.Email))
        {
            return Fail("Seu usuário master não tem e-mail para receber o teste.");
        }

        try
        {
            await _sender.SendAsync(TransactionalEmailRules.SmtpTest(actor.Email, DateTime.UtcNow), cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
        {
            var destination = Destination(options);
            _logger.LogWarning(ex, "Falha no e-mail de teste do Cofre. Destino {Destination}.", destination);
            await _audit.LogAsync(actor, AuditAction, "Email", null, "Teste de envio falhou.", cancellationToken);
            return Fail(DescribeFailure(ex, destination));
        }

        await _audit.LogAsync(actor, AuditAction, "Email", null, "Teste de envio para " + actor.Email + ".", cancellationToken);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = "E-mail de teste enviado para " + actor.Email + " (remetente " + TransactionalEmailSender.EffectiveFromAddress(options) + ", respostas para " + options.ReplyToAddress + "). Confira a caixa de entrada e o spam."
        };
    }

    /// <summary>"servidor:porta" para a mensagem e o log (nunca usuário e senha).</summary>
    public static string Destination(EmailOptions options) =>
        (options.Smtp?.Host ?? string.Empty).Trim() + ":" + (options.Smtp is { Port: > 0 } smtp ? smtp.Port : 587);

    /// <summary>Traduz os erros mais comuns do Gmail para uma instrução prática.</summary>
    public static string DescribeFailure(Exception ex, string? destination = null)
    {
        var root = RootCause(ex);
        var detail = root.Message == ex.Message ? ex.Message : ex.Message + " → " + root.Message;
        var all = ex.Message + " " + root.Message;
        if (all.Contains("5.7.8", StringComparison.Ordinal)
            || all.Contains("Username and Password not accepted", StringComparison.OrdinalIgnoreCase)
            || all.Contains("Authentication", StringComparison.OrdinalIgnoreCase)
            || all.Contains("5.7.0", StringComparison.Ordinal))
        {
            return "O Gmail recusou o login. Use a senha de app de 16 letras (myaccount.google.com/apppasswords), não a senha da conta, e confira o usuário. Detalhe: " + detail;
        }

        if (root is SocketException or TimeoutException)
        {
            return "A API não alcançou o servidor SMTP" + (string.IsNullOrWhiteSpace(destination) ? string.Empty : " " + destination) + " (servidor errado, rede ou porta bloqueada). Para o Gmail use smtp.gmail.com, porta 587. Detalhe: " + detail;
        }

        if (ex is FormatException)
        {
            return "Endereço de e-mail inválido no remetente ou no destinatário. Detalhe: " + detail;
        }

        return "Não foi possível enviar. Detalhe: " + detail;
    }

    private static Exception RootCause(Exception ex)
    {
        var current = ex;
        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }

    private static MasterOperationResultDto Fail(string message) => new() { Success = false, Message = message };
}
