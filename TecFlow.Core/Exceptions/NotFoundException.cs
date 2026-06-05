// Caminho Completo: TecFlow.Core\Exceptions\NotFoundException.cs

using System;
using System.Net; // Necessário para HttpStatusCode (ENUM)

namespace TecFlow.Core.Exceptions
{
    /// <summary>
    /// Exceção lançada quando um recurso solicitado não é encontrado.
    /// </summary>
    public class NotFoundException : BaseCustomException
    {
        public NotFoundException(string message)
            // Chama o construtor da classe base, passando a mensagem e o código HTTP como INT
            : base(message, (int)System.Net.HttpStatusCode.NotFound) // Usamos o ENUM System.Net.HttpStatusCode
        {
        }

        public NotFoundException(string message, Exception innerException)
            // Chama o construtor da classe base com inner exception
            : base(message, innerException, (int)System.Net.HttpStatusCode.NotFound) // Usamos o ENUM System.Net.HttpStatusCode
        {
        }
    }
}