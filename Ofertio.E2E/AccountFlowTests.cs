using Microsoft.Playwright;
using Ofertio.Business.Dto;
using Ofertio.Business.Service.Billing;
using Ofertio.Business.Service.Common;
using Ofertio.Core.Entities;

namespace Ofertio.E2E;

public class AccountFlowTests
{
    private static readonly DateTime CardClock = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AccountRules_RejectBadDocumentCepAndCard()
    {
        var cpf = UserProfileRules.Validate(new UserProfileDto
        {
            DisplayName = "E2E",
            Document = AccountPilot.InvalidCpf
        });
        Assert.Contains("CPF inválido.", cpf);

        var cnpj = UserProfileRules.Validate(new UserProfileDto
        {
            DisplayName = "E2E",
            PersonType = PersonTypes.Company,
            Document = AccountPilot.InvalidCnpj
        });
        Assert.Contains("CNPJ inválido.", cnpj);

        var cep = UserProfileRules.Validate(new UserProfileDto
        {
            DisplayName = "E2E",
            PostalCode = AccountPilot.InvalidCep
        });
        Assert.Contains("CEP inválido. Use 8 dígitos.", cep);
        Assert.Null(BrazilTimeZoneRules.NormalizeStateCode("XX"));
        Assert.Equal("SP", BrazilTimeZoneRules.NormalizeStateCode("SP"));

        var badCard = TestCard(AccountPilot.InvalidCard);
        Assert.Contains("Número do cartão inválido.", CreditCardRules.Validate(badCard, CardClock));

        var testCard = TestCard(AccountPilot.TestCard);
        Assert.Empty(CreditCardRules.Validate(testCard, CardClock));
        Assert.Equal("Visa", CreditCardRules.DetectBrand(AccountPilot.TestCard));
    }

    [RequiresE2EFact]
    public async Task Register_RejectsInvalidEmailAndMismatchedPasswords()
    {
        await using var session = await OpenAnonymousAsync();
        await session.ExpectHeadingAsync("/cadastro", "Crie sua conta");
        await session.AcceptCookieBannerAsync();
        var page = session.Page;

        await PortalSession.FillAndCommitAsync(page.Locator("#register-email"), AccountPilot.InvalidEmail);
        await PortalSession.FillAndCommitAsync(page.Locator("#register-password"), "senha-a");
        await PortalSession.FillAndCommitAsync(page.Locator("#register-confirm-password"), "senha-a");
        var submit = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Concluir cadastro" });
        await Assertions.Expect(submit).ToBeDisabledAsync();
        await page.Locator("#register-terms").CheckAsync();
        await Assertions.Expect(submit).ToBeEnabledAsync();
        await submit.ClickAsync();
        await Assertions.Expect(page.GetByText("E-mail inválido.")).ToBeVisibleAsync(Visible);
        Assert.Contains("/cadastro", page.Url, StringComparison.OrdinalIgnoreCase);

        await PortalSession.FillAndCommitAsync(page.Locator("#register-email"), AccountPilot.UntouchedEmail);
        await PortalSession.FillAndCommitAsync(page.Locator("#register-password"), "senha-a");
        await PortalSession.FillAndCommitAsync(page.Locator("#register-confirm-password"), "senha-b");
        await Assertions.Expect(page.GetByText("As senhas informadas não coincidem.")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Concluir cadastro" })).ToBeDisabledAsync();
        Assert.Contains("/cadastro", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [RequiresE2EFact]
    public async Task Recovery_AcceptsTheRequestWithoutSendingANewPassword()
    {
        await using var session = await OpenAnonymousAsync();
        await session.ExpectHeadingAsync("/recuperar-senha", "Recuperar ou alterar senha");
        await session.AcceptCookieBannerAsync();
        var page = session.Page;
        await PortalSession.FillAndCommitAsync(page.Locator("#recovery-email"), AccountPilot.InvalidEmail);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Enviar instruções" }).ClickAsync();
        await Assertions.Expect(page.GetByText("E-mail inválido.")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByText("Recebemos sua solicitação")).ToHaveCountAsync(0);

        await PortalSession.FillAndCommitAsync(page.Locator("#recovery-email"), AccountPilot.UntouchedEmail);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Enviar instruções" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Recebemos sua solicitação")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByText("A recuperação automática de senha será habilitada em breve.")).ToBeVisibleAsync(Visible);
    }

    [RequiresE2EFact]
    public async Task Profile_RejectsInvalidCpfCnpjAndCepWithoutSaving()
    {
        await using var session = await OpenSignedInAsync();
        await session.ExpectHeadingAsync("/meus-dados", "Meus dados");
        var page = session.Page;
        await page.Locator("#profile-document").WaitForAsync();

        var document = await page.Locator("#profile-document").InputValueAsync();
        var cep = await page.Locator("#profile-cep").InputValueAsync();
        var state = await page.Locator("#profile-state").InputValueAsync();
        var wasCompany = await page.Locator("#person-pj").IsCheckedAsync();

        if (wasCompany)
        {
            await page.Locator("label[for='person-pf']").ClickAsync();
        }

        await page.Locator("#profile-document").FillAsync(AccountPilot.InvalidCpf);
        await SaveProfileAsync(page);
        await Assertions.Expect(page.GetByText("CPF inválido.").First).ToBeVisibleAsync(Visible);

        await page.Locator("#profile-document").FillAsync(document);
        await page.Locator("#profile-cep").FillAsync(AccountPilot.InvalidCep);
        await SaveProfileAsync(page);
        await Assertions.Expect(page.GetByText("CEP inválido. Use 8 dígitos.").First).ToBeVisibleAsync(Visible);
        await page.Locator("#profile-cep").FillAsync(cep);

        await page.Locator("#profile-state").SelectOptionAsync("SP");
        await Assertions.Expect(page.Locator("#profile-state")).ToHaveValueAsync("SP");
        await page.Locator("#profile-state").SelectOptionAsync(state);

        await page.Locator("label[for='person-pj']").ClickAsync();
        await Assertions.Expect(page.Locator("#person-pj")).ToBeCheckedAsync();
        await Assertions.Expect(page.Locator("#profile-document")).ToHaveValueAsync(string.Empty);
        await PortalSession.FillAndCommitAsync(page.Locator("#profile-document"), AccountPilot.InvalidCnpj);
        await SaveProfileAsync(page);
        await Assertions.Expect(page.GetByText("CNPJ inválido.").First).ToBeVisibleAsync(Visible);

        if (!wasCompany)
        {
            await page.Locator("label[for='person-pf']").ClickAsync();
        }

        await page.Locator("#profile-document").FillAsync(document);
        await Assertions.Expect(page.GetByText("Dados cadastrais salvos.")).ToHaveCountAsync(0);
    }

    [RequiresE2EFact]
    public async Task Password_StaysUntouchedWhenTheConfirmationDoesNotMatch()
    {
        await using var session = await OpenSignedInAsync();
        await session.ExpectHeadingAsync("/minha-conta", "Minha Conta / Segurança");
        var page = session.Page;

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Alterar senha" }).ClickAsync();
        await page.Locator("#new-password").WaitForAsync();
        await PortalSession.FillAndCommitAsync(page.Locator("#new-password"), "senha-a");
        await PortalSession.FillAndCommitAsync(page.Locator("#confirm-password"), "senha-b");
        await Assertions.Expect(page.GetByText("A confirmação da nova senha não coincide.")).ToBeVisibleAsync(Visible);

        var save = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salvar nova senha" });
        await Assertions.Expect(save).ToBeDisabledAsync();

        if (await page.Locator("#current-password").CountAsync() == 0)
        {
            return;
        }

        await PortalSession.FillAndCommitAsync(page.Locator("#new-password"), "senha-igual");
        await PortalSession.FillAndCommitAsync(page.Locator("#confirm-password"), "senha-igual");
        await Assertions.Expect(save).ToBeEnabledAsync();
        await save.ClickAsync();
        await Assertions.Expect(page.GetByText("Senha atual é obrigatória.")).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByText("Senha alterada")).ToHaveCountAsync(0);
    }

    [RequiresE2EFact]
    public async Task Card_UsesTheTestNumberWithoutARealCharge()
    {
        await using var session = await OpenSignedInAsync();
        await session.ExpectHeadingAsync("/meu-plano", "Meu plano");
        var page = session.Page;
        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Planos disponíveis", Level = 2 }).WaitForAsync();

        if (await page.GetByText("Conta Master não tem assinatura").IsVisibleAsync())
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Escolher" })).ToHaveCountAsync(0);
            return;
        }

        var escolher = page.Locator("button:not([disabled])").Filter(new LocatorFilterOptions { HasText = "Escolher" });
        if (await page.GetByText("Para assinar um plano ainda falta").IsVisibleAsync())
        {
            await escolher.First.ClickAsync();
            await Assertions.Expect(page.Locator("#confirm-dialog-title")).ToHaveTextAsync("Faltam dados cadastrais");
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Agora não" }).ClickAsync();
            Assert.DoesNotContain("/pagamento/", page.Url, StringComparison.OrdinalIgnoreCase);
            return;
        }

        if (await escolher.CountAsync() == 0)
        {
            return;
        }

        await escolher.First.ClickAsync();
        await page.WaitForURLAsync("**/meu-plano/pagamento/**", new PageWaitForURLOptions { Timeout = 30_000 });

        var simulated = page.GetByText("nenhuma cobrança é feita");
        var cycle = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Como você quer pagar?" });
        var blocked = page.GetByText("Antes de pagar, complete")
            .Or(page.GetByText("A conta Master não assina planos"))
            .Or(page.GetByText("Plano não encontrado"));
        await Assertions.Expect(simulated.Or(cycle).Or(blocked)).ToBeVisibleAsync(Visible);
        if (await blocked.IsVisibleAsync() || !await cycle.IsVisibleAsync())
        {
            return;
        }

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Continuar para o cartão" }).ClickAsync();
        await page.Locator("#card-number").WaitForAsync();
        await FillCardAsync(page, AccountPilot.InvalidCard);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Assinar" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Número do cartão inválido.")).ToBeVisibleAsync(Visible);
        Assert.DoesNotContain("/concluido/", page.Url, StringComparison.OrdinalIgnoreCase);

        await FillCardAsync(page, AccountPilot.TestCard);
        await Assertions.Expect(page.GetByText("Visa", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync(Visible);
        if (!await simulated.IsVisibleAsync())
        {
            return;
        }

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Assinar" }).ClickAsync();
        await page.WaitForURLAsync("**/meu-plano/pagamento/concluido/**", new PageWaitForURLOptions { Timeout = 30_000 });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Pedido registrado em modo de teste", Level = 1 })).ToBeVisibleAsync(Visible);
        await Assertions.Expect(page.GetByText("1111")).ToBeVisibleAsync(Visible);
    }

    private static CreditCardDto TestCard(string number) => new()
    {
        Number = number,
        HolderName = AccountPilot.TestHolder,
        Expiry = AccountPilot.TestExpiry,
        Cvv = AccountPilot.TestCvv
    };

    private static async Task SaveProfileAsync(IPage page)
    {
        var save = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salvar" });
        await Assertions.Expect(save).ToBeEnabledAsync();
        await save.ClickAsync();
    }

    private static async Task FillCardAsync(IPage page, string number)
    {
        await page.Locator("#card-number").FillAsync(number);
        await page.Locator("#card-holder").FillAsync(AccountPilot.TestHolder);
        await page.Locator("#card-expiry").FillAsync(AccountPilot.TestExpiry);
        await page.Locator("#card-cvv").FillAsync(AccountPilot.TestCvv);
    }

    private static async Task<PortalSession> OpenAnonymousAsync()
    {
        Assert.True(E2ESettings.TryLoad(out var settings));
        return await PortalSession.OpenAsync(settings!);
    }

    private static async Task<PortalSession> OpenSignedInAsync()
    {
        var session = await OpenAnonymousAsync();
        await session.LoginAsync();
        return session;
    }

    private static LocatorAssertionsToBeVisibleOptions Visible => new() { Timeout = 30_000 };
}
