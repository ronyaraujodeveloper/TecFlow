// Caminho Completo: TecFlow.API\Middleware\ExceptionMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net; // Necessário para HttpStatusCode (enum nativo)
using System.Text.Json;
using System.Threading.Tasks;
using TecFlow.Core.Exceptions; // Importar as exceções customizadas criadas

namespace TecFlow.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
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
            // --- Tratamento de Exceções Específicas ---
            // Captura exceções customizadas e usa a propriedade de INSTÂNCIA 'StatusCode' para definir o status HTTP.
            catch (NotFoundException nfEx) // nfEx é uma INSTÂNCIA da exceção
            {
                _logger.LogWarning(nfEx, "Requisição resultou em NotFound: {Message}", nfEx.Message);
                // Acessamos nfEx.StatusCode que é um membro de INSTÂNCIA.
                await HandleExceptionAsync(context, nfEx, nfEx.StatusCode);
            }
            catch (UnauthorizedAccessExceptionCustom uaEx) // uaEx é uma INSTÂNCIA da exceção
            {
                _logger.LogWarning(uaEx, "Requisição falhou por falta de autorização: {Message}", uaEx.Message);
                // Acessamos uaEx.StatusCode que é um membro de INSTÂNCIA.
                await HandleExceptionAsync(context, uaEx, uaEx.StatusCode);
            }
            // Captura exceções genéricas do sistema
            catch (System.UnauthorizedAccessException sysAuthEx) // Esta é uma exceção do .NET
            {
                _logger.LogWarning(sysAuthEx, "Requisição falhou por acesso não autorizado (Sistema): {Message}", sysAuthEx.Message);
                // Para exceções do sistema que não têm nosso 'StatusCode' customizado, usamos o status padrão.
                await HandleExceptionAsync(context, sysAuthEx, (int)System.Net.HttpStatusCode.Forbidden); // 403 Forbidden
            }
            // --- Tratamento Genérico (para erros não previstos) ---
            catch (Exception ex) // ex é uma INSTÂNCIA da exceção
            {
                _logger.LogError(ex, "Uma exceção não tratada ocorreu durante a requisição: {Message}", ex.Message);
                // Se a exceção não for uma das nossas customizadas, definimos o status como 500.
                // Mesmo que a exceção tenha uma propriedade `HttpStatusCode` (o que é incomum para exceções genéricas do sistema),
                // usar um status padrão de 500 para erros não mapeados é mais seguro.
                await HandleExceptionAsync(context, ex, (int)System.Net.HttpStatusCode.InternalServerError); // 500 Internal Server Error por padrão
            }
        }

        /// <summary>
        /// Manipula a exceção e constrói a resposta HTTP padrão.
        /// </summary>
        /// <param name="context">O HttpContext da requisição.</param>
        /// <param name="exception">A exceção ocorrida.</param>
        /// <param name="statusCodeInt">O código de status HTTP a ser retornado como inteiro.</param>
        private Task HandleExceptionAsync(HttpContext context, Exception exception, int statusCodeInt)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCodeInt; // Define corretamente o status code da resposta

            string responseMessage = exception.Message; // Padrão: Usar a mensagem da exceção original
            string? responseDetail = _env.IsDevelopment() ? exception.StackTrace?.ToString() : null; // Stack trace apenas em dev

            // Mensagens de erro mais genéricas e seguras para produção
            if (!_env.IsDevelopment())
            {
                // Tenta obter uma mensagem mais genérica baseada no status code se a exceção for interna.
                var statusCodeEnum = (HttpStatusCode)statusCodeInt; // Converte int para enum para facilitar a comparação
                switch (statusCodeEnum)
                {
                    case HttpStatusCode.NotFound:
                        responseMessage = "O recurso solicitado não foi encontrado.";
                        break;
                    case HttpStatusCode.Forbidden:
                        responseMessage = "Você não tem permissão para acessar este recurso.";
                        break;
                    case HttpStatusCode.InternalServerError:
                    default:
                        // Se for um InternalServerError, é um erro desconhecido no servidor.
                        responseMessage = "Ocorreu um erro interno inesperado no servidor.";
                        break;
                }
            }

            // Cria o objeto de resposta, usando o statusCodeInt passado.
            var response = new ErrorDetails(statusCodeInt, responseMessage, responseDetail);

            // Serializa o objeto de resposta para JSON.
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }

    // Record para detalhes de erro mais estruturado.
    public record ErrorDetails(int StatusCode, string Message, string? Detail);
}