using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Ofertio.Business.Service.Common;

namespace Ofertio.Business.Service.Mail;

/// <summary>Textos e o token da redefinição. Sem acesso a banco nem a SMTP.</summary>
public static class TransactionalEmailRules
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    public const string RequestAcceptedMessage =
        "Recebemos sua solicitação. Se o e-mail estiver cadastrado, enviaremos um link para escolher uma nova senha. O link vale por 30 minutos.";

    public const string PasswordUpdatedMessage = "Senha atualizada. Entre com a nova senha.";

    public const string InvalidTokenMessage =
        "Este link é inválido ou expirou. Peça um novo em Recuperar senha.";

    public static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string BuildResetUrl(string? publicBaseUrl, string token)
    {
        var root = NormalizeRoot(publicBaseUrl);
        return root + "/recuperar-senha?token=" + Uri.EscapeDataString(token);
    }

    public static string BuildPlanUrl(string? publicBaseUrl)
    {
        return NormalizeRoot(publicBaseUrl) + "/meu-plano";
    }

    public static TransactionalEmail PasswordReset(string to, string resetUrl)
    {
        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            "Recebemos um pedido para redefinir a senha da sua conta Ofertio.",
            "Abra o link abaixo para escolher uma nova senha. Ele vale por 30 minutos e só pode ser usado uma vez.",
            resetUrl,
            "Se você não pediu isso, ignore este e-mail.",
            "Para falar conosco: " + OfertioMailAddresses.Contact);

        return new TransactionalEmail
        {
            To = to,
            Subject = "Redefina sua senha do Ofertio",
            TextBody = text,
            HtmlBody = "<p>Recebemos um pedido para redefinir a senha da sua conta Ofertio.</p>"
                + "<p>Abra o link abaixo para escolher uma nova senha. Ele vale por 30 minutos e só pode ser usado uma vez.</p>"
                + "<p><a href=\"" + WebUtility.HtmlEncode(resetUrl) + "\">Escolher uma nova senha</a></p>"
                + "<p>Se você não pediu isso, ignore este e-mail.</p>"
                + "<p>Para falar conosco: " + WebUtility.HtmlEncode(OfertioMailAddresses.Contact) + "</p>"
        };
    }

    public static TransactionalEmail PlanPaymentConfirmed(
        string to,
        string? plan,
        string? cycle,
        decimal amount,
        DateTime paidAtUtc,
        string planUrl)
    {
        var planLabel = string.IsNullOrWhiteSpace(plan) ? "Ofertio" : plan.Trim();
        var cycleLabel = string.IsNullOrWhiteSpace(cycle) ? "Mensal" : cycle.Trim();
        var when = FormatDate(paidAtUtc);
        var money = FormatMoney(amount);
        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            "Confirmamos o pagamento da sua assinatura Ofertio.",
            "Plano: " + planLabel,
            "Ciclo: " + cycleLabel,
            "Valor: " + money,
            "Data: " + when,
            "Acompanhe em " + planUrl,
            "Dúvidas: " + OfertioMailAddresses.Contact);

        return new TransactionalEmail
        {
            To = to,
            Subject = "Pagamento confirmado — Ofertio",
            TextBody = text,
            HtmlBody = "<p>Confirmamos o pagamento da sua assinatura Ofertio.</p><ul>"
                + "<li>Plano: " + WebUtility.HtmlEncode(planLabel) + "</li>"
                + "<li>Ciclo: " + WebUtility.HtmlEncode(cycleLabel) + "</li>"
                + "<li>Valor: " + WebUtility.HtmlEncode(money) + "</li>"
                + "<li>Data: " + WebUtility.HtmlEncode(when) + "</li></ul>"
                + "<p><a href=\"" + WebUtility.HtmlEncode(planUrl) + "\">Abrir Meu plano</a></p>"
                + "<p>Dúvidas: " + WebUtility.HtmlEncode(OfertioMailAddresses.Contact) + "</p>"
        };
    }

    public static TransactionalEmail PlanPaymentOverdue(string to, string? plan, decimal amount, string planUrl)
    {
        var planLabel = string.IsNullOrWhiteSpace(plan) ? "Ofertio" : plan.Trim();
        var money = FormatMoney(amount);
        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            "Não identificamos o pagamento da assinatura " + planLabel + " (" + money + ").",
            "Regularize em " + planUrl + " para manter o acesso.",
            "Se precisar de ajuda: " + OfertioMailAddresses.Contact);

        return new TransactionalEmail
        {
            To = to,
            Subject = "Pagamento em aberto — Ofertio",
            TextBody = text,
            HtmlBody = "<p>Não identificamos o pagamento da assinatura "
                + WebUtility.HtmlEncode(planLabel) + " (" + WebUtility.HtmlEncode(money) + ").</p>"
                + "<p><a href=\"" + WebUtility.HtmlEncode(planUrl) + "\">Abrir Meu plano</a></p>"
                + "<p>Se precisar de ajuda: " + WebUtility.HtmlEncode(OfertioMailAddresses.Contact) + "</p>"
        };
    }

    public static TransactionalEmail PlanPaymentCanceled(string to, decimal amount, string planUrl)
    {
        var money = FormatMoney(amount);
        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            "O pagamento de " + money + " da sua assinatura Ofertio foi estornado ou removido.",
            "O estado da conta está em " + planUrl + ".",
            "Para falar conosco: " + OfertioMailAddresses.Contact);

        return new TransactionalEmail
        {
            To = to,
            Subject = "Pagamento estornado — Ofertio",
            TextBody = text,
            HtmlBody = "<p>O pagamento de " + WebUtility.HtmlEncode(money)
                + " da sua assinatura Ofertio foi estornado ou removido.</p>"
                + "<p><a href=\"" + WebUtility.HtmlEncode(planUrl) + "\">Abrir Meu plano</a></p>"
                + "<p>Para falar conosco: " + WebUtility.HtmlEncode(OfertioMailAddresses.Contact) + "</p>"
        };
    }

    /// <summary>Aviso de fim do teste. Com cartão, informa a cobrança automática; sem cartão, pede a escolha de um plano.</summary>
    public static TransactionalEmail TrialEnding(
        string to,
        bool hasCard,
        string? nextPlan,
        decimal nextPlanPrice,
        DateTime trialEndsAtUtc,
        string planUrl)
    {
        var planLabel = string.IsNullOrWhiteSpace(nextPlan) ? "Starter" : nextPlan.Trim();
        var when = FormatDate(trialEndsAtUtc);
        var money = FormatMoney(nextPlanPrice);
        string text;
        string html;
        if (hasCard)
        {
            text = string.Join(
                Environment.NewLine + Environment.NewLine,
                "Seu teste grátis do Ofertio termina em " + when + ".",
                "Nesse dia sua conta passa para o plano " + planLabel + " e o valor de " + money + " será cobrado no cartão cadastrado.",
                "Para trocar de plano ou cancelar antes da cobrança, acesse " + planUrl,
                "Dúvidas: " + OfertioMailAddresses.Contact);
            html = "<p>Seu teste grátis do Ofertio termina em <strong>" + WebUtility.HtmlEncode(when) + "</strong>.</p>"
                + "<p>Nesse dia sua conta passa para o plano " + WebUtility.HtmlEncode(planLabel)
                + " e o valor de " + WebUtility.HtmlEncode(money) + " será cobrado no cartão cadastrado.</p>"
                + "<p><a href=\"" + WebUtility.HtmlEncode(planUrl) + "\">Trocar de plano ou cancelar</a></p>"
                + "<p>Dúvidas: " + WebUtility.HtmlEncode(OfertioMailAddresses.Contact) + "</p>";
        }
        else
        {
            text = string.Join(
                Environment.NewLine + Environment.NewLine,
                "Seu teste grátis do Ofertio termina em " + when + ".",
                "Escolha um plano para continuar usando sem interrupção. Seus dados ficam guardados.",
                planUrl,
                "Dúvidas: " + OfertioMailAddresses.Contact);
            html = "<p>Seu teste grátis do Ofertio termina em <strong>" + WebUtility.HtmlEncode(when) + "</strong>.</p>"
                + "<p>Escolha um plano para continuar usando sem interrupção. Seus dados ficam guardados.</p>"
                + "<p><a href=\"" + WebUtility.HtmlEncode(planUrl) + "\">Escolher meu plano</a></p>"
                + "<p>Dúvidas: " + WebUtility.HtmlEncode(OfertioMailAddresses.Contact) + "</p>";
        }

        return new TransactionalEmail
        {
            To = to,
            Subject = "Seu teste grátis termina em breve — Ofertio",
            TextBody = text,
            HtmlBody = html
        };
    }

    /// <summary>Aviso de cancelamento: informa até quando links e páginas públicas continuam ativos.</summary>
    public static TransactionalEmail AccountCanceled(string to, int graceDays, DateTime linksActiveUntilUtc, string planUrl)
    {
        var until = FormatDate(linksActiveUntilUtc);
        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            "Sua conta Ofertio foi cancelada.",
            graceDays > 0
                ? "Seus links curtos e sua página pública continuam funcionando, com os seus próprios dados de afiliado, até " + until + ". Depois dessa data ficam indisponíveis."
                : "Seus links curtos e sua página pública deixaram de funcionar.",
            "Seus dados ficam guardados. Para voltar a usar, reative sua assinatura em " + planUrl,
            "Dúvidas: " + OfertioMailAddresses.Contact);

        return new TransactionalEmail
        {
            To = to,
            Subject = "Sua conta Ofertio foi cancelada",
            TextBody = text,
            HtmlBody = "<p>Sua conta Ofertio foi cancelada.</p>"
                + (graceDays > 0
                    ? "<p>Seus links curtos e sua página pública continuam funcionando, com os seus próprios dados de afiliado, até <strong>"
                        + WebUtility.HtmlEncode(until) + "</strong>. Depois dessa data ficam indisponíveis.</p>"
                    : "<p>Seus links curtos e sua página pública deixaram de funcionar.</p>")
                + "<p>Seus dados ficam guardados. <a href=\"" + WebUtility.HtmlEncode(planUrl) + "\">Reativar minha assinatura</a></p>"
                + "<p>Dúvidas: " + WebUtility.HtmlEncode(OfertioMailAddresses.Contact) + "</p>"
        };
    }

    private static string NormalizeRoot(string? publicBaseUrl)
    {
        var root = string.IsNullOrWhiteSpace(publicBaseUrl)
            ? "https://ofertio.com.br"
            : publicBaseUrl.Trim().TrimEnd('/');
        return root;
    }

    private static string FormatMoney(decimal amount) =>
        amount.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    private static string FormatDate(DateTime utc) =>
        BrazilTimeZoneRules.ToLocal(utc, null).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
}
