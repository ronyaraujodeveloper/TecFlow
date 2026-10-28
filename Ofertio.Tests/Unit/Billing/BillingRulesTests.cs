using Ofertio.Business.Dto;
using Ofertio.Business.Service.Billing;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;

namespace Ofertio.Tests.Unit.Billing;

public class UserProfileRulesTests
{
    private const string ValidCpf = "529.982.247-25";
    private const string ValidCnpj = "11.222.333/0001-81";

    private static UserProfileDto CompleteIndividual() => new()
    {
        PersonType = PersonTypes.Individual,
        DisplayName = "Maria",
        FullName = "Maria da Silva",
        Document = ValidCpf,
        Phone = "(11) 98765-4321",
        PostalCode = "01310-100",
        Street = "Avenida Paulista",
        Number = "1000",
        District = "Bela Vista",
        City = "São Paulo",
        State = "sp"
    };

    [Fact]
    public void Normalize_ShouldTrimAndKeepOnlyUsefulCharacters()
    {
        var source = CompleteIndividual();
        source.FullName = "  Maria da Silva  ";
        source.ResponsibleName = "Ignorado em PF";

        var normalized = UserProfileRules.Normalize(source);

        Assert.Equal("Maria da Silva", normalized.FullName);
        Assert.Equal("52998224725", normalized.Document);
        Assert.Equal("11987654321", normalized.Phone);
        Assert.Equal("01310100", normalized.PostalCode);
        Assert.Equal("SP", normalized.State);
        Assert.Null(normalized.ResponsibleName);
    }

    [Fact]
    public void CompleteIndividual_ShouldHaveNoErrorsAndNothingMissing()
    {
        var normalized = UserProfileRules.Normalize(CompleteIndividual());

        Assert.Empty(UserProfileRules.Validate(normalized));
        Assert.Empty(UserProfileRules.MissingFields(normalized));
    }

    [Fact]
    public void Validate_ShouldRejectInvalidCpfAndCnpj()
    {
        var individual = UserProfileRules.Normalize(CompleteIndividual());
        individual.Document = "11111111111";
        var company = UserProfileRules.Normalize(CompleteIndividual());
        company.PersonType = PersonTypes.Company;
        company.Document = "11222333000100";

        Assert.Contains("CPF inválido.", UserProfileRules.Validate(individual));
        Assert.Contains("CNPJ inválido.", UserProfileRules.Validate(company));
    }

    [Fact]
    public void Validate_ShouldRequireDisplayName()
    {
        var profile = UserProfileRules.Normalize(CompleteIndividual());
        profile.DisplayName = " ";

        Assert.Contains("Informe o nome de exibição.", UserProfileRules.Validate(UserProfileRules.Normalize(profile)));
    }

    [Fact]
    public void MissingFields_ForCompany_ShouldAskCompanyNameCnpjAndResponsible()
    {
        var missing = UserProfileRules.MissingFields(new UserProfileDto { PersonType = PersonTypes.Company });

        Assert.Equal(
            ["Razão social", "CNPJ", "Nome do responsável", "Celular", "CEP", "Rua", "Número", "Bairro", "Cidade", "UF"],
            missing);
    }

    [Fact]
    public void MissingFields_ForCompleteCompany_ShouldBeEmpty()
    {
        var profile = CompleteIndividual();
        profile.PersonType = PersonTypes.Company;
        profile.FullName = "Loja da Maria LTDA";
        profile.Document = ValidCnpj;
        profile.ResponsibleName = "Maria da Silva";

        Assert.Empty(UserProfileRules.MissingFields(UserProfileRules.Normalize(profile)));
    }

    [Theory]
    [InlineData("52998224725", "529.982.247-25")]
    [InlineData("11222333000181", "11.222.333/0001-81")]
    [InlineData("123", "123")]
    [InlineData(null, null)]
    public void FormatDocument_ShouldMaskCpfAndCnpj(string? document, string? expected)
    {
        Assert.Equal(expected, UserProfileRules.FormatDocument(document));
    }
}

public class PlanPricingRulesTests
{
    private static SaasPlanDto Plan(decimal monthly, decimal annual) => new()
    {
        Code = SaasPlanCodes.Pro,
        Name = "Pro",
        MonthlyPrice = monthly,
        AnnualPrice = annual,
        MaxStores = 3,
        MaxDailySchedules = 0,
        DailyVipQuota = 10,
        RadarEnabled = true
    };

    [Theory]
    [InlineData("Mensal", "Mensal")]
    [InlineData("monthly", "Mensal")]
    [InlineData(" anual ", "Anual")]
    [InlineData("yearly", "Anual")]
    [InlineData("semanal", null)]
    [InlineData(null, null)]
    public void NormalizeCycle_ShouldAcceptPortugueseAndEnglish(string? cycle, string? expected)
    {
        Assert.Equal(expected, PlanPricingRules.NormalizeCycle(cycle));
    }

    [Fact]
    public void Price_ShouldFollowCycle()
    {
        var plan = Plan(100m, 1000m);

        Assert.Equal(100m, PlanPricingRules.Price(plan, SaasMetricsRules.CycleMonthly));
        Assert.Equal(1000m, PlanPricingRules.Price(plan, SaasMetricsRules.CycleAnnual));
        Assert.False(PlanPricingRules.IsFree(plan));
        Assert.True(PlanPricingRules.IsFree(Plan(0, 0)));
    }

    [Theory]
    [InlineData(100, 1000, 17)]
    [InlineData(100, 1200, 0)]
    [InlineData(100, 1500, 0)]
    [InlineData(0, 1000, 0)]
    public void AnnualSavingsPercent_ShouldCompareWithTwelveMonths(decimal monthly, decimal annual, int expected)
    {
        Assert.Equal(expected, PlanPricingRules.AnnualSavingsPercent(monthly, annual));
    }

    [Fact]
    public void Highlights_ShouldDropBulletsAndEmptyLines()
    {
        const string raw = "- Suporte prioritário\r\n\n• Relatórios semanais\n*  Bot de conversão  \n   ";

        Assert.Equal(["Suporte prioritário", "Relatórios semanais", "Bot de conversão"], PlanPricingRules.HighlightLines(raw));
        Assert.Equal("Suporte prioritário\nRelatórios semanais\nBot de conversão", PlanPricingRules.NormalizeHighlights(raw));
        Assert.Null(PlanPricingRules.NormalizeHighlights(" \n "));
    }

    [Fact]
    public void LimitLines_ShouldDescribeUnlimitedAsUnlimited()
    {
        var lines = PlanPricingRules.LimitLines(Plan(100m, 1000m));

        Assert.Contains("Até 3 loja(s) conectada(s)", lines);
        Assert.Contains("Disparos ilimitados (WhatsApp + Telegram)", lines);
        Assert.Contains("Radar de ofertas incluído", lines);
    }
}

public class CreditCardRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("4111 1111 1111 1111", true)]
    [InlineData("4111 1111 1111 1112", false)]
    [InlineData("1234", false)]
    public void IsLuhnValid_ShouldCheckDigit(string number, bool expected)
    {
        Assert.Equal(expected, CreditCardRules.IsLuhnValid(CreditCardRules.Digits(number)));
    }

    [Theory]
    [InlineData("4111111111111111", "Visa")]
    [InlineData("5555555555554444", "Mastercard")]
    [InlineData("2223000048400011", "Mastercard")]
    [InlineData("378282246310005", "Amex")]
    [InlineData("6062825624254001", "Hipercard")]
    [InlineData("9999999999999999", "Cartão")]
    public void DetectBrand_ShouldRecognizeCommonBrands(string number, string expected)
    {
        Assert.Equal(expected, CreditCardRules.DetectBrand(number));
    }

    [Fact]
    public void Validate_ValidCard_ShouldPass()
    {
        var card = new CreditCardDto { Number = "4111 1111 1111 1111", HolderName = "MARIA SILVA", Expiry = "12/30", Cvv = "123" };

        Assert.Empty(CreditCardRules.Validate(card, Now));
        Assert.Equal("1111", CreditCardRules.Last4(card.Number));
    }

    [Fact]
    public void Validate_ShouldRejectExpiredCardAndSingleWordName()
    {
        var card = new CreditCardDto { Number = "4111111111111111", HolderName = "MARIA", Expiry = "09/26", Cvv = "123" };

        var errors = CreditCardRules.Validate(card, Now);

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, error => error.Contains("vencido"));
        Assert.Contains(errors, error => error.Contains("nome impresso"));
    }

    [Fact]
    public void Validate_AmexShouldRequireFourDigitCvv()
    {
        var card = new CreditCardDto { Number = "378282246310005", HolderName = "MARIA SILVA", Expiry = "12/2030", Cvv = "123" };

        Assert.Contains(CreditCardRules.Validate(card, Now), error => error.Contains("4 dígitos"));
        card.Cvv = "1234";
        Assert.Empty(CreditCardRules.Validate(card, Now));
    }

    [Theory]
    [InlineData("10/26", false)]
    [InlineData("09/26", true)]
    public void IsExpired_ShouldAllowTheCurrentMonth(string expiry, bool expected)
    {
        Assert.True(CreditCardRules.TryParseExpiry(expiry, out var month, out var year));
        Assert.Equal(expected, CreditCardRules.IsExpired(month, year, Now));
    }
}
