using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

public interface IStructuredOfferParserService
{
    OfferDataExtraction Parse(string? rawMessage);
}
