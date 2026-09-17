using Microsoft.AspNetCore.Identity;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.API.Extensions;

/// <summary>
/// Garante credenciais válidas do usuário demo de homologação (demo@tecso.local / Test@123).
/// </summary>
public static class HomologDemoUserSeeder
{
    private const string DemoEmail = "demo@tecso.local";
    private const string LegacyDemoEmail = "demo@TecFlow.local";
    private const string DemoPassword = "Test@123";

    public static async Task SeedHomologDemoUserAsync(this WebApplication app)
    {
        if (!IsSeedEnvironment(app.Environment.EnvironmentName))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
        var tenantProvisioning = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TecFlow.HomologSeed");

        currentTenant.BypassTenantFilters = true;

        await EnsureDemoUserAsync(userRepository, tenantProvisioning, userManager, DemoEmail, logger);
        await EnsureDemoUserAsync(userRepository, tenantProvisioning, userManager, LegacyDemoEmail, logger);
    }

    private static bool IsSeedEnvironment(string environmentName) =>
        string.Equals(environmentName, "Homologacao", StringComparison.OrdinalIgnoreCase)
        || string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);

    private static async Task EnsureDemoUserAsync(
        IUserAccountRepository userRepository,
        ITenantProvisioningService tenantProvisioning,
        UserManager<UserAccount> userManager,
        string email,
        ILogger logger)
    {
        var user = await userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            logger.LogInformation("Criando usuário demo {Email}...", email);
            user = new UserAccount
            {
                Name = "Usuário Demo Homologação",
                Email = email,
                PasswordHash = string.Empty,
                Plan = "Pro",
                WhatsAppPhone = "11987654321",
                CreatedAt = DateTime.UtcNow
            };

            var tenant = await tenantProvisioning.EnsureTenantForUserAsync(user);
            user.TenantId = tenant.Id;
            await userRepository.AddAsync(user);
        }

        user = await userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            logger.LogWarning("Não foi possível localizar usuário demo {Email} após criação.", email);
            return;
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var removeResult = await userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                logger.LogWarning(
                    "Falha ao remover senha anterior do demo {Email}: {Errors}",
                    email,
                    string.Join("; ", removeResult.Errors.Select(error => error.Description)));
            }
        }

        var addPasswordResult = await userManager.AddPasswordAsync(user, DemoPassword);
        if (!addPasswordResult.Succeeded)
        {
            logger.LogError(
                "Falha ao definir senha demo para {Email}: {Errors}",
                email,
                string.Join("; ", addPasswordResult.Errors.Select(error => error.Description)));
            return;
        }

        logger.LogInformation("Senha demo sincronizada para {Email}.", email);
    }

    /// <summary>
    /// Redefine a senha de um usuário local (Homologação/Development) via BCrypt + AppDbContext,
    /// sem a política Identity (cadastro exige 8+ caracteres, maiúscula e especial).
    /// </summary>
    public static async Task ResetLocalUserPasswordAsync(
        this WebApplication app,
        string email,
        string password)
    {
        if (!IsSeedEnvironment(app.Environment.EnvironmentName))
        {
            Console.WriteLine("Reset de senha permitido apenas em Homologacao/Development.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Informe e-mail e senha.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
        var tenantProvisioning = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TecFlow.HomologSeed");

        currentTenant.BypassTenantFilters = true;
        email = email.Trim();

        var user = await userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            logger.LogInformation("Criando usuário local {Email} para reset de senha...", email);
            user = new UserAccount
            {
                Name = email.Split('@')[0],
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Plan = "Pro",
                CreatedAt = DateTime.UtcNow
            };

            var tenant = await tenantProvisioning.EnsureTenantForUserAsync(user);
            user.TenantId = tenant.Id;
            await userRepository.AddAsync(user);
            Console.WriteLine($"Usuário {email} criado e senha definida.");
            return;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user);
        Console.WriteLine($"Senha redefinida para {email}.");
    }
}
