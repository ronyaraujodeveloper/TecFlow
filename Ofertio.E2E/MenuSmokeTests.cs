namespace Ofertio.E2E;

public class MenuSmokeTests
{
    [Fact]
    public void Catalog_ListsTheInternalScreensWithoutAWriteAction()
    {
        Assert.Equal(6, SmokeRoutes.Pages.Count);
        Assert.Contains(("/dashboard", "Painel"), SmokeRoutes.Pages);
        Assert.Contains(("/meus-dados", "Meus dados"), SmokeRoutes.Pages);
        Assert.Contains(("/propaganda/produtos", "Produtos de propaganda"), SmokeRoutes.Pages);
        Assert.Contains(("/busca", "Buscar Ofertas"), SmokeRoutes.Pages);
        Assert.Contains(("/integracoes/grupos/monitorados", "Grupos monitorados"), SmokeRoutes.Pages);
        Assert.Contains(("/minhas-lojas", "Minhas Lojas / Integrações"), SmokeRoutes.Pages);
        Assert.All(SmokeRoutes.Pages, page => Assert.StartsWith("/", page.Path, StringComparison.Ordinal));
    }

    [RequiresE2EFact]
    public async Task AuthenticatedMenu_ShowsEachScreenTitle()
    {
        Assert.True(E2ESettings.TryLoad(out var settings));
        await using var session = await PortalSession.OpenAsync(settings!);
        await session.LoginAsync();

        foreach (var (path, heading) in SmokeRoutes.Pages)
        {
            await session.ExpectHeadingAsync(path, heading);
        }

        await session.ExpectMasterHeadingWhenAllowedAsync();
    }
}
