namespace TecFlow.Database.MultiTenancy;

/// <summary>
/// Os filtros globais de tenant/loja ficam em <see cref="AppDbContext"/> como lambdas
/// de instância. O EF religa <c>CurrentTenantId</c> ao contexto da query; capturar
/// <see cref="ICurrentTenantService"/> no modelo (primeiro request) fazia o login sem JWT
/// ocultar todos os <c>Usuarios</c> e devolver "Credenciais inválidas".
/// </summary>
internal static class TenantQueryFilterExtensions
{
}
