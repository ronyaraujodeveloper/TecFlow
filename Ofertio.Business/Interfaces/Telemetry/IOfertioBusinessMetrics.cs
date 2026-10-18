namespace Ofertio.Business.Interfaces.Telemetry;

public interface IOfertioBusinessMetrics
{
    long ComentariosProcessadosTotal { get; }
    long LinksEnviadosSucessoTotal { get; }
    long ErrosConciliacaoContagem { get; }

    void RecordCommentProcessed(bool success);
    void RecordAffiliateLinkSent(bool success);
    void RecordConciliationError();
}
