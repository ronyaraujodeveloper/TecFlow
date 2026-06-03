using Microsoft.Extensions.Configuration;      // Essencial para ConfigurationBuilder e GetConnectionString
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection; // Essencial para ServiceCollection e AddDbContext
using Microsoft.Extensions.Logging;            // Essencial para ILoggingBuilder e ClearProviders
using Serilog;                                 // Essencial para Log.Logger, AddSerilog
using System;                                  // Essencial para Environment, Exception, etc.
using System.IO;                               // Essencial para Directory.GetCurrentDirectory()
using System.Threading.Tasks;                  // Essencial para await e Task
using TecFlow.Configuracao;
using TecFlow.Core.Interfaces.Repositories;
using TecFlow.Core.Interfaces.Services;
using TecFlow.Infrastructure.Configuration;
using TecFlow.Database;
using TecFlow.Infrastructure.Interfaces;
using TecFlow.Infrastructure.Security;
using TecFlow.Infrastructure.Services.ExternalServices;
using TecFlow.Infrastructure.Services.Repositories;

// RESOLUÇÃO DA AMBIGUIDADE (Mantendo o padrão estruturado anterior)
using CoreServices = TecFlow.Core.Interfaces.Services;

namespace TecFlow.Configuracao
{
    internal class Configurator
    {
        public async Task ConfigureAndRunAsync()
        {
            // Configurar Serilog para logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Iniciando aplicação...");

                // Construir configuração
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                    .AddUserSecrets<Program>()
                    .AddEnvironmentVariables()
                    .Build();

                // Configurar serviços (Dependency Injection)
                var services = new ServiceCollection();

                // REGISTRO DE CONTEXTO HTTP E TENANCY
                services.AddHttpContextAccessor(); // Agora reconhecido com o using correto
                services.AddScoped<IUserContextProvider, UserContextProvider>();

                // Logging
                services.AddLogging(config =>
                {
                    config.ClearProviders();
                    config.AddSerilog();
                });

                // Mapeia a configuração para IAppConfiguration
                services.AddSingleton<IAppConfiguration, AppConfiguration>();
                services.AddSingleton(configuration);

                // Banco de Dados
                var dbProvider = configuration.GetSection("Database:Provider").Value;
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(dbProvider) || string.IsNullOrEmpty(connectionString))
                {
                    Log.Error("Configuração de Banco de Dados incompleta. Verifique 'Database:Provider' e 'DefaultConnection'.");
                }
                else
                {
                    if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                    {
                        services.AddDbContext<AppDbContext>(options =>
                            options.UseNpgsql(connectionString));
                    }
                    else
                    {
                        services.AddDbContext<AppDbContext>(options =>
                            options.UseSqlite(connectionString));
                    }
                }

                // Serviços de negócio
                services.AddScoped<IDataService, DataService>();
                services.AddScoped<IAnaliseService, AnaliseService>();
                services.AddScoped<IRankingService, RankingService>();

                // Repositories
                services.AddScoped<IProductRepository, ProductRepository>();
                services.AddScoped<IContentRepository, ContentRepository>();
                services.AddScoped<ICampaignRepository, CampaignRepository>();

                // Orquestrador principal mapeado sem ambiguidade
                services.AddScoped<CoreServices.IOrquestradorService, OrquestradorPrincipal>();

                // Construir o Service Provider
                var serviceProvider = services.BuildServiceProvider();

                // Executar orquestrador
                var orquestrador = serviceProvider.GetRequiredService<CoreServices.IOrquestradorService>();
                await orquestrador.ExecuteFullPipelineAsync();

                Log.Information("Aplicação finalizada com sucesso.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Aplicação encerrou com erro.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}