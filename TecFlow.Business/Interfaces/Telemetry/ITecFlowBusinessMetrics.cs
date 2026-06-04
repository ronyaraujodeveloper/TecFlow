namespace TecFlow.Business.Interfaces.Telemetry;

public interface ITecFlowBusinessMetrics
{
    long ComentariosProcessadosTotal { get; }
    long LinksEnviadosSucessoTotal { get; }
    long ErrosConciliacaoContagem { get; }

    void RecordCommentProcessed(bool success);
    void RecordAffiliateLinkSent(bool success);
    void RecordConciliationError();
}
