using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Ofertio.API.Controllers;
using Ofertio.API.Controllers.Master;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Business.Service.Radar;

namespace Ofertio.Tests.Unit.Radar;

/// <summary>
/// Auditoria A4: o cliente não pode somar créditos sem pagar.
/// A recarga existe só pelo Master, com permissão própria e auditoria.
/// </summary>
public class DealCreditsTopUpRemovalTests
{
    [Fact]
    public void ApiAchadinhos_NaoTemEndpointDeRecargaDireta()
    {
        var templates = typeof(DealIntelligenceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => attribute.Template ?? string.Empty);

        Assert.DoesNotContain(templates, template =>
            template.Contains("creditos", StringComparison.OrdinalIgnoreCase)
            || template.Contains("pacote", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ServicoDeCreditos_NaoOfereceRecargaSemPagamento()
    {
        var methods = typeof(IDealCreditsService).GetMethods().Select(method => method.Name);

        Assert.DoesNotContain(methods, name => name.StartsWith("TopUp", StringComparison.Ordinal));
    }

    [Fact]
    public void AjusteDeCreditos_ContinuaRestritoAPermissaoMaster()
    {
        var method = typeof(MasterTenantsController).GetMethod(nameof(MasterTenantsController.AdjustCreditsAsync));

        var policies = method!.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy);

        Assert.Contains(MasterPolicies.AdjustCredits, policies);
    }

    [Fact]
    public void MensagemDeSaldoInsuficiente_NaoOfereceCompraInexistente()
    {
        Assert.DoesNotContain("Compre", DealCreditRules.InsufficientBalanceMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("suporte", DealCreditRules.InsufficientBalanceMessage, StringComparison.OrdinalIgnoreCase);
    }
}
