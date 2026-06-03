using TecFlow.Portal.Components;
using TecFlow.Portal.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddPortalAuthentication(builder.Configuration);
builder.Services.AddPortalServices(builder.Configuration);

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

app.MapPortalAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
