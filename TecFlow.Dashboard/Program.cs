using Microsoft.AspNetCore.Diagnostics;
using TecFlow.Business.Service.Application;
using TecFlow.Infrastructure;
using TecFlow.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

DatabaseUrlConfiguration.ApplyCloudDatabaseUrl(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddTecFlowCoreServices();
builder.Services.AddTecFlowInfrastructureServices(builder.Configuration);
builder.Services.AddTecFlowInfrastructureData(builder.Configuration);
builder.Services.AddTecFlowApplicationServices();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(appError =>
    {
        appError.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            var errorFeature = context.Features.Get<IExceptionHandlerFeature>()!;
            var exception = errorFeature.Error;
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "Erro global não tratado capturado pelo middleware.");
            var errorResponse = new { Message = "Ocorreu um erro interno no servidor.", ErrorCode = "INTERNAL_SERVER_ERROR" };
            var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(json);
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
