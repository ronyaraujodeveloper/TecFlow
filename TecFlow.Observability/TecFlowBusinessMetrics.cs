using System.Diagnostics.Metrics;
using TecFlow.Business.Interfaces.Telemetry;

namespace TecFlow.Observability;

public sealed class TecFlowBusinessMetrics : ITecFlowBusinessMetrics, IDisposable
{
    public const string MeterName = "TecFlow.Business";

    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _comentariosProcessados;
    private readonly Counter<long> _linksEnviadosSucesso;
    private readonly Counter<long> _errosConciliacao;

    private long _comentariosSnapshot;
    private long _linksSnapshot;
    private long _errosConciliacaoSnapshot;

    public TecFlowBusinessMetrics()
    {
        _comentariosProcessados = _meter.CreateCounter<long>(
            "comentarios_processados_total",
            description: "Total de comentários processados na fila de engajamento");

        _linksEnviadosSucesso = _meter.CreateCounter<long>(
            "links_enviados_sucesso",
            description: "Links de afiliado enviados com sucesso após triagem");

        _errosConciliacao = _meter.CreateCounter<long>(
            "erros_conciliacao_contagem",
            description: "Erros durante conciliação financeira de comissões");
    }

    public long ComentariosProcessadosTotal => Interlocked.Read(ref _comentariosSnapshot);
    public long LinksEnviadosSucessoTotal => Interlocked.Read(ref _linksSnapshot);
    public long ErrosConciliacaoContagem => Interlocked.Read(ref _errosConciliacaoSnapshot);

    public void RecordCommentProcessed(bool success)
    {
        Interlocked.Increment(ref _comentariosSnapshot);
        _comentariosProcessados.Add(1, new KeyValuePair<string, object?>("success", success));
    }

    public void RecordAffiliateLinkSent(bool success)
    {
        if (success)
        {
            Interlocked.Increment(ref _linksSnapshot);
            _linksEnviadosSucesso.Add(1);
        }
    }

    public void RecordConciliationError()
    {
        Interlocked.Increment(ref _errosConciliacaoSnapshot);
        _errosConciliacao.Add(1);
    }

    public void Dispose() => _meter.Dispose();
}
