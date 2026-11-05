using Microsoft.Playwright;

namespace Ofertio.E2E;

public class CatalogFlowTests
{
    [Fact]
    public void CatalogPilot_DescribesTheRangeMessageAndAProductUrlWithoutOAuth()
    {
        Assert.Contains("mínimo", CatalogPilot.MinGreaterThanMax, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("máximo", CatalogPilot.MinGreaterThanMax, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("https://shopee.com.br/", CatalogPilot.SampleProductUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("oauth", CatalogPilot.SampleProductUrl, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(CatalogPilot.QuickSearch));
    }

    [RequiresE2EFact]
    public async Task Search_RejectsAMinimumGreaterThanTheMaximum()
    {
        await using var session = await OpenAsync("/busca", "Buscar Ofertas");
        var page = session.Page;

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Filtros", Exact = true }).ClickAsync();
        await page.Locator("#busca-min").WaitForAsync();
        await page.Locator("#busca-min").FillAsync("500");
        await page.Locator("#busca-max").FillAsync("100");
        await page.Locator("#busca-max").BlurAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Aplicar Filtros" }).ClickAsync();

        await Assertions.Expect(page.GetByText(CatalogPilot.MinGreaterThanMax)).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Filtros avançados" })).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByText("Buscando ofertas...")).ToHaveCountAsync(0);
        Assert.Contains("/busca", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresE2EFact]
    public async Task MonitoredGroups_AppliesTheQuickSearch()
    {
        await using var session = await OpenAsync("/integracoes/grupos/monitorados", "Grupos monitorados");
        var page = session.Page;
        await page.Locator("#live-search-q").WaitForAsync();

        var pending = page.WaitForResponseAsync(
            response => response.Url.Contains($"q={CatalogPilot.QuickSearch}", StringComparison.OrdinalIgnoreCase),
            new PageWaitForResponseOptions { Timeout = 30_000 });
        await page.Locator("#live-search-q").FillAsync(CatalogPilot.QuickSearch);
        await page.Locator("#live-search-q").BlurAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Buscar", Exact = true }).ClickAsync();

        var response = await pending;
        Assert.True(response.Ok);
        await Assertions.Expect(page.Locator("#live-search-q")).ToHaveValueAsync(CatalogPilot.QuickSearch);
        await Assertions.Expect(page.Locator("#ofertas-container")).ToBeVisibleAsync(Visible);
        Assert.Contains("/grupos/monitorados", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresE2EFact]
    public async Task LinkGenerator_FillsTheProductUrlWithoutStartingOAuth()
    {
        await using var session = await OpenAsync("/gerador-links", "Gerador de Links de Comissão");
        var page = session.Page;

        await page.Locator("#product-url").FillAsync(CatalogPilot.SampleProductUrl);
        await page.Locator("#product-url").BlurAsync();
        await Assertions.Expect(page.GetByText("Shopee")).ToBeVisibleAsync(Visible);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Gerar Link de Comissão" }).ClickAsync();

        var result = page.Locator("#link-generator-result");
        var feedback = page.GetByText("não tem uma conta").Or(page.Locator(".alert-danger"));
        await Assertions.Expect(result.Or(feedback)).ToBeVisibleAsync(Visible);
        Assert.DoesNotContain("oauth", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/minhas-lojas", page.Url, StringComparison.OrdinalIgnoreCase);

        if (await result.IsVisibleAsync())
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                Name = "Seus links de comissão"
            })).ToBeVisibleAsync(Visible);
        }
    }

    private static async Task<PortalSession> OpenAsync(string path, string heading)
    {
        Assert.True(E2ESettings.TryLoad(out var settings));
        var configured = settings ?? throw new InvalidOperationException("Homologação sem configuração.");
        var session = await PortalSession.OpenAsync(configured);
        await session.LoginAsync();
        await session.ExpectHeadingAsync(path, heading);
        return session;
    }

    private static LocatorAssertionsToBeVisibleOptions Visible => new() { Timeout = 30_000 };
}
