using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

using Serilog;

using System.Text;
using TecFlow.Business.Service.Application;
using TecFlow.Infrastructure;

using TecFlow.Infrastructure.Services;

using TecFlow.Infrastructure.Services.Security;

using TecFlow.Observability;

using TecFlow.Orquestrador.Extensions;



var builder = WebApplication.CreateBuilder(args);

DatabaseUrlConfiguration.ApplyCloudDatabaseUrl(builder.Configuration);

var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrEmpty(port))

{

    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

}



builder.Host.UseSerilog((context, configuration) => configuration

    .ReadFrom.Configuration(context.Configuration)

    .Enrich.FromLogContext());



builder.Services.AddTecFlowCoreServices();

builder.Services.AddTecFlowInfrastructureServices(builder.Configuration);

builder.Services.AddTecFlowInfrastructureData(builder.Configuration);

builder.Services.AddTecFlowApplicationServices();
builder.Services.AddTecFlowEngagementMessaging(builder.Configuration, TecFlow.Infrastructure.Services.Messaging.TecFlowMessagingRole.Publisher);

builder.Services.AddTecFlowTelemetry(builder.Configuration, "TecFlow.Orquestrador", enableAspNetCoreInstrumentation: true);



if (args.Contains("--reencrypt-credentials"))

{

    var dryRun = args.Contains("--dry-run");

    await using var reEncryptHost = builder.Build();

    using var scope = reEncryptHost.Services.CreateScope();

    var reEncryptService = scope.ServiceProvider.GetRequiredService<LegacyCredentialReEncryptService>();

    var result = await reEncryptService.ExecuteAsync(dryRun);



    Console.WriteLine($"Usu?rios verificados: {result.UsersScanned}");

    Console.WriteLine($"Usu?rios atualizados: {result.UsersUpdated}");

    Console.WriteLine($"Campos criptografados: {result.FieldsEncrypted}");



    if (result.DryRun)

    {

        Console.WriteLine("(dry-run ? nenhuma altera??o foi persistida)");

    }



    return;

}



builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();



var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()

    ?? [];



builder.Services.AddCors(options =>

{

    options.AddPolicy("TecFlowPortal", policy =>

    {

        if (allowedOrigins.Length > 0)

        {

            policy.WithOrigins(allowedOrigins)

                .AllowAnyHeader()

                .AllowAnyMethod();

        }

        else

        {

            policy.AllowAnyOrigin()

                .AllowAnyHeader()

                .AllowAnyMethod();

        }

    });

});



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

await app.ApplyDatabaseMigrationsAsync();
await app.SeedDevelopmentDataAsync();



if (app.Environment.IsDevelopment())

{

    app.UseDeveloperExceptionPage();

    app.UseSwagger();

    app.UseSwaggerUI();

}

else

{

    app.UseExceptionHandler("/Error");

}



app.UseRouting();

app.UseCors("TecFlowPortal");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



app.MapGet("/health", () => Results.Ok(new { status = "healthy", database = "postgresql" }));



if (app.Environment.IsDevelopment())

{

    app.MapGet("/", () => Results.Content(

        """

        <!DOCTYPE html>

        <html lang="pt-BR">

        <head><meta charset="utf-8"><title>TecFlow Orquestrador ? API</title></head>

        <body style="font-family:Segoe UI,sans-serif;max-width:40rem;margin:3rem auto;padding:0 1rem;">

          <h1>TecFlow.Orquestrador (API + PostgreSQL)</h1>

          <p>Backend hosped?vel em Render / Railway.</p>

          <ul>

            <li><a href="/swagger">Swagger</a></li>

            <li><a href="/health">Health check</a></li>

            <li><a href="https://localhost:7259">Portal ? https://localhost:7259</a></li>

          </ul>

        </body>

        </html>

        """,

        "text/html"));

}



app.Run();

