using Microsoft.AspNetCore.Components.Server.Circuits;
using Serilog;
using Ofertio.SharedUi.Extensions;
using Ofertio.WebUi.Components;
using Ofertio.WebUi.Extensions;
using Ofertio.WebUi.Logging;

var cultureInfo = new System.Globalization.CultureInfo("pt-BR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<CircuitHandler, BlazorCircuitLoggingHandler>();
builder.Services.AddWebUiServices(builder.Configuration, builder.Environment);
builder.Services.AddWebUiAuthentication(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

// wwwroot, _content (RCL) e *.styles.css devem ser atendidos antes de auth/rotas Blazor.
app.UseStaticFiles();
app.UseOfertioProductUploads();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseMasterRouteGuard();
app.UseAntiforgery();

app.MapWebUiAuthEndpoints();
app.MapIntegracoesEndpoints();
app.MapMasterEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ServiceCollectionExtensions).Assembly);

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
