using Ofertio.Business.Interfaces.Services;

namespace Ofertio.API.Middlewares;

/// <summary>Conta requisições /api para o dashboard executivo (últimas 24h).</summary>
public sealed class ApiRequestCounterMiddleware
{
    private readonly RequestDelegate _next;

    public ApiRequestCounterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, IApiRequestCounter counter)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            counter.Increment();
        }

        return _next(context);
    }
}
