using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ofertio.API.Middlewares;
using Ofertio.API.Extensions;
using Ofertio.API.Security;
using Ofertio.Infrastructure.Services.Master;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.Application;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Infrastructure.Services.LinkStrategies;
using Ofertio.Infrastructure;
using Ofertio.Infrastructure.Security;
using Ofertio.Infrastructure.Services;
using Ofertio.Observability;

var builder = WebApplication.CreateBuilder(args);

DatabaseUrlConfiguration.ApplyCloudDatabaseUrl(builder.Configuration);
builder.Configuration.AddOfertioDatabaseSettings();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "app-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.JsonSerializerOptions.Converters.Add(new MarketplaceTypeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Ofertio.API.ModelState");
        var fields = FormatModelState(context.ModelState);
        logger.LogWarning("ModelState inválido. Campos: {Fields}", fields);
        return new BadRequestObjectResult(MarketplaceAccountResponseDto.Fail($"Payload de vinculação inválido. {fields}"));
    };
});
builder.Services.AddProblemDetails();
builder.Services.AddOfertioMasterAuthorization();
builder.Services.AddCors(options => options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddOfertioCoreServices();
builder.Services.AddOfertioDataProtection(builder.Configuration);
builder.Services.AddOfertioInfrastructureServices(builder.Configuration);
// IDbContextFactory<AppDbContext> + AddDbContext (SQL Server / tenant scoped) — paralelo seguro no /busca.
builder.Services.AddOfertioInfrastructureData(builder.Configuration);
builder.Services.AddOfertioApplicationServices();
builder.Services.AddAffiliateLinkInfrastructureServices();
builder.Services.AddAffiliateLinkStrategyServices();
builder.Services.AddOfertioEngagementMessaging(builder.Configuration, Ofertio.Infrastructure.Services.Messaging.OfertioMessagingRole.Publisher);
builder.Services.AddOfertioTelemetry(builder.Configuration, "Ofertio.API", enableAspNetCoreInstrumentation: true);
builder.Services.AddOfertioMasterBackoffice();
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    options.ShutdownTimeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHostedService<Ofertio.API.Workers.WhatsAppBroadcastWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.TelegramBroadcastWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.TelegramUserMonitorWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.OfferMiningWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.OfferIntelligenceWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.PreFlightWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.GlobalTrendingDealsWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.ProductImageCleanupWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.OfferRevalidationWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.CouponReclassificationWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.ProductTagBackfillWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.DataPurgeWorker>();
builder.Services.AddHostedService<Ofertio.API.Workers.TenantBillingWorker>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Key"] ?? jwtSection["Secret"]
    ?? throw new InvalidOperationException("JWT Secret is missing in configuration (Jwt:Key / Jwt:Secret).");
var jwtIssuer = string.IsNullOrWhiteSpace(jwtSection["Issuer"]) ? "OfertioAPI" : jwtSection["Issuer"]!;
var jwtAudience = string.IsNullOrWhiteSpace(jwtSection["Audience"]) ? "OfertioClient" : jwtSection["Audience"]!;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidAudiences = new[] { jwtAudience, "OfertioClient" },
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                status = false,
                descricao = "Sessão expirada ou não autenticada. Entre novamente no Ofertio.",
                errorCode = "UNAUTHORIZED"
            });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                status = false,
                descricao = "Seu perfil não tem permissão para esta área do Painel Master.",
                errorCode = "FORBIDDEN"
            });
        }
    };
});

var app = builder.Build();

app.UseOfertioTelemetry();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Homologação/IIS: exposto globalmente para diagnóstico (restringir por ambiente após validação).
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ofertio.API v1");
    c.RoutePrefix = "swagger";
});

var uploadStorage = app.Services.GetRequiredService<IUploadStorage>();
Directory.CreateDirectory(Path.Combine(uploadStorage.UploadsRoot, "products"));
var staticTypes = new FileExtensionContentTypeProvider();
staticTypes.Mappings[".webp"] = "image/webp";
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadStorage.UploadsRoot),
    RequestPath = UploadStorageRules.RequestPath,
    ContentTypeProvider = staticTypes
});
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = staticTypes });
app.Logger.LogInformation(
    "Uploads servidos em {RequestPath} a partir de {UploadsRoot}",
    UploadStorageRules.RequestPath,
    uploadStorage.UploadsRoot);
app.UseRouting();
app.UseCors("AllowAll");
app.UseMiddleware<ApiRequestCounterMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantAccessGuardMiddleware>();
app.UseAuthorization();
app.MapControllers();

if (args is { Length: >= 3 }
    && string.Equals(args[0], "--reset-user-password", StringComparison.OrdinalIgnoreCase))
{
    await app.ResetLocalUserPasswordAsync(args[1], args[2]);
    return;
}

try
{
    await app.SeedHomologDemoUserAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Seed de homologação falhou. A API continuará no ar.");
}

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

static string FormatModelState(ModelStateDictionary modelState) =>
    string.Join("; ", modelState
        .Where(entry => entry.Value is { Errors.Count: > 0 })
        .Select(entry =>
        {
            var messages = entry.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.Exception?.Message ?? "inválido"
                    : error.ErrorMessage);
            return $"{entry.Key}: {string.Join(", ", messages)}";
        }));
