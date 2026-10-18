using TecFlow.Business.Dto;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Groups;

/// <summary>
/// Mesmo critério do filtro do feed (<c>ApplyRelevanceFilter</c>) aplicado a uma oferta isolada, para tirar
/// da tela na hora a oferta que o Verificar marcou como indisponível.
/// </summary>
public static class OfferFeedVisibilityRules
{
    public const string RemovedFromFeedToast = "Produto esgotado. Oferta removida do feed.";

    public static bool IsVisibleInFeed(OfferType type, bool isAvailable, string? status) =>
        type == OfferType.Coupon
            ? isAvailable
            : GroupOfferStatuses.IsFeedVisible(isAvailable, status);

    /// <summary>Remove a oferta da lista em memória; devolve false quando ela já não estava na tela.</summary>
    public static bool RemoveFromFeed(List<GroupCapturedOfferDto> offers, int offerId) =>
        offers.RemoveAll(item => item.Id == offerId) > 0;

    /// <summary>Troca a oferta da tela pela versão revalidada, mantendo a posição no feed.</summary>
    public static bool ReplaceInFeed(List<GroupCapturedOfferDto> offers, GroupCapturedOfferDto validated)
    {
        var index = offers.FindIndex(item => item.Id == validated.Id);
        if (index < 0)
        {
            return false;
        }

        offers[index] = validated;
        return true;
    }
}
