// Caminho Completo: TecFlow.Core\Exceptions\UnauthorizedAccessExceptionCustom.cs

using System;
using System.Net; // Necessário para HttpStatusCode (ENUM)

namespace TecFlow.Core.Exceptions
{
    /// <summary>
    /// Exceção lançada quando um usuário não tem permissão para realizar uma ação.
    /// </summary>
    public class UnauthorizedAccessExceptionCustom : BaseCustomException
    {
        public UnauthorizedAccessExceptionCustom(string message)
            : base(message, (int)System.Net.HttpStatusCode.Forbidden) // Usamos o ENUM System.Net.HttpStatusCode
        {
        }

        public UnauthorizedAccessExceptionCustom(string message, Exception innerException)
            : base(message, innerException, (int)System.Net.HttpStatusCode.Forbidden) // Usamos o ENUM System.Net.HttpStatusCode
        {
        }
    }
}