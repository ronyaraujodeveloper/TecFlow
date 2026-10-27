using Ofertio.Business.Service.Common;
using Ofertio.Core.Entities;

namespace Ofertio.Business.Service.Groups;

/// <summary>
/// Data/hora das mensagens capturadas (Telegram/WhatsApp): gravada sempre em UTC (<c>datetime2</c>) com o horário
/// de envio da mensagem e exibida no horário de Brasília.
/// </summary>
public static class CapturedMessageTimeRules
{
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxMessageAge = TimeSpan.FromDays(365);

    /// <summary>
    /// Ordem padrão do feed: horário da mensagem capturada (mais recente primeiro). <c>CreatedAt</c> não serve
    /// porque a sincronização de histórico grava mensagens antigas depois das novas.
    /// </summary>
    public static IOrderedQueryable<GroupCapturedMessage> OrderByCapturedAtDescending(
        this IQueryable<GroupCapturedMessage> query) =>
        query.OrderByDescending(item => item.ReceivedAt).ThenByDescending(item => item.Id);

    /// <summary>Valor em UTC: <c>Local</c> é convertido e <c>Unspecified</c> (lido do banco) já é UTC.</summary>
    public static DateTime NormalizeUtc(DateTime value) => BrazilTimeZoneRules.NormalizeUtc(value);

    public static DateTime? FromUnixSeconds(long seconds)
    {
        if (seconds <= 0)
        {
            return null;
        }

        if (seconds > 99_999_999_999)
        {
            seconds /= 1000;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Horário de envio da mensagem quando plausível; senão o instante da captura.</summary>
    public static DateTime ResolveReceivedAtUtc(DateTime? sentAt, DateTime utcNow)
    {
        var now = NormalizeUtc(utcNow);
        if (sentAt is not { } value || value == default)
        {
            return now;
        }

        var sent = NormalizeUtc(value);
        return sent > now + MaxClockSkew || sent < now - MaxMessageAge ? now : sent;
    }

    public static DateTime ToBrasilia(DateTime value) => BrazilTimeZoneRules.ToLocal(value, null);
}
