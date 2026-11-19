using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Legal;

namespace Ofertio.API.Controllers;

/// <summary>
/// Dados do visitante de uma chamada pública. A WebUi fica na frente da API (YARP ou chamada do servidor Blazor) e
/// encaminha o IP (CF-Connecting-IP / X-Forwarded-For) e o navegador (X-Client-User-Agent); sem isso a API só enxergaria o servidor.
/// A API só escuta internamente, então esses cabeçalhos vêm da própria WebUi.
/// </summary>
public static class ClientRequestInfo
{
    public const string UserAgentHeader = "X-Client-User-Agent";

    public static PublicVisitorInfo Read(HttpContext? context)
    {
        if (context is null)
        {
            return new PublicVisitorInfo(null, null);
        }

        var headers = context.Request.Headers;
        var ip = TermsDocumentRules.ResolveClientIp(
            headers["CF-Connecting-IP"].ToString(),
            headers["X-Forwarded-For"].ToString(),
            context.Connection.RemoteIpAddress?.ToString());
        var forwardedAgent = headers[UserAgentHeader].ToString();
        var userAgent = string.IsNullOrWhiteSpace(forwardedAgent) ? headers.UserAgent.ToString() : forwardedAgent;
        return new PublicVisitorInfo(ip, userAgent);
    }
}
