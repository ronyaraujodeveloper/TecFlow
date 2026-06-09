using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TecFlow.API.Middlewares;
using TecFlow.API.Extensions;
using TecFlow.Business.Service.Application;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Infrastructure;
using TecFlow.Infrastructure.Services;
using TecFlow.Observability;

var builder = WebApplication.CreateBuilder(args);

DatabaseUrlConfiguration.ApplyCloudDatabaseUrl(builder.Configuration);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddAuthorization();
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

if (jwtSection.Exists())
{
    var jwtSecret = jwtSection["Key"];
    if (string.IsNullOrEmpty(jwtSecret))
    {
        throw new InvalidOperationException("JWT Secret is missing in configuration.");
    }

    var jwtIssuer = jwtSection["Issuer"];
    var jwtAudience = jwtSection["Audience"];

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
            ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });
}

var app = builder.Build();

app.UseTecFlowTelemetry();
app.UseMiddleware<ExceptionMiddleware>();

// Homologação/IIS: exposto globalmente para diagnóstico (restringir por ambiente após validação).
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TecFlow.API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.SeedHomologDemoUserAsync();

app.Run();
