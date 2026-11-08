using Microsoft.Playwright;

namespace Ofertio.E2E;

public class MasterFlowTests
{
    [Fact]
    public void MasterPilot_ReadsFiltersAndLeavesCreditsAndSuspensionAlone()
    {
        Assert.Contains("audit-action", MasterPilot.ReadOnlyIds);
        Assert.Contains("tenant-search", MasterPilot.ReadOnlyIds);
        Assert.DoesNotContain(MasterPilot.ReadOnlyIds, id => MasterPilot.UntouchedIds.Contains(id));
        Assert.Contains("tenant-credits", MasterPilot.UntouchedIds);
        Assert.Contains("tenant-action", MasterPilot.UntouchedIds);
        Assert.StartsWith(E2ESettings.RecordPrefix, E2ESettings.RecordName("operador"), StringComparison.Ordinal);
    }

    [RequiresE2EFact]
    public async Task Operator_IsCreatedAndThenDeactivated()
    {
        await using var visit = await OpenMasterAsync("/master/operadores", "Operadores master e matriz de permissões");
        if (!visit.Allowed || await visit.Page.Locator("#operator-name").CountAsync() == 0)
        {
            return;
        }

        var page = visit.Page;
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var name = E2ESettings.RecordName($"operador-{stamp}");
        var email = $"e2e-operador-{stamp}@example.com";

        await page.Locator("#operator-name").FillAsync(name);
        await page.Locator("#operator-email").FillAsync(email);
        await page.Locator("#operator-password").FillAsync($"E2e-{stamp}!a");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salvar", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByText("Operador salvo.")).ToBeVisibleAsync(Visible);

        var row = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = email });
        await Assertions.Expect(row.GetByText("Ativo")).ToBeVisibleAsync(Visible);
        await row.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Desativar" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Operador inativado.")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(row.GetByText("Inativo")).ToBeVisibleAsync(Visible);
    }

    [RequiresE2EFact]
    public async Task PlanHighlights_AreRestoredToThePreviousText()
    {
        await using var visit = await OpenMasterAsync("/master/planos", "Planos e matriz de funcionalidades");
        if (!visit.Allowed)
        {
            return;
        }

        var page = visit.Page;
        var saves = page.Locator("[id^='plan-save-']");
        if (await saves.CountAsync() == 0)
        {
            return;
        }

        var save = saves.First;
        var saveId = await save.GetAttributeAsync("id");
        var code = saveId!["plan-save-".Length..];
        var highlights = page.Locator($"[id='plan-highlights-{code}']");
        var planName = await page.Locator($"[id='plan-name-{code}']").InputValueAsync();
        var original = await highlights.InputValueAsync();
        var edited = original.Length > 1996 ? original[..^4] + " E2E" : original + " E2E";
        var changed = false;
        try
        {
            await highlights.FillAsync(edited);
            await highlights.BlurAsync();
            changed = true;
            await save.ClickAsync();
            await Assertions.Expect(page.GetByText($"Plano {planName} salvo.")).ToBeVisibleAsync(Visible);
        }
        finally
        {
            if (changed)
            {
                await highlights.FillAsync(original);
                await highlights.BlurAsync();
                await save.ClickAsync();
                await Assertions.Expect(page.GetByText($"Plano {planName} salvo.")).ToBeVisibleAsync(Visible);
                await Assertions.Expect(page.Locator(".alert-danger")).ToHaveCountAsync(0);
            }
        }
    }

    [RequiresE2EFact]
    public async Task AuditAndTenants_OnlyReadTheFilters()
    {
        await using (var audit = await OpenMasterAsync("/master/auditoria", "Trilha de auditoria imutável"))
        {
            if (audit.Allowed)
            {
                var page = audit.Page;
                var filter = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Filtrar" });
                await Assertions.Expect(filter).ToBeEnabledAsync();
                await page.Locator("#audit-action").FillAsync("e2e.leitura");
                await page.Locator("#audit-action").BlurAsync();
                await filter.ClickAsync();
                await Assertions.Expect(page.GetByText("Nenhum registro.")).ToBeVisibleAsync(Visible);
                Assert.Contains("/master/auditoria", page.Url, StringComparison.OrdinalIgnoreCase);
            }
        }

        await using var tenants = await OpenMasterAsync("/master/tenants", "Central de inquilinos (Tenant 360°)");
        if (!tenants.Allowed)
        {
            return;
        }

        var tenantPage = tenants.Page;
        var tenantFilter = tenantPage.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Filtrar" });
        await Assertions.Expect(tenantFilter).ToBeEnabledAsync();
        await tenantPage.Locator("#tenant-search").FillAsync("e2e-nao-existe");
        await tenantPage.Locator("#tenant-search").BlurAsync();
        await tenantFilter.ClickAsync();
        await Assertions.Expect(tenantPage.GetByText("Nenhum inquilino encontrado.")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(tenantPage.Locator("#tenant-credits")).ToHaveCountAsync(0);
        await Assertions.Expect(tenantPage.Locator("#tenant-action")).ToHaveCountAsync(0);
        await Assertions.Expect(tenantPage.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Aplicar ação" })).ToHaveCountAsync(0);
    }

    [RequiresE2EFact]
    public async Task Terms_ScreenOpensWithoutPublishingAVersion()
    {
        await using var visit = await OpenMasterAsync("/master/termos", "Termos e privacidade");
        if (!visit.Allowed || await visit.Page.Locator("#terms-content").CountAsync() == 0)
        {
            return;
        }

        await Assertions.Expect(visit.Page.Locator("#terms-kind")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(visit.Page.Locator("#terms-publish")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(visit.Page.GetByText("Versão 1.1 publicada.")).ToHaveCountAsync(0);
    }

    private static async Task<MasterVisit> OpenMasterAsync(string path, string heading)
    {
        Assert.True(E2ESettings.TryLoad(out var settings));
        var configured = settings ?? throw new InvalidOperationException("Homologação sem configuração.");
        var session = await PortalSession.OpenAsync(configured);
        await session.LoginAsync();
        var target = new Uri(configured.BaseUrl, path);
        await session.Page.GotoAsync(target.AbsoluteUri, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var title = session.Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
        {
            Name = heading,
            Level = 1,
            Exact = true
        });
        try
        {
            await title.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15_000
            });
            return new MasterVisit(session, true);
        }
        catch (TimeoutException)
        {
            if (!session.Page.Url.Contains("/master", StringComparison.OrdinalIgnoreCase))
            {
                return new MasterVisit(session, false);
            }

            throw;
        }
    }

    private static LocatorAssertionsToBeVisibleOptions Visible => new() { Timeout = 30_000 };

    private sealed class MasterVisit(PortalSession session, bool allowed) : IAsyncDisposable
    {
        public bool Allowed { get; } = allowed;

        public IPage Page => session.Page;

        public ValueTask DisposeAsync() => session.DisposeAsync();
    }
}
