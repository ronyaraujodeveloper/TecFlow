using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Ofertio.Business.Service.Common;

namespace Ofertio.E2E;

public class PricePilotTests
{
    [Fact]
    public void Pilot_ReusesTheCommaLetterAndNegativeSamples()
    {
        Assert.True(MoneyPilot.ValidComma.IsValid);
        Assert.Equal(89.90m, MoneyPilot.ValidComma.Expected);
        Assert.False(MoneyPilot.Letter.IsValid);
        Assert.Equal("abc", MoneyPilot.Letter.Text);
        Assert.False(MoneyPilot.Negative.IsValid);
        Assert.Equal("-1", MoneyPilot.Negative.Text);
    }

    [RequiresE2EFact]
    public async Task AdvertisingPrice_SavesCommaAndRejectsLetterAndNegative()
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var keptName = E2ESettings.RecordName($"preco-{stamp}");
        var letterName = E2ESettings.RecordName($"preco-letra-{stamp}");
        var negativeName = E2ESettings.RecordName($"preco-negativo-{stamp}");

        Assert.True(E2ESettings.TryLoad(out var settings));
        await using var session = await PortalSession.OpenAsync(settings!);
        await session.LoginAsync();
        await session.ExpectHeadingAsync("/propaganda/produtos", "Produtos de propaganda");

        var page = session.Page;
        await page.Locator("#propaganda-preco").WaitForAsync();
        await RejectAdvertisingPriceAsync(page, letterName, MoneyPilot.Letter.Text!);
        await RejectAdvertisingPriceAsync(page, negativeName, MoneyPilot.Negative.Text!);

        await PortalSession.FillAndCommitAsync(page.Locator("#propaganda-nome"), keptName);
        var priceField = page.Locator("#propaganda-preco");
        await PortalSession.FillAndCommitAsync(priceField, MoneyPilot.ValidComma.Text!);
        await Assertions.Expect(priceField).ToHaveValueAsync(MoneyPilot.ValidComma.Text!);
        await page.Locator("#propaganda-salvar").ClickAsync();
        var card = page.Locator("article.propaganda-product-card").Filter(new LocatorFilterOptions { HasText = keptName });
        await Assertions.Expect(card.Locator(".propaganda-product-card__price")).ToContainTextAsync("89,90", Containing);
    }

    [RequiresE2EFact]
    public async Task CampaignBudget_SavesCommaAndRejectsLetterAndNegative()
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var keptName = E2ESettings.RecordName($"orcamento-{stamp}");
        var letterName = E2ESettings.RecordName($"orcamento-letra-{stamp}");
        var negativeName = E2ESettings.RecordName($"orcamento-negativo-{stamp}");

        Assert.True(E2ESettings.TryLoad(out var settings));
        await using var session = await PortalSession.OpenAsync(settings!);
        await session.LoginAsync();
        await session.ExpectHeadingAsync("/dashboard", "Painel");

        var page = session.Page;
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Nova campanha" }).ClickAsync();
        await page.Locator("#campaign-budget").WaitForAsync();
        await RejectCampaignBudgetAsync(page, letterName, MoneyPilot.Letter.Text!);
        await RejectCampaignBudgetAsync(page, negativeName, MoneyPilot.Negative.Text!);

        await PortalSession.FillAndCommitAsync(page.Locator("#campaign-name"), keptName);
        var budget = page.Locator("#campaign-budget");
        await PortalSession.FillAndCommitAsync(budget, MoneyPilot.ValidComma.Text!);
        await Assertions.Expect(budget).ToHaveValueAsync(MoneyPilot.ValidComma.Text!);
        await Assertions.Expect(page.Locator("#campaign-start")).ToHaveValueAsync(new Regex(@"\d{4}-\d{2}-\d{2}"), new LocatorAssertionsToHaveValueOptions { Timeout = 30_000 });
        await page.Locator("#campaign-save").ClickAsync();
        await Assertions.Expect(page.GetByText("Criada com sucesso.")).ToBeVisibleAsync(Visible);

        var nameFilter = page.Locator("#campaign-name-filter");
        await PortalSession.FillAndCommitAsync(nameFilter, keptName);
        await Assertions.Expect(nameFilter).ToHaveValueAsync(keptName);
        await nameFilter
            .Locator("xpath=ancestor::div[contains(@class,'card-body')][1]")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Buscar", Exact = true })
            .ClickAsync();

        var row = page.Locator("#campanhas .data-table-desktop tr").Filter(new LocatorFilterOptions { HasText = keptName });
        await Assertions.Expect(row).ToContainTextAsync("89,90", Containing);
        await Assertions.Expect(page.Locator("#campanhas").GetByText(letterName)).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#campanhas").GetByText(negativeName)).ToHaveCountAsync(0);
    }

    private static async Task RejectAdvertisingPriceAsync(IPage page, string name, string price)
    {
        await PortalSession.FillAndCommitAsync(page.Locator("#propaganda-nome"), name);
        await page.Locator("#propaganda-preco").FillAsync(price);
        await page.Locator("#propaganda-salvar").ClickAsync();
        await Assertions.Expect(page.GetByText(MoneyAmountRules.InvalidPriceMessage)).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = name, Level = 3 })).ToHaveCountAsync(0);
    }

    private static async Task RejectCampaignBudgetAsync(IPage page, string name, string budget)
    {
        await PortalSession.FillAndCommitAsync(page.Locator("#campaign-name"), name);
        await page.Locator("#campaign-budget").FillAsync(budget);
        await page.Locator("#campaign-save").ClickAsync();
        await Assertions.Expect(page.GetByText(MoneyAmountRules.InvalidBudgetMessage)).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.Locator("#campanhas").GetByText(name)).ToHaveCountAsync(0);
    }

    private static LocatorAssertionsToBeVisibleOptions Visible => new() { Timeout = 30_000 };

    private static LocatorAssertionsToContainTextOptions Containing => new() { Timeout = 30_000 };
}
