using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Core.Exceptions;

namespace TecFlow.API.Middlewares;

/// <summary>
/// Captura exceções não tratadas, registra contexto seguro (LGPD) e retorna ProblemDetails em JSON.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, ex, ex.StatusCode, LogLevel.Warning);
        }
        catch (UnauthorizedAccessExceptionCustom ex)
        {
            await WriteProblemAsync(context, ex, ex.StatusCode, LogLevel.Warning);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, ex, StatusCodes.Status403Forbidden, LogLevel.Warning);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex, StatusCodes.Status500InternalServerError, LogLevel.Error);
        }
    }

    private async Task WriteProblemAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        LogLevel level)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Resposta já iniciada; não foi possível escrever ProblemDetails.");
            throw exception;
        }

        var route = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var authContext = BuildSafeAuthContext(context);

        _logger.Log(
            level,
            exception,
            "Exceção na API {Method} {Route} Status={StatusCode} TraceId={TraceId} UserId={UserId} AuthContext={AuthContext}",
            method,
            route,
            statusCode,
            traceId,
            userId,
            authContext);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = GetSafeDetail(statusCode, exception),
            Instance = route,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problem.Extensions["traceId"] = traceId;

        if (_env.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().Name;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static string? BuildSafeAuthContext(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var provider = context.Request.Query.TryGetValue("provider", out var providerValue)
            ? providerValue.ToString()
            : null;

        return $"AuthRoute={context.Request.Path};Provider={provider ?? "n/a"};HasBody={context.Request.ContentLength > 0}";
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Recurso não encontrado",
        StatusCodes.Status401Unauthorized => "Não autenticado",
        StatusCodes.Status403Forbidden => "Acesso negado",
        StatusCodes.Status400BadRequest => "Requisição inválida",
        _ => "Erro interno do servidor"
    };

    private string GetSafeDetail(int statusCode, Exception exception)
    {
        if (_env.IsDevelopment())
        {
            return exception.Message;
        }

        return statusCode switch
        {
            StatusCodes.Status404NotFound => "O recurso solicitado não foi encontrado.",
            StatusCodes.Status403Forbidden => "Você não tem permissão para acessar este recurso.",
            StatusCodes.Status401Unauthorized => "Credenciais inválidas ou sessão expirada.",
            _ => "Ocorreu um erro interno inesperado no servidor."
        };
    }
}
