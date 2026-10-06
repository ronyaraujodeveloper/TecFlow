using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IStructuredOfferParserService
{
    OfferDataExtraction Parse(string? rawMessage);
}
