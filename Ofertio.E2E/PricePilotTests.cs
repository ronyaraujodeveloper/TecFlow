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

        await page.Locator("#propaganda-nome").FillAsync(keptName);
        await page.Locator("#propaganda-preco").FillAsync(MoneyPilot.ValidComma.Text!);
        await page.Locator("#propaganda-salvar").ClickAsync();

        await Assertions.Expect(page.GetByText("Produto salvo com sucesso.")).ToBeVisibleAsync(Visible);
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

        await page.Locator("#campaign-name").FillAsync(keptName);
        await page.Locator("#campaign-budget").FillAsync(MoneyPilot.ValidComma.Text!);
        await Assertions.Expect(page.Locator("#campaign-start")).ToHaveValueAsync(new Regex(@"\d{4}-\d{2}-\d{2}"), new LocatorAssertionsToHaveValueOptions { Timeout = 30_000 });
        await page.Locator("#campaign-save").ClickAsync();
        await Assertions.Expect(page.GetByText("Criada com sucesso.")).ToBeVisibleAsync(Visible);

        await page.Locator("#campaign-name-filter").FillAsync(keptName);
        await page.Locator("#campaign-name-filter")
            .Locator("xpath=ancestor::div[contains(@class,'card-body')][1]")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Buscar" })
            .ClickAsync();

        var row = page.Locator("#campanhas tr").Filter(new LocatorFilterOptions { HasText = keptName });
        await Assertions.Expect(row).ToContainTextAsync("89,90", Containing);
        await Assertions.Expect(page.Locator("#campanhas").GetByText(letterName)).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#campanhas").GetByText(negativeName)).ToHaveCountAsync(0);
    }

    private static async Task RejectAdvertisingPriceAsync(IPage page, string name, string price)
    {
        await page.Locator("#propaganda-nome").FillAsync(name);
        await page.Locator("#propaganda-preco").FillAsync(price);
        await page.Locator("#propaganda-salvar").ClickAsync();
        await Assertions.Expect(page.GetByText(MoneyAmountRules.InvalidPriceMessage)).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = name, Level = 3 })).ToHaveCountAsync(0);
    }

    private static async Task RejectCampaignBudgetAsync(IPage page, string name, string budget)
    {
        await page.Locator("#campaign-name").FillAsync(name);
        await page.Locator("#campaign-budget").FillAsync(budget);
        await page.Locator("#campaign-save").ClickAsync();
        await Assertions.Expect(page.GetByText(MoneyAmountRules.InvalidBudgetMessage)).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.Locator("#campanhas").GetByText(name)).ToHaveCountAsync(0);
    }

    private static LocatorAssertionsToBeVisibleOptions Visible => new() { Timeout = 30_000 };

    private static LocatorAssertionsToContainTextOptions Containing => new() { Timeout = 30_000 };
}
