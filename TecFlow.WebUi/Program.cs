using TecFlow.SharedUi.Extensions;
using TecFlow.WebUi.Components;
using TecFlow.WebUi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddWebUiServices(builder.Configuration);
builder.Services.AddWebUiAuthentication(builder.Configuration);

var app = builder.Build();

// wwwroot, _content (RCL) e *.styles.css devem ser atendidos antes de auth/rotas Blazor.
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapWebUiAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ServiceCollectionExtensions).Assembly);

app.Run();
