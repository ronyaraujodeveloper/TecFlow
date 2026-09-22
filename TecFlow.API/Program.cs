using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TecFlow.API.Middlewares;
using TecFlow.API.Extensions;
using TecFlow.Business.Dto;
using TecFlow.Business.Service.Application;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Infrastructure;
using TecFlow.Infrastructure.Services;
using TecFlow.Observability;

var builder = WebApplication.CreateBuilder(args);

DatabaseUrlConfiguration.ApplyCloudDatabaseUrl(builder.Configuration);

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
            .CreateLogger("TecFlow.API.ModelState");
        var fields = FormatModelState(context.ModelState);
        logger.LogWarning("ModelState inválido. Campos: {Fields}", fields);
        return new BadRequestObjectResult(MarketplaceAccountResponseDto.Fail($"Payload de vinculação inválido. {fields}"));
    };
});
builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();
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

builder.Services.AddTecFlowCoreServices();
builder.Services.AddTecFlowInfrastructureServices(builder.Configuration);
builder.Services.AddTecFlowInfrastructureData(builder.Configuration);
builder.Services.AddTecFlowApplicationServices();
builder.Services.AddAffiliateLinkInfrastructureServices();
builder.Services.AddAffiliateLinkStrategyServices();
builder.Services.AddTecFlowEngagementMessaging(builder.Configuration, TecFlow.Infrastructure.Services.Messaging.TecFlowMessagingRole.Publisher);
builder.Services.AddTecFlowTelemetry(builder.Configuration, "TecFlow.API", enableAspNetCoreInstrumentation: true);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Key"] ?? jwtSection["Secret"]
    ?? throw new InvalidOperationException("JWT Secret is missing in configuration (Jwt:Key / Jwt:Secret).");
var jwtIssuer = string.IsNullOrWhiteSpace(jwtSection["Issuer"]) ? "TecFlowAPI" : jwtSection["Issuer"]!;
var jwtAudience = string.IsNullOrWhiteSpace(jwtSection["Audience"]) ? "TecFlowClient" : jwtSection["Audience"]!;
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
        ValidAudiences = new[] { jwtAudience, "TecFlowClient" },
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
});

var app = builder.Build();

app.UseTecFlowTelemetry();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Homologação/IIS: exposto globalmente para diagnóstico (restringir por ambiente após validação).
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TecFlow.API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (args is { Length: >= 3 }
    && string.Equals(args[0], "--reset-user-password", StringComparison.OrdinalIgnoreCase))
{
    await app.ResetLocalUserPasswordAsync(args[1], args[2]);
    return;
}

await app.SeedHomologDemoUserAsync();

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
