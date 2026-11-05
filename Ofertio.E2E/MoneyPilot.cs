using Ofertio.Business.Service.Common;

namespace Ofertio.E2E;

/// <summary>Três casos da matriz <c>MoneyAmountRules.Samples</c> exercitados na tela.</summary>
public static class MoneyPilot
{
    public static MoneyAmountSample ValidComma { get; } = Find("89,90");

    public static MoneyAmountSample Letter { get; } = Find("abc");

    public static MoneyAmountSample Negative { get; } = Find("-1");

    private static MoneyAmountSample Find(string text) =>
        MoneyAmountRules.Samples.Single(sample => sample.Text == text);
}
