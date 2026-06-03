// Caminho Completo: TecFlow.Core\Exceptions\BaseCustomException.cs

using System;
using System.Net; // Importamos o namespace para poder usar o enum HttpStatusCode

namespace TecFlow.Core.Exceptions
{
    /// <summary>
    /// Classe base para exceções customizadas da aplicação.
    /// </summary>
    public abstract class BaseCustomException : Exception
    {
        /// <summary>
        /// O código de status HTTP associado a esta exceção.
        /// </summary>
        public int StatusCode { get; } // Renomeado para 'StatusCode' (sem 'Http') para evitar confusão com o enum.

        // Construtor para exceções sem inner exception
        protected BaseCustomException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode; // Atribui ao membro de INSTÂNCIA
        }

        // Construtor para exceções com inner exception
        protected BaseCustomException(string message, Exception innerException, int statusCode) : base(message, innerException)
        {
            StatusCode = statusCode; // Atribui ao membro de INSTÂNCIA
        }
    }
}