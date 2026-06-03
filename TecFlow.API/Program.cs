using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TecFlow.API.Middlewares;
using TecFlow.Business.Service.Application;
using TecFlow.Infrastructure;
using TecFlow.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

DatabaseUrlConfiguration.ApplyCloudDatabaseUrl(builder.Configuration);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTecFlowCoreServices();
builder.Services.AddTecFlowInfrastructureServices(builder.Configuration);
builder.Services.AddTecFlowInfrastructureData(builder.Configuration);
builder.Services.AddTecFlowApplicationServices();

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

// Primeiro middleware: envolve todo o pipeline e padroniza respostas de erro (JSON).
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
