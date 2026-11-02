using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Ofertio.SharedUi.Components.Layout;
using Ofertio.SharedUi.Components.Pages.Legal;
using Ofertio.SharedUi.Services.Navigation;

namespace Ofertio.Tests.Unit.SharedUi;

public class LegalPagesTests
{
    public static TheoryData<Type, string> LegalPages => new()
    {
        { typeof(TermosDeUso), LegalRoutes.Terms },
        { typeof(PoliticaPrivacidade), LegalRoutes.Privacy },
        { typeof(PoliticaSeguranca), LegalRoutes.Security }
    };

    [Theory]
    [MemberData(nameof(LegalPages))]
    public void LegalPage_ShouldBePublicWithExpectedRouteAndLayout(Type pageType, string expectedRoute)
    {
        // Páginas legais são exigidas pelas plataformas parceiras e precisam abrir sem login.
        var routes = pageType.GetCustomAttributes<RouteAttribute>().Select(r => r.Template);
        Assert.Contains(expectedRoute, routes);
        Assert.NotNull(pageType.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal(typeof(LegalLayout), pageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType);
    }

    [Fact]
    public void LegalRoutes_ShouldNotCollideWithAccountSecurityRoute()
    {
        Assert.NotEqual("/seguranca", LegalRoutes.Security);
        Assert.StartsWith("/legal/", LegalRoutes.Security);
    }
}
