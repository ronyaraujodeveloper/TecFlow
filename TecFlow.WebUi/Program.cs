using TecFlow.SharedUi.Extensions;
using TecFlow.WebUi.Components;
using TecFlow.WebUi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddWebUiServices(builder.Configuration);
builder.Services.AddWebUiAuthentication(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapWebUiAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ServiceCollectionExtensions).Assembly);

app.Run();
